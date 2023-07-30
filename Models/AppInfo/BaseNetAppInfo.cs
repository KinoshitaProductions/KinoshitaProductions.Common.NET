namespace KinoshitaProductions.Common.Models.AppInfo;

#if WINDOWS_UWP
using Windows.Web.Http;
using HttpRequestMessage = Windows.Web.Http.HttpRequestMessage;
using HttpProductInfoHeaderValue = Windows.Web.Http.Headers.HttpProductInfoHeaderValue;
#else
using System.Net.Http.Headers;
#endif
using KinoshitaProductions.Common.Interfaces.AppInfo;
using Newtonsoft.Json;
using static Web;

/// <summary>
/// This class provides a basic and overridable implementation of <see cref="Interfaces.AppInfo.INetAppInfo"/>.
/// </summary>
public class BaseNetAppInfo : BaseAppInfo, INetAppInfo
{
    private HttpClient? _httpClient;
    public HttpClient HttpClient
    {
        get
        {
            if (_httpClient == null)
            {
                // initialize here if not yet initialized
                if (!Web.IsInitialized)
                    Web.Initialize(this);

                _httpClient = Web.GetNewHttpClient();
#if !WINDOWS_UWP
                _httpClient.Timeout = TimeSpan.FromSeconds(24); // It should never take longer than this (previously 10 seconds, but increased due to slow networks)
#endif
            }
            return _httpClient;
        }
    }
    private string? _userAgent;
    public string UserAgent =>
        _userAgent ??= string.IsNullOrWhiteSpace(AppPlatform)
            ? AppNameWithoutSpaces
            : string.Join(".", AppNameWithoutSpaces, AppPlatform) + "/" + AppVersion;

#if WINDOWS_UWP
    private HttpProductInfoHeaderValue? _userAgentHeader;
    public HttpProductInfoHeaderValue UserAgentHeader =>
        _userAgentHeader ??= new HttpProductInfoHeaderValue(string.IsNullOrWhiteSpace(AppPlatform) 
        ?  AppNameWithoutSpaces 
        : string.Join(".", AppNameWithoutSpaces, AppPlatform), AppVersion);
#else
    private ProductInfoHeaderValue? _userAgentHeader;
    public ProductInfoHeaderValue UserAgentHeader =>
        _userAgentHeader ??= new ProductInfoHeaderValue(
            string.IsNullOrWhiteSpace(AppPlatform)
                ? AppNameWithoutSpaces
                : string.Join(".", AppNameWithoutSpaces, AppPlatform), AppVersion);
#endif
    /// <summary>
    /// This function may be called if for some reason the user agent should be recalculated (e.g. a site requires to include username, or a configuration caused it to change)
    /// </summary>
    public void RefreshUserAgent()
    {
        _userAgent = null;
        _userAgentHeader = null;
    }
    private void SetRequestHeaders(HttpRequestMessage requestMessage, bool secure = true)
    {
        requestMessage.Headers.UserAgent.Clear();
        requestMessage.Headers.UserAgent.Add(UserAgentHeader);
        requestMessage.Headers.AcceptEncoding.Clear();
        // to avoid vulnerabilities such as VORACLE, secure requests always go with identity
        if (secure)
        {
            requestMessage.Headers.AcceptEncoding.TryParseAdd("identity");
        }
        else
        {
            requestMessage.Headers.AcceptEncoding.TryParseAdd("gzip;q=1.0");
            requestMessage.Headers.AcceptEncoding.TryParseAdd("deflate;q=1.0");
            requestMessage.Headers.AcceptEncoding.TryParseAdd("*;q=0.0");
        }
    }
    private HttpRequestMessage GetHttpRequestTo(Uri requestUri, bool secure)
    {
        var requestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            Content = null,
            RequestUri = requestUri
        };

        SetRequestHeaders(requestMessage, secure: secure);

        return requestMessage;
    }
    public Task<DisposableResponseStream> ResolveHttpRequest(HttpRequestMessage request, bool fetchBody = true) => Web.ResolveHttpRequest(HttpClient, request, fetchBody);
    public Task<T?> ResolveHttpRequestAndParseJson<T>(HttpRequestMessage request) where T : class, new() => Web.ResolveHttpRequestAndParseJson<T>(HttpClient, request);
    public HttpRequestMessage GetHttpRequestTo(Uri requestUri) => GetHttpRequestTo(requestUri, secure: true);
    public HttpRequestMessage GetInsecureHttpRequestTo(Uri requestUri) => GetHttpRequestTo(requestUri, secure: false);

    private HttpRequestMessage PostHttpRequestTo<T>(Uri requestUri, T content, bool secure)
    {
        var json = JsonConvert.SerializeObject(content);
        var requestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
#if WINDOWS_UWP
            Content = new HttpStringContent(json, Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/json"),
#else
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json"),
#endif
            RequestUri = requestUri,
        };

        SetRequestHeaders(requestMessage, secure: secure);

        return requestMessage;
    }
    public HttpRequestMessage PostHttpRequestTo<T>(Uri requestUri, T content) => PostHttpRequestTo(requestUri, content, secure: true);
    public HttpRequestMessage PostInsecureHttpRequestTo<T>(Uri requestUri, T content) => PostHttpRequestTo(requestUri, content, secure: false);
}
