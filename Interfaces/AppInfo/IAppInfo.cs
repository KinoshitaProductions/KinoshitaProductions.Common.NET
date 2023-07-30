namespace KinoshitaProductions.Common.Interfaces.AppInfo;

/// <summary>
/// Interface with the basic data to identify an app instance being executed.
/// </summary>
public interface IAppInfo
{
    /// <summary>
    /// Gets the DeviceId in case the app needs it to identify the user's device.
    /// </summary>
#if ANDROID
string? GetDeviceId(Android.Content.ContentResolver contentResolver);
#else
    string DeviceId { get; }
#endif
    /// <summary>
    /// Gets the app name (with spaces).
    /// </summary>
    string AppName { get; }

    /// <summary>
    /// Gets the app version (e.g. 1.0.0.0).
    /// </summary>
    string AppVersion { get; }

    /// <summary>
    /// Optional suffix for the build (e.g. Debug/Canary).
    /// </summary>
    string AppVersionSuffix { get; }

    /// <summary>
    /// Platform's name where the app is being executed.
    /// </summary>
    string AppPlatform { get; }
}
