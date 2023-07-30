namespace KinoshitaProductions.Common.Models;

#if WINDOWS_UWP
using Windows.Web.Http;
#else
using System.Net;
#endif

public sealed class HttpClientDownloadWithProgress : IDisposable
#if NET7_0_OR_GREATER
    , IAsyncDisposable
#endif
{
    private readonly CancellationToken _cancellationToken;
    private HttpRequestMessage _httpRequestMessage;
    private Stream? _outputStream;

    private HttpClient _httpClient;

    public delegate void ProgressChangedHandler(long? totalFileSize, long totalBytesDownloaded, double? progressPercentage);

    public event ProgressChangedHandler? ProgressChanged;

    public HttpClientDownloadWithProgress(HttpClient httpClient, HttpRequestMessage httpRequestMessage, CancellationToken cancellationToken, Stream? outputStream = null)
    {
        _httpClient = httpClient;
        _httpRequestMessage = httpRequestMessage;
        _cancellationToken = cancellationToken;
        _outputStream = outputStream;
        //httpClient.Timeout = Timeout.InfiniteTimeSpan; // do not timeout! // Would require separate clients for images and json
    }

    public async Task<DownloadResult> StartDownload(
#if WINDOWS_UWP
        bool leaveOpen = false
#endif
        )
    {
        try
        {
            if (_cancellationToken.IsCancellationRequested)
                return new DownloadResult(DownloadResultStatus.Cancelled);

            using var response = await _httpClient.
#if WINDOWS_UWP
                SendRequestAsync(_httpRequestMessage, HttpCompletionOption.ResponseHeadersRead);
#else
                SendAsync(_httpRequestMessage, HttpCompletionOption.ResponseHeadersRead, _cancellationToken);
#endif
            var download = await DownloadFileFromHttpResponseMessage(response);
#if WINDOWS_UWP
            if (leaveOpen)
            {
                download.LeaveOpen = true;
            }
#endif
            return download;
        }
        catch (Exception ex)
        {
            var genericMessage = "Unable to read data from the transport connection: ";
            return new DownloadResult(ex.Message.Contains(genericMessage) ? ex.Message.Substring(genericMessage.Length) : ex.Message); // SHOULD NEVER HAPPEN
        }
    }

    private async Task<DownloadResult> DownloadFileFromHttpResponseMessage(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            return response.StatusCode is HttpStatusCode.InternalServerError or HttpStatusCode.RequestTimeout or HttpStatusCode.GatewayTimeout ? new DownloadResult(DownloadResultStatus.Error) : new DownloadResult(DownloadResultStatus.Invalid);
        }

        if (_cancellationToken.IsCancellationRequested)
            return new DownloadResult(DownloadResultStatus.Cancelled);

        var compressionAlgorithm = Web.GetCompressionAlgorithmForResponse(response);
        
        if (_cancellationToken.IsCancellationRequested)
            return new DownloadResult(DownloadResultStatus.Cancelled);
#if NET7_0_OR_GREATER
        await using var contentStream = Compression.GetDecompressionStreamFor(await response.Content
#if WINDOWS_UWP
            .ReadAsInputStreamAsync()
#else
            .ReadAsStreamAsync(_cancellationToken)
#endif
            , compressionAlgorithm);
        return await ProcessContentStream(
#if WINDOWS_UWP
            (long?)
#endif
            response.Content.Headers.ContentLength
            , contentStream);
#else
        using var contentStream = Compression.GetDecompressionStreamFor(await response.Content
#if WINDOWS_UWP
                .ReadAsInputStreamAsync()
#else
            .ReadAsStreamAsync(_cancellationToken)
#endif
            , compressionAlgorithm);
        return await ProcessContentStream(
#if WINDOWS_UWP
            (long?)
#endif
            response.Content.Headers.ContentLength
            , contentStream);
#endif
    }

    private async Task<DownloadResult> ProcessContentStream(long? totalDownloadSize, Stream contentStream)
    {
        long totalBytesRead = 0L, readCount = 0L;
        var buffer = new byte[1024 * 16];
        var writingToFile = _outputStream != null;
        var outputStream = _outputStream ?? new MemoryStream();
        do
        {
            // check for cancellation
            if (_cancellationToken.IsCancellationRequested)
            {
#if NET7_0_OR_GREATER
                await outputStream.DisposeAsync(); // dispose created stream
#else
                outputStream.Dispose();
#endif
                return new DownloadResult(DownloadResultStatus.Cancelled);
            }
#if NET7_0_OR_GREATER
            var bytesRead = await contentStream.ReadAsync(buffer, _cancellationToken);
            await outputStream.WriteAsync(buffer.AsMemory(0, bytesRead), _cancellationToken);
#else
            var bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, _cancellationToken);
            await outputStream.WriteAsync(buffer, 0, bytesRead, _cancellationToken);
#endif
            totalBytesRead += bytesRead;
            readCount += 1;

            if (totalDownloadSize.HasValue)
            {
                // we know download size, so wait until it's completed
                if (totalBytesRead >= totalDownloadSize.Value)
                {
                    break;
                }
            }
            else if (bytesRead == 0)
            {
                // we don't know download size, so wait until we don't get more bytes
                break;
            }

            if (readCount % 128 == 0)
                TriggerProgressChanged(totalDownloadSize, totalBytesRead);
        }
        while (true);

        if (_cancellationToken.IsCancellationRequested)
            return new DownloadResult(DownloadResultStatus.Cancelled);

        // rewind stream if not writing to file
        if (!writingToFile)
        {
            outputStream.Seek(0, SeekOrigin.Begin);
            return new DownloadResult(DownloadResultStatus.Success, outputStream);
        }
        else
        {
            await outputStream.FlushAsync(_cancellationToken);
            return new DownloadResult(DownloadResultStatus.Success); // does not return stream if outputted
        }
    }

    private void TriggerProgressChanged(long? totalDownloadSize, long totalBytesRead)
    {
        if (ProgressChanged == null)
            return;

        double? progressPercentage;
        if (totalDownloadSize.HasValue)
            progressPercentage = Math.Round((double)totalBytesRead / totalDownloadSize.Value * 100, 2);
        else
            progressPercentage = 0; // UNSUPPORTED

        ProgressChanged(totalDownloadSize, totalBytesRead, progressPercentage);
    }

    public void Dispose()
    {
        Dispose(true);
    }

    private void Dispose(bool disposing)
    {
        if (!disposing) return;
        _httpClient = null!; // this is reused
        if (_outputStream != null)
        {
            _outputStream.Dispose();
            _outputStream = null;
        }
        _httpRequestMessage.Dispose();
        _httpRequestMessage = null!;
    }
#if NET7_0_OR_GREATER
    public async ValueTask DisposeAsync()
    {
        await DisposeAsync(true);
    }
    private async ValueTask DisposeAsync(bool disposing)
    {
        if (!disposing) return;
        _httpClient = null!; // this is reused
        if (_outputStream != null)
        {
            await _outputStream.DisposeAsync();
            _outputStream = null;
        }
        _httpRequestMessage.Dispose();
        _httpRequestMessage = null!;
    }
#endif
}
