// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace KinoshitaProductions.Common.Models;

#if ANDROID
using Android.App;
using Android.Content;
#endif

public class FileManagerOptions
{
    public Func<bool> SettingsLoadedSuccessfullyFn { get; set; } = () => true;
    public Action<string> NotifyStorageEventFn { get; set; } = (_) => {};
    public Action<string> ChangeToFallbackStorageFn { get; set; } = (_) => {};
    public Func<string?> GetNextStoragePathFn { get; set; } = () => null;
#if ANDROID
    public Action<List<StorageDeviceInfo>> NotifyStorageDeviceInfosUpdatedFn { get; set; } = (_) => {};
    public Func<bool> SaveMediaToDownloadsFolderFn { get; set; } = () => false;
    public Func<StorageDeviceInfo, bool> CheckIfIsDefaultStorageFn { get; set; } = (_) => false;
    public Func<Activity?> GetCurrentActivityFn { get; set; } = () => null;
    public Func<Context?> GetCurrentContextFn { get; set; } = () => null;
#endif
#if WINDOWS_UWP
    public Func<string?> GetStorageTokenFn { get; set; } = () => null;
#endif
}