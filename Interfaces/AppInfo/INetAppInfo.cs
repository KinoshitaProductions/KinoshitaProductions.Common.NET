namespace KinoshitaProductions.Common.Interfaces.AppInfo;

#if WINDOWS_UWP
using HttpClient = Windows.Web.Http.HttpClient;
using HttpRequestMessage = Windows.Web.Http.HttpRequestMessage;
using ProductInfoHeaderValue = Windows.Web.Http.Headers.HttpProductInfoHeaderValue;
#else
using System.Net.Http.Headers;
#endif

/// <summary>
/// Extends IAppInfo, by including data for HttpClient usage, as well a slot for a global HttpClient.
/// </summary>
public interface INetAppInfo : IAppInfo
{
    HttpClient HttpClient { get; }
    HttpRequestMessage GetHttpRequestTo(Uri requestUri);
    HttpRequestMessage GetInsecureHttpRequestTo(Uri requestUri);
    HttpRequestMessage PostHttpRequestTo<T>(Uri requestUri, T content);
    HttpRequestMessage PostInsecureHttpRequestTo<T>(Uri requestUri, T content);
    string UserAgent { get; }
    ProductInfoHeaderValue UserAgentHeader { get; }

    /// <summary>
    /// This function may be called if for some reason the user agent should be recalculated (e.g. a site requires to include username, or a configuration caused it to change)
    /// </summary>
    void RefreshUserAgent();
}
