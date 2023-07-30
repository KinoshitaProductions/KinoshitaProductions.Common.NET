namespace KinoshitaProductions.Common.Helpers;

public static class TimeHelper
{
    public static DateTime UnixTimeStampToDateTime(int unixTimeStamp)
#if NET7_0_OR_GREATER
        => DateTime.UnixEpoch.AddSeconds(unixTimeStamp).ToLocalTime();
#else
        => new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(unixTimeStamp).ToLocalTime();
#endif
}
