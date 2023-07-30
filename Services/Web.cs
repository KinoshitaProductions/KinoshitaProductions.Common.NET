// ReSharper disable MemberCanBePrivate.Global

using System.Diagnostics;

namespace KinoshitaProductions.Common.Services;
#if WINDOWS_UWP
using Windows.Web.Http;
using Windows.Web.Http.Filters;
#else
using System.Net;
using System.Net.Http;
using System.Net.Security;
#endif
using System.Text;

using Newtonsoft.Json;
using Serilog;
using System.Runtime.Serialization;

public static class Web
{
#if !WINDOWS_UWP
    public static Func<HttpRequestMessage, System.Security.Cryptography.X509Certificates.X509Certificate2?, System.Security.Cryptography.X509Certificates.X509Chain?, SslPolicyErrors, bool>? ServerCertificateCustomValidationCallback { get; private set; }
#endif
#pragma warning disable CS8618
    private static INetAppInfo _appInfo;
#pragma warning restore CS8618
    public static void Initialize(INetAppInfo useAppInfo
#if !WINDOWS_UWP
        , Func<HttpRequestMessage, System.Security.Cryptography.X509Certificates.X509Certificate2?, System.Security.Cryptography.X509Certificates.X509Chain?, SslPolicyErrors, bool>? useServerCertificateCustomValidationCallback = null
#endif
    )
    {
        Web._appInfo = useAppInfo;
#if !WINDOWS_UWP
        Web.ServerCertificateCustomValidationCallback ??= useServerCertificateCustomValidationCallback; // we coalesce for this one since it can be initialized internally
#endif
        Web.IsInitialized = true;
    }
    public static bool IsInitialized { get; private set; }
    
    public static HttpClient GetNewHttpClient(
#if !WINDOWS_UWP
        CookieContainer? cookieContainer = null
#endif
        )
    {
#if WINDOWS_UWP
        var rootFilter = new HttpBaseProtocolFilter();
        rootFilter.AllowUI = false;
        var httpClient = new HttpClient(rootFilter);
#else

        var httpClientHandler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = ServerCertificateCustomValidationCallback,
        };

        if (cookieContainer != null)
        {
            httpClientHandler.CookieContainer = cookieContainer;
        }

        var httpClient = new HttpClient(httpClientHandler);
#endif
        httpClient.DefaultRequestHeaders.UserAgent.Clear();

        httpClient.DefaultRequestHeaders.AcceptEncoding.Clear();
        httpClient.DefaultRequestHeaders.AcceptEncoding.TryParseAdd("gzip;q=1.0");
        httpClient.DefaultRequestHeaders.AcceptEncoding.TryParseAdd("deflate;q=1.0");
        httpClient.DefaultRequestHeaders.AcceptEncoding.TryParseAdd("*;q=0.0");

