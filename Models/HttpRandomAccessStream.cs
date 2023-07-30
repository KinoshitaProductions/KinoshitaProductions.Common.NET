#if WINDOWS_UWP
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Storage.Streams;
using HttpClient = Windows.Web.Http.HttpClient;
using HttpRequestMessage = Windows.Web.Http.HttpRequestMessage;
using HttpResponseMessage = Windows.Web.Http.HttpResponseMessage;
using HttpCompletionOption = Windows.Web.Http.HttpCompletionOption;
using HttpMethod = Windows.Web.Http.HttpMethod;
using Serilog;

namespace KinoshitaProductions.Common.Models;

public class HttpRandomAccessStream : IRandomAccessStreamWithContentType
{
    private sealed class SharedStreamData
    {
        internal readonly int AttemptNumber;
        internal readonly string ContentType;
        internal readonly ulong Size;
        internal readonly MemoryStream BufferStream;
        internal readonly IInputStream InputStream;

        public SharedStreamData(int attemptNumber, string contentType, ulong size, MemoryStream bufferStream, IInputStream inputStream)
        {
            AttemptNumber = attemptNumber;
            ContentType = contentType;
            Size = size;
            BufferStream = bufferStream;
            InputStream = inputStream;
        }
    }

    private static readonly Dictionary<Uri, SharedStreamData> SharedData = new ();
    private MemoryStream? _ms;
    private readonly HttpClient _client;
    private IInputStream? _inputStream;
    private ulong _currentPosition;
    private string? _etagHeader;
    private string? _lastModifiedHeader;
    private readonly Uri _requestedUri;
    private int _attemptNumber;
    private bool _finishedReading;

    // No public constructor, factory methods instead to handle async tasks.
    private HttpRandomAccessStream(HttpClient client, Uri uri)
    {
        _client = client;
        _requestedUri = uri;
        _currentPosition = 0;
        _ms = new MemoryStream();
    }

    public static IAsyncOperation<HttpRandomAccessStream> CreateAsync(HttpClient client, Uri uri)
    {
        HttpRandomAccessStream randomStream = new HttpRandomAccessStream(client, uri);

        return AsyncInfo.Run<HttpRandomAccessStream>(async (cancellationToken) =>
        {
            int nextAttemptNumber = 0;

            // check if there was an attempt running earlier and clean it up
            lock (SharedData)
            {
                if (SharedData.TryGetValue(uri, out var previousSharedData))
                {
                    nextAttemptNumber = previousSharedData.AttemptNumber + 10;
                }
            }

            await randomStream.SendRequestAsync(nextAttemptNumber, cancellationToken).ConfigureAwait(false);
            return randomStream;
        });
    }

    private async Task SendRequestAsync(int attemptNumber, CancellationToken cancellationToken)
    {
        _attemptNumber = attemptNumber;
        lock (SharedData)
        {
            if (SharedData.TryGetValue(_requestedUri, out var sharedData) && attemptNumber == sharedData.AttemptNumber)
            {
                ContentType = sharedData.ContentType;
                Size = sharedData.Size;
                _inputStream = sharedData.InputStream;
                _ms = sharedData.BufferStream;
                return;
            }
        }
        
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, _requestedUri);

