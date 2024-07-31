namespace KinoshitaProductions.Common.Interfaces.AppInfo;

#if WINDOWS_UWP
using Windows.Web.Http;
#endif

public interface IAuthenticatedServiceAppInfo : IServiceAppInfo
{
    /// <summary>
    /// If authentication credentials has been set, this field will reflect the type.
    /// </summary>
    AuthenticationType AuthenticationTypeSet { get; }

    Task ClearAuthenticationCredentials(bool deletePersistedCredentials = true);

    /// <summary>
    /// Sends a HTTP request including authentication details (such as token, or Basic Auth headers).
    /// </summary>
    /// <param name="requestUri"></param>
    /// <param name="jwtTokenKind"></param>
    /// <returns></returns>
    HttpRequestMessage GetAuthenticatedHttpRequestTo(Uri requestUri,
        JwtTokenKind jwtTokenKind = JwtTokenKind.Session);

    HttpRequestMessage PostAuthenticatedHttpRequestTo<T>(Uri requestUri, T content,
        JwtTokenKind jwtTokenKind = JwtTokenKind.Session);
}