        return httpClient;
    }

    public static HttpRequestMessage UpdateHttpRequestMessageUserAgent(HttpRequestMessage httpRequestMessage)
    {
        httpRequestMessage.Headers.UserAgent.Clear();
        httpRequestMessage.Headers.UserAgent.Add(_appInfo.UserAgentHeader);

        return httpRequestMessage;
    }
    [Obsolete("Should use DisposableResponseStream variants instead")]
    public static async Task<JsonRestResponse<T>> GetRestResponseFromJson<T>(HttpClient httpClient, HttpRequestMessage httpRequestMessage, string serviceName, Func<string, HttpResponseMessage, string, Task>? onErrorResponseFn = null) where T : class, new()
    {
        var restResponse = new JsonRestResponse<T>();
        var httpRequestTask = httpClient.
#if WINDOWS_UWP
                    SendRequestAsync
#else
                    SendAsync
#endif
                    (httpRequestMessage, HttpCompletionOption.ResponseContentRead);

        using var response = await httpRequestTask;
        restResponse.Status = response.StatusCode;

        // decompress it
        var compressionAlgorithm = GetCompressionAlgorithmForResponse(response);
#if WINDOWS_UWP
#if NET7_0_OR_GREATER
        await
 #endif
        using var stream = Compression.GetDecompressionStreamFor(await response.Content.ReadAsInputStreamAsync(), compressionAlgorithm);
#else
        await using var stream = Compression.GetDecompressionStreamFor(await response.Content.ReadAsStreamAsync(), compressionAlgorithm);
#endif
        using var reader = new StreamReader(stream, Encoding.UTF8);
        if (response.IsSuccessStatusCode)
        {
            restResponse.Message = await reader.ReadToEndAsync();
        }
        else
        {
            // To handle custom error messages
            if (onErrorResponseFn != null) await onErrorResponseFn.Invoke(serviceName, response, await reader.ReadToEndAsync()).ConfigureAwait(false);
        }
        return restResponse; //must be disposed by the invoker
    }

    [Obsolete("Should use DisposableResponseStream variants instead")]
    public static async Task<JsonRestResponse<Stream>> GetRestResponseFromJson(HttpClient httpClient, HttpRequestMessage httpRequestMessage, string serviceName, Func<string, HttpResponseMessage, string, Task>? onErrorResponseFn = null)
    {
        var restResponse = new JsonRestResponse<Stream>();
        var httpRequestTask = httpClient.
#if WINDOWS_UWP
                    SendRequestAsync
#else
                    SendAsync
#endif
                    (httpRequestMessage, HttpCompletionOption.ResponseContentRead);

        using var response = await httpRequestTask;
        restResponse.Status = response.StatusCode;

        // decompress it
        var compressionAlgorithm = GetCompressionAlgorithmForResponse(response);
#if WINDOWS_UWP
#if NET7_0_OR_GREATER
        await 
#endif
        using var stream = Compression.GetDecompressionStreamFor(await response.Content.ReadAsInputStreamAsync(), compressionAlgorithm);
#else
        await using var stream = Compression.GetDecompressionStreamFor(await response.Content.ReadAsStreamAsync(), compressionAlgorithm);
#endif
        if (response.IsSuccessStatusCode)
        {
            restResponse.Message = "OK";
            var ms = new MemoryStream();
            await stream.CopyToAsync(ms); // create a new one to avoid disposal
            ms.Seek(0, SeekOrigin.Begin);
            restResponse.Result = ms;
        }
        else
        {
            using var reader = new StreamReader(stream, Encoding.UTF8);

            // To handle custom error messages
            if (onErrorResponseFn != null) await onErrorResponseFn.Invoke(serviceName, response, await reader.ReadToEndAsync()).ConfigureAwait(false);
        }

        return restResponse; //must be disposed by the invoker
    }

    // it'd be nice getURLContentStream for using in-stream JSON deserialization
    public static async Task<string> GetUrlContentV2(HttpClient httpClient, HttpRequestMessage httpRequestMessage, string serviceName, Func<string, HttpResponseMessage, string, Task>? onErrorResponseFn = null)
    {
        var httpRequestTask = httpClient.
#if WINDOWS_UWP
                    SendRequestAsync
#else
                    SendAsync
#endif
                    (httpRequestMessage, HttpCompletionOption.ResponseContentRead);

        using var response = await httpRequestTask;
        // decompress it
        var compressionAlgorithm = GetCompressionAlgorithmForResponse(response);
#if WINDOWS_UWP
#if NET7_0_OR_GREATER
        await
 #endif
        using var stream = Compression.GetDecompressionStreamFor(await response.Content.ReadAsInputStreamAsync(), compressionAlgorithm);
#else
        await using var stream = Compression.GetDecompressionStreamFor(await response.Content.ReadAsStreamAsync(), compressionAlgorithm);
#endif
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var responseContent = await StreamHelper.ReadFullyAsStringAsync(stream);
        if (!response.IsSuccessStatusCode && onErrorResponseFn != null) 
        {
            await onErrorResponseFn.Invoke(serviceName, response, responseContent).ConfigureAwait(false);
        }
        return responseContent;
    }

    public sealed class DisposableResponseStream : IDisposable
#if !WINDOWS_UWP
        , IAsyncDisposable
#endif
    {
        public async Task EnsureStreamIsSeekable()
        {
            Stream = await StreamHelper.ReadFullyAsSeekableStreamAsync(Stream).ConfigureAwait(false);
        }
        public HttpResponseMessage Response { get; private set; }
        public Stream Stream
        {
            get;
            private set;
        }

        internal readonly bool IsNullStream;
        public DisposableResponseStream(HttpResponseMessage response, Stream? content = null)
        {
            Response = response;
            Stream = content ?? Stream.Null;
            IsNullStream = content == Stream.Null;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!IsNullStream)
                    Stream.Dispose();
                Response.Dispose();
            }
        }
#if !WINDOWS_UWP
        public async ValueTask DisposeAsync()
        {
            await DisposeAsync(true);
        }
        private async ValueTask DisposeAsync(bool disposing)
        {
            if (!disposing) return;
            if (!IsNullStream)
                await Stream.DisposeAsync();
            Response.Dispose();
        }
