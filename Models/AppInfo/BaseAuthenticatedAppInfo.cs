namespace KinoshitaProductions.Common.Models.AppInfo;

#if WINDOWS_UWP
using Windows.Web.Http;
using HttpRequestMessage = Windows.Web.Http.HttpRequestMessage;
using AuthenticationHeaderValue = Windows.Web.Http.Headers.HttpCredentialsHeaderValue;
using HttpMethod = Windows.Web.Http.HttpMethod;
#else
using System.Net.Http.Headers;
#endif

using KinoshitaProductions.Common.Interfaces.AppInfo;
using System;
using System.Security.Authentication;
using System.Security;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;

public class BaseAuthenticatedAppInfo : BaseServiceAppInfo, IJwtAuthenticatedServiceAppInfo, IBasicAuthenticatedServiceAppInfo
{
    private string? _elevatedJwtToken;
    private string? _appJwtToken;
    private string? _sessionJwtToken;
    private string? _basicToken;
    public AuthenticationType AuthenticationTypeSet { get; private set; }

    public async Task ClearAuthenticationCredentials(bool deletePersistedCredentials = true)
    {
        if (deletePersistedCredentials)
        {
            var filePresence = await SettingsManager.ExistsAsync("___adt");
            if (filePresence != FilePresence.NotFound)
                await SettingsManager.DeleteAsync("___adt", filePresence);
        }

        switch (AuthenticationTypeSet)
        {
            case AuthenticationType.Basic:
                _basicToken = null;
                break;
            case AuthenticationType.Jwt:
                _elevatedJwtToken = null;
                _appJwtToken = null;
                _sessionJwtToken = null;
                break;
        }

        AuthenticationTypeSet = AuthenticationType.None;
    }
    private void ValidateRequestTo(Uri requestUri)
    {
        // check that the request is going to be performed through HTTPS
        if (requestUri.Scheme != "https")
        {
            throw new SecurityException("Denied creation of an authenticated request to a non-HTTPS host.");
        }

        // check that the request is either heading to the proper domain or a subdomain within it
        if (requestUri.Host != SiteUri.Host && !requestUri.Host.EndsWith($"." + SiteUri.Host))
        {
            // if still didn't match, remove the subdomain word and check if it matches the target
            var domainComponents = requestUri.Host.Split(".");
            var topDomain = requestUri.Host.Substring(domainComponents[0].Length + 1);
            if (domainComponents.Length < 3 || !(SiteUri.Host.EndsWith(topDomain) && (SiteUri.Host.Length <= topDomain.Length 
                || SiteUri.Host[SiteUri.Host.Length - topDomain.Length - 1] == '.' // if it's another subdomain, validate this one
            )))
            {
                throw new SecurityException("Denied creation of request to an unknown host (not matching expected host pattern).");
            }
        }

        if (AuthenticationTypeSet == AuthenticationType.None)
        {
            throw new AuthenticationException("No authentication credentials set, unable to create authenticated request.");
        }
    }
    private string? GetJwtTokenOfKind(JwtTokenKind jwtTokenKind) => 
        jwtTokenKind switch {
            JwtTokenKind.Elevated => _elevatedJwtToken,
            JwtTokenKind.App => _appJwtToken,
            JwtTokenKind.Session => _sessionJwtToken,
            _ => null
        };
    private void SetAuthenticationHeaders(HttpRequestMessage requestMessage, JwtTokenKind jwtTokenKind)
    {
        requestMessage.Headers.UserAgent.Clear();
        requestMessage.Headers.UserAgent.Add(UserAgentHeader);
        requestMessage.Headers.AcceptEncoding.Clear();
        // for security reasons, Auth requests only allow no compression (VORACLE, etc.)
        requestMessage.Headers.AcceptEncoding.TryParseAdd("identity");

        // set auth credential
        switch (AuthenticationTypeSet)
        {
            case AuthenticationType.Basic:
                if (_basicToken != null) {
                    requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Basic", _basicToken);
                }
                break;
            case AuthenticationType.Jwt:
                var jwtToken = GetJwtTokenOfKind(jwtTokenKind);
                if (jwtToken != null)
                    requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwtToken);
                break;
        }
    }

    public HttpRequestMessage GetAuthenticatedHttpRequestTo(Uri requestUri, JwtTokenKind jwtTokenKind = JwtTokenKind.Session)
    {
        ValidateRequestTo(requestUri);

        var requestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            Content = null,
            RequestUri = requestUri,
        };

        SetAuthenticationHeaders(requestMessage, jwtTokenKind);

        return requestMessage;
    }
 
    public HttpRequestMessage PostAuthenticatedHttpRequestTo<T>(Uri requestUri, T content, JwtTokenKind jwtTokenKind = JwtTokenKind.Session)
    {
        ValidateRequestTo(requestUri);

        var json = JsonConvert.SerializeObject(content);
        var requestMessage = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
#if WINDOWS_UWP
            Content = new HttpStringContent(json, Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/json"),
#else
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
#endif
            RequestUri = requestUri,
        };

        SetAuthenticationHeaders(requestMessage, jwtTokenKind);

        return requestMessage;
    }
    
    public void SetBasicAuthenticationCredentials(string username, string password)
    {
        _basicToken = Convert.ToBase64String(Encoding.ASCII.GetBytes(username + ":" + password));
        AuthenticationTypeSet = AuthenticationType.Basic;
    }
  
    public void SetJwtAuthenticationCredentials(string? elevatedToken, string? appToken, string? sessionToken)
    {
        _elevatedJwtToken = elevatedToken;
        _appJwtToken = appToken;
        _sessionJwtToken = sessionToken;
        AuthenticationTypeSet = AuthenticationType.Jwt;
    }
}
