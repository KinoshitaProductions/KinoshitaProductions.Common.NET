#if __ANDROID__ || WINDOWS_UWP
namespace KinoshitaProductions.Common.Helpers;

public static class MetricsHelper
{
    private static int _availableRamInMb;
    private static int _maxAvailableRamInMb;
    private static DateTime? _lastAvailableRamRenewal;
    // ReSharper disable once MemberCanBePrivate.Global
    public static int AvailableRamInMb
    {
        get
        {
            if (_lastAvailableRamRenewal.HasValue && _availableRamInMb > 0 && _lastAvailableRamRenewal.Value > DateTime.Now)
                return _availableRamInMb;
#if __ANDROID__
            // scan for memory usage every 10 seconds tops
            var activityManager = (ActivityManager?) Application.Context.GetSystemService( Android.Content.Context.ActivityService);
            if (activityManager == null) return -1; // couldn't get
            ActivityManager.MemoryInfo memoryInfo = new ActivityManager.MemoryInfo();
            activityManager.GetMemoryInfo(memoryInfo);

            var availableRam = memoryInfo.TotalMem - memoryInfo.AvailMem;
            var calculatedAvailableRamInMb = (int)(availableRam / 1024L / 1024L / 2L);
            calculatedAvailableRamInMb /= 2; // reduce to 50% to avoid overuse
            calculatedAvailableRamInMb = Math.Min(80, calculatedAvailableRamInMb); // Set up hard limit of 80MB RAM
            _availableRamInMb = calculatedAvailableRamInMb;
            _lastAvailableRamRenewal = DateTime.Now.AddSeconds(10);
            return _availableRamInMb;
#elif WINDOWS_UWP
            var calculatedAvailableRamInMb = Math.Min((int)((Windows.System.MemoryManager.AppMemoryUsageLimit - Windows.System.MemoryManager.AppMemoryUsage) / (1 * 1024 * 1024)), MaxAvailableRamInMb);
            _availableRamInMb = calculatedAvailableRamInMb;
            _lastAvailableRamRenewal = DateTime.Now.AddSeconds(10);
            return calculatedAvailableRamInMb;
#endif
        }
    }
    // ReSharper disable once MemberCanBePrivate.Global
    public static int MaxAvailableRamInMb
    {
        get
        {
            if (_maxAvailableRamInMb == 0)
            {
                //measure available RAM
#if __ANDROID__
                var activityManager = (ActivityManager?)Application.Context.GetSystemService(Android.Content.Context.ActivityService);
                if (activityManager == null) return -1; // couldn't get
                ActivityManager.MemoryInfo memoryInfo = new ActivityManager.MemoryInfo();
                activityManager.GetMemoryInfo(memoryInfo);

                var totalRam = memoryInfo.TotalMem - memoryInfo.AvailMem;
                _maxAvailableRamInMb = (int)(totalRam / 1024L / 1024L / 2L); // Reduce RAM required for tags //the original size comes in bytes, divided in 2 to leave half of RAM to system
#elif WINDOWS_UWP
                _maxAvailableRamInMb = (int)(Windows.System.MemoryManager.AppMemoryUsageLimit / (1 * 1024 * 1024));
#endif
            }
            return _maxAvailableRamInMb; // this is a stub value
        }
    }
}
#endif