        HttpResponseMessage response = await _client.SendRequestAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead).AsTask(cancellationToken).ConfigureAwait(false);

        if (response.Content.Headers.ContentType != null)
            ContentType = response.Content.Headers.ContentType.MediaType;

        if (response.Content.Headers.ContentLength != null)
            Size = response.Content.Headers.ContentLength.Value;

        if (string.IsNullOrEmpty(_etagHeader) && response.Headers.TryGetValue("ETag", out var etagHeader))
        {
            _etagHeader = etagHeader;
        }
        if (string.IsNullOrEmpty(_lastModifiedHeader) && response.Content.Headers.TryGetValue("Last-Modified", out var lastModifiedHeader))
        {
            _lastModifiedHeader = lastModifiedHeader;
        }
        if (response.Content.Headers.TryGetValue("Content-Type", out var contentTypeHeader))
        {
            ContentType = contentTypeHeader;
        }

        _inputStream = await response.Content.ReadAsInputStreamAsync().AsTask(cancellationToken).ConfigureAwait(false);

        if (_ms != null)
        {
            var cacheData = new SharedStreamData
            (
                attemptNumber: attemptNumber,
                contentType: ContentType,
                size: Size,
                bufferStream: _ms,
                inputStream: _inputStream
            );
            lock (SharedData)
                SharedData[_requestedUri] = cacheData;
        }
    }

    public string ContentType { get; private set; } = string.Empty;

    public bool CanRead => true;

    public bool CanWrite => false;

    public IRandomAccessStream CloneStream()
    {
        return this; // since we rely on caching for this, it should be safe to return the same
    }

    public IInputStream GetInputStreamAt(ulong position)
    {
        throw new InvalidOperationException("This stream does not support seeking" );
    }

    public IOutputStream GetOutputStreamAt(ulong position)
    {
        throw new InvalidOperationException("This stream does not support seeking" );
    }

    public ulong Position => _currentPosition;

    public void Seek(ulong position)
    {
        if (_currentPosition != position)
        {
            _ms?.Seek((long)position, SeekOrigin.Begin);
            _currentPosition = position;
        }
    }

    public ulong Size { get; set; }

    protected virtual void Dispose(bool disposing)
    {
        lock (SharedData)
            SharedData.Remove(_requestedUri);
        _inputStream?.Dispose();
        _inputStream = null;
        _ms?.Dispose();
        _ms = null;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~HttpRandomAccessStream()
    {
        Dispose(false);
    }
    
    public IAsyncOperationWithProgress<IBuffer, uint> ReadAsync(IBuffer buffer, uint count, InputStreamOptions options)
    {
        return AsyncInfo.Run<IBuffer, uint>(async (cancellationToken, progress) =>
        {
            if (_ms == null)
                return new Windows.Storage.Streams.Buffer(0); // return empty buffer, we're disposing

            progress.Report(0);
            
            // cache up to requested position
            while (!_finishedReading && (long)_currentPosition + count > _ms.Length)
            {
                if (_inputStream == null)
                {
                    try
                    {
                        await SendRequestAsync(_attemptNumber + 1, cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Failed to get input stream");
                    }
                }
                
                if (_inputStream == null)
                    return new Windows.Storage.Streams.Buffer(0); // return empty buffer, there's no data source

                var readBuffer = await _inputStream.ReadAsync(buffer, count, options).AsTask(cancellationToken).ConfigureAwait(false);
                if (readBuffer.Length < count)
                    _finishedReading = true;
                if (_ms == null)
                    return new Windows.Storage.Streams.Buffer(0); // return empty buffer, we're disposing
                
                // to properly add data, we must seek to the end
                _ms.Seek(0, SeekOrigin.End);
                await readBuffer.AsStream().CopyToAsync(_ms, 81920, cancellationToken);
                _ms.Seek((long)_currentPosition, SeekOrigin.Begin); // rewind
            }
            if (cancellationToken.IsCancellationRequested)
                return new Windows.Storage.Streams.Buffer(0); // return empty buffer, we're cancelling
            // return requested data
            if (_ms == null)
                return new Windows.Storage.Streams.Buffer(0); // return empty buffer, we're disposing
            
            var finalBuffer = await _ms.AsInputStream().ReadAsync(buffer, count, InputStreamOptions.ReadAhead).AsTask(cancellationToken).ConfigureAwait(false);
            
            // Move position forward.
            _currentPosition += count;

            return finalBuffer;
        });
    }

    public IAsyncOperation<bool> FlushAsync()
    {
        throw new InvalidOperationException("This stream does not support writing (and so, neither flushing)");
    }

    public IAsyncOperationWithProgress<uint, uint> WriteAsync(IBuffer buffer)
    {
        throw new InvalidOperationException("This stream does not support writing");
    }
}
#endif