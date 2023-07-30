namespace KinoshitaProductions.Common.Interfaces.AppInfo;

public interface IBasicAuthenticatedServiceAppInfo : IAuthenticatedServiceAppInfo
{
    void SetBasicAuthenticationCredentials(string username, string password);
}
