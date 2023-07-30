namespace KinoshitaProductions.Common.Models.AppInfo;

using KinoshitaProductions.Common.Interfaces.AppInfo;

public class BaseServiceAppInfo : BaseNetAppInfo, IServiceAppInfo
{
    public virtual string ApiUrl => "NO-API-URL";
    public virtual Uri ApiUri => new Uri(ApiUrl);
    public virtual string SiteUrl => "NO-SITE-URL";
    public virtual Uri SiteUri => new Uri(SiteUrl);
}
