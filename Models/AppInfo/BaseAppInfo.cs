namespace KinoshitaProductions.Common.Models.AppInfo;

using KinoshitaProductions.Common.Interfaces.AppInfo;

/// <summary>
/// This class provides a basic and overridable implementation of <see cref="Interfaces.AppInfo.IAppInfo"/>.
/// </summary>
public class BaseAppInfo : IAppInfo
{
#if WINDOWS_UWP
    public string DeviceId => "UWP"; // NOT YET IMPLEMENTED DUE TO DIFFICULTIES
#elif ANDROID
    public string? GetDeviceId(Android.Content.ContentResolver contentResolver) => Android.Provider.Settings.Secure.GetString(contentResolver, Android.Provider.Settings.Secure.AndroidId);
#else
    public virtual string DeviceId => "NO-DEVICE-ID";
#endif
    public virtual string AppName => "No App Name";
    // ReSharper disable once MemberCanBeProtected.Global
    public string AppNameWithoutSpaces => AppName.Replace(" ", "", StringComparison.Ordinal);
    private string? _appVersion;
    public virtual string AppVersion
    {
        get
        {
            if (_appVersion == null)
            {
#if WINDOWS_UWP
                var package = Windows.ApplicationModel.Package.Current;
                var packageId = package.Id;
                var version = packageId.Version;

                _appVersion = string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}.{1}.{2}.{3}", version.Major, version.Minor, version.Build, version.Revision);
#elif ANDROID
                var context = Application.Context;
                if (context.PackageName != null) 
                {
                    #pragma warning disable CS0618
                    _appVersion = (context.PackageManager?.GetPackageInfo(context.PackageName, 0)?.VersionName ?? "0.0.0.0");
                    #pragma warning restore CS0618
                }
                else 
                {
                   _appVersion = "0.0.0.0"; 
                }
#else
                _appVersion = "0.0.0.0";
#endif
            }
            return _appVersion;
        }
    }
    public virtual string AppVersionSuffix => "No Version Suffix";
#if WINDOWS_UWP
    public virtual string AppPlatform => "Windows";
#elif ANDROID
    public virtual string AppPlatform => "Android";
#else
    public virtual string AppPlatform => "UnknownPlatform";
#endif
}