#endif
        public bool IsSuccess => (int)Response.StatusCode >= 200 && (int)Response.StatusCode < 400;
    }
    
    public static async Task<DisposableResponseStream> GetUrlContentStreamV2(HttpClient httpClient, HttpRequestMessage httpRequestMessage)
    {
        var httpRequestTask = httpClient.
#if WINDOWS_UWP
            SendRequestAsync
#else
            SendAsync
#endif
            (httpRequestMessage);
        HttpResponseMessage? response = null;
        Stream? responseContent = null;
        try
        {
            response = await httpRequestTask;
#if WINDOWS_UWP
            responseContent = (await response.Content.ReadAsInputStreamAsync()).AsStreamForRead();
#else
            responseContent = await response.Content.ReadAsStreamAsync();
#endif
            // decompress it
            var compressionAlgorithm = GetCompressionAlgorithmForResponse(response);
            responseContent = Compression.GetDecompressionStreamFor(new BufferedStream(responseContent, 16 * 1024), compressionAlgorithm);
        }
        catch
        {
            response?.Dispose();
            if (responseContent != null)
            {
#if NET7_0_OR_GREATER
                await responseContent.DisposeAsync();
#else
                responseContent.Dispose();
#endif
            }
            throw;
        }
        return new DisposableResponseStream(response, responseContent);
    }

    public static async Task<T?> GetUrlJsonAs<T>(HttpClient httpClient, HttpRequestMessage httpRequestMessage) where T : class, new()
    {
#if !WINDOWS_UWP
        await 
#endif
        using var drs = await GetUrlContentStreamV2(httpClient, httpRequestMessage);
        if (drs.IsNullStream) return null;
        
        using var streamReader = new StreamReader(drs.Stream);
#if NET7_0_OR_GREATER
        await
#endif
        using var jsonTextReader = new JsonTextReader(streamReader);
        var jsonSerializer = new JsonSerializer();
        return jsonSerializer.Deserialize<T>(jsonTextReader);
    }

    public static async Task<RestResponse<string>> ResolveRequestAsStringRestResponse(HttpClient httpClient, HttpRequestMessage httpRequestMessage)
    {
#if !WINDOWS_UWP
        await 
#endif
        using var drs = await GetUrlContentStreamV2(httpClient, httpRequestMessage);
        await drs.EnsureStreamIsSeekable().ConfigureAwait(false);
        if (!drs.IsNullStream)
        {
            using var streamReader = new StreamReader(drs.Stream);
#if NET7_0_OR_GREATER
            await 
#endif
            using var jsonTextReader = new JsonTextReader(streamReader);
            // compatibility with old format
            try
            {
                var jsonSerializer = new JsonSerializer();
                var response = jsonSerializer.Deserialize<RestResponse<string>>(jsonTextReader);
                if (response != null && (int)response.Status != 0)
                    return response;
            }
            catch
            {
                // will retry
            }
            try
            {
                drs.Stream.Seek(0, SeekOrigin.Begin);
                if (!drs.IsSuccess)
                    return new RestResponse<string>
                    {
                        Status = drs.Response.StatusCode,
                        Error = await StreamHelper.ReadFullyAsStringAsync(drs.Stream),
                    };
                return new RestResponse<string>
                {
                    Status = drs.Response.StatusCode,
                    Result = await StreamHelper.ReadFullyAsStringAsync(drs.Stream),
                };
            }
            catch
            {
                // failed
            }
        }
        return new RestResponse<string>
        {
            Status = drs.Response.StatusCode
        };
    }
    
    public static async Task<RestResponse<T>> ResolveRequestAsRestResponse<T>(HttpClient httpClient, HttpRequestMessage httpRequestMessage) where T : class, new()
    {
#if !WINDOWS_UWP
        await 
#endif 
        using var drs = await GetUrlContentStreamV2(httpClient, httpRequestMessage);
        await drs.EnsureStreamIsSeekable().ConfigureAwait(false);

        // compatibility with old format
        var jsonSerializer = new JsonSerializer();
        if (!drs.IsNullStream)
        {
            using var streamReader1 = new StreamReader(drs.Stream);
#if NET7_0_OR_GREATER
            await
 #endif
            using var jsonTextReader1 = new JsonTextReader(streamReader1);
            Exception? firstException = null;
            try
            {
                var response = jsonSerializer.Deserialize<RestResponse<T>>(jsonTextReader1);
                if (response != null && (int)response.Status != 0)
                    return response;
            }
            catch (Exception ex)
            {
                // will retry next
                firstException = ex;
            }
            // compatibility with new format
            try
            {
                if (!drs.IsSuccess)
                    return new RestResponse<T>
                    {
                        Status = drs.Response.StatusCode,
                        Error = await StreamHelper.ReadFullyAsStringAsync(drs.Stream),
                    };
                drs.Stream.Seek(0, SeekOrigin.Begin);
                using var streamReader2 = new StreamReader(drs.Stream);
#if NET7_0_OR_GREATER
                await
#endif
                using var jsonTextReader2 = new JsonTextReader(streamReader2);
                return new RestResponse<T>
                {
                    Status = drs.Response.StatusCode,
                    Result = jsonSerializer.Deserialize<T>(jsonTextReader2),
                };
            }
            catch(Exception ex)
            {
                // fail
                Log.Error(firstException, "Failed to parse response (first attempt)");
                Log.Error(ex, "Failed to parse response (second attempt)");
            }
        }
        return new RestResponse<T>
        {
            Status = drs.Response.StatusCode
        };
    }
    
    public static async Task<RestResponse> ResolveRequestAsRestResponse(HttpClient httpClient, HttpRequestMessage httpRequestMessage)
    {
#if !WINDOWS_UWP
        await 
#endif
        using var drs = await GetUrlContentStreamV2(httpClient, httpRequestMessage);
        await drs.EnsureStreamIsSeekable().ConfigureAwait(false);
        if (!drs.IsNullStream)
        {
            using var streamReader = new StreamReader(drs.Stream);
#if NET7_0_OR_GREATER
            await 
#endif
            using var jsonTextReader = new JsonTextReader(streamReader);
            // compatibility with old format
            try
            {
                var jsonSerializer = new JsonSerializer();
                var response = jsonSerializer.Deserialize<RestResponse>(jsonTextReader);
                if (response != null && (int)response.Status != 0)
                    return response;
            }
            catch
            {
                // will retry
            }
            try
            {
                drs.Stream.Seek(0, SeekOrigin.Begin);
                if (!drs.IsSuccess)
                    return new RestResponse
                    {
                        Status = drs.Response.StatusCode,
                        Error = await StreamHelper.ReadFullyAsStringAsync(drs.Stream),
                    };
                return new RestResponse
                {
                    Status = drs.Response.StatusCode,
                    Message = await StreamHelper.ReadFullyAsStringAsync(drs.Stream),
                };
            }
            catch
            {
                // failed
            }
        }
        return new RestResponse
        {
            Status = drs.Response.StatusCode
        };
    }

    [Obsolete("This is a legacy implementation which shouldn't be used anymore")]
    public static async Task<DisposableResponseStream> PostItemToUrl<T>(Uri uri, T item, bool decompress = true)
    {
        var json = JsonConvert.SerializeObject(item);
        var httpClient = GetNewHttpClient(); // this is a bad practice, but won't get fixed as this code should be deleted instead
#if WINDOWS_UWP
        var httpRequestTask = httpClient.PostAsync(uri, new HttpStringContent(json, Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/json"));
#else
        var httpRequestTask = httpClient.PostAsync(uri, new StringContent(json, Encoding.UTF8, "application/json"));
#endif
        HttpResponseMessage? response = null;
        Stream? responseContent = null;
        try
        {
            response = await httpRequestTask;
            // decompress it
            var compressionAlgorithm = decompress ? GetCompressionAlgorithmForResponse(response) : CompressionAlgorithm.None;
#if WINDOWS_UWP
            var stream = Compression.GetDecompressionStreamFor(new BufferedStream((await response.Content.ReadAsInputStreamAsync()).AsStreamForRead(), 16 * 1024), compressionAlgorithm);
#else
            var stream = Compression.GetDecompressionStreamFor(new BufferedStream(await response.Content.ReadAsStreamAsync(), 16 * 1024), compressionAlgorithm);
#endif
            responseContent = stream;
        }
        catch
        {
            response?.Dispose();
            if (responseContent != null)
            {
#if NET7_0_OR_GREATER
                await responseContent.DisposeAsync();
#else 
                responseContent.Dispose();
#endif
            }
            throw;
        }
        return new DisposableResponseStream(response, responseContent);
    }

    public static CompressionAlgorithm GetCompressionAlgorithmForResponse(HttpResponseMessage response)
    {
        var compressionAlgorithm = CompressionAlgorithm.None;
#if WINDOWS_UWP
        foreach (var contentCoding in response.Content.Headers.ContentEncoding.Select(contentEncoding => contentEncoding.ContentCoding))
            if (contentCoding.Contains("gzip"))
                compressionAlgorithm = CompressionAlgorithm.GZip;
            else if (contentCoding.Contains("deflate"))
                compressionAlgorithm = CompressionAlgorithm.Deflate;
#else
        if (response.Content.Headers.ContentEncoding.Contains("gzip"))
            compressionAlgorithm = CompressionAlgorithm.GZip;
        else if (response.Content.Headers.ContentEncoding.Contains("deflate"))
            compressionAlgorithm = CompressionAlgorithm.Deflate;
#endif
        return compressionAlgorithm;
    }

    /// <summary>
    /// Submits a test request to check if a service is online or not.
    /// </summary>
    /// <param name="httpClient">HttpClient to send the request from.</param>
    /// <param name="httpRequestMessage">HttpRequestMessage to send for the test.</param>
    /// <returns>True if succeeded, false if failed.</returns>
    public static async Task<(bool IsSuccess, bool IsTimeout, Exception? Exception)> SendTestRequest(HttpClient httpClient, HttpRequestMessage httpRequestMessage)
    {
        // Should not need progress nor anything else, but it could be reasonable to return a boolean instead
        try
        {
#if WINDOWS_UWP
            var responseTask = httpClient.SendRequestAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead).AsTask();
#else
            var responseTask = httpClient.SendAsync(httpRequestMessage);
#endif
            var timeoutTask = Task.Delay(10000);
            await Task.WhenAny( responseTask, timeoutTask);
            if (timeoutTask.IsCompleted)
            {
                return (false, true, null);
            }

            using var response = await responseTask.ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Failed to send test request");
            return (false, false, ex);
        }
        return (true, false, null);
    }
    public static async Task<(bool IsSuccess, bool IsTimeout, Exception? Exception, TimeSpan FullfillmentTime)> SendTestRequestAndBench(HttpClient httpClient, HttpRequestMessage httpRequestMessage)
    {
        var stopWatch = Stopwatch.StartNew();
        var result = await SendTestRequest(httpClient, httpRequestMessage);
        stopWatch.Stop();
        return (result.IsSuccess, result.IsTimeout, result.Exception, stopWatch.Elapsed);
    }

    public static async Task<DisposableResponseStream> ResolveHttpRequest(HttpClient httpClient, HttpRequestMessage request, bool fetchBody = false)
    {
        HttpResponseMessage? response = null;
        Stream? responseContent = null;
        try
        {
#if WINDOWS_UWP
            response = await httpClient.SendRequestAsync(request, fetchBody ? HttpCompletionOption.ResponseContentRead : HttpCompletionOption.ResponseHeadersRead);
#else
            response = await httpClient.SendAsync(request, fetchBody ? HttpCompletionOption.ResponseContentRead : HttpCompletionOption.ResponseHeadersRead);
#endif

            if (fetchBody)
            {
                var compressionAlgorithm = GetCompressionAlgorithmForResponse(response);
#if WINDOWS_UWP
                var stream = Compression.GetDecompressionStreamFor(new BufferedStream((await response.Content.ReadAsInputStreamAsync()).AsStreamForRead(), 16 * 1024), compressionAlgorithm);
#else
                var stream = Compression.GetDecompressionStreamFor(new BufferedStream(await response.Content.ReadAsStreamAsync(), 16 * 1024), compressionAlgorithm);
#endif
                responseContent = stream;
            }
        }
        catch
        {
            response?.Dispose();
            if (responseContent != null)
            {
#if NET7_0_OR_GREATER
                await responseContent.DisposeAsync();
#else
                responseContent.Dispose();
#endif
            }
            throw;
        }
        return new DisposableResponseStream(response, responseContent);
    }

    public static async Task<T?> ResolveHttpRequestAndParseJson<T>(HttpClient httpClient, HttpRequestMessage httpRequestMessage) where T : class, new()
    {
#if !WINDOWS_UWP
        await 
#endif
        using var drs = await ResolveHttpRequest(httpClient, httpRequestMessage, true);
        if (drs.IsNullStream) return null;
        using var streamReader = new StreamReader(drs.Stream);
#if NET7_0_OR_GREATER
        await
#endif
        using var jsonTextReader = new JsonTextReader(streamReader);
        var jsonSerializer = new JsonSerializer();

        return jsonSerializer.Deserialize<T>(jsonTextReader);
    }
}
