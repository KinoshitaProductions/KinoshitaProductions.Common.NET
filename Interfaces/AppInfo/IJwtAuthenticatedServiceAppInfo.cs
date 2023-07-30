namespace KinoshitaProductions.Common.Interfaces.AppInfo;

public interface IJwtAuthenticatedServiceAppInfo : IAuthenticatedServiceAppInfo
{
    void SetJwtAuthenticationCredentials(string? elevatedToken, string appToken, string? sessionToken);
}
