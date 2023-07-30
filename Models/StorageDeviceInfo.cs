// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
#if ANDROID
namespace KinoshitaProductions.Common.Models;

// this feature is currently only supported in Android
public record StorageDeviceInfo
{
#pragma warning disable CS8618
    public string Name { get; init; }
    public string Path { get; init; }
    public string FallbackPath { get; init; }
    public string DisplayPath { get; init; }
#pragma warning restore CS8618
    public long UsedSpaceBytes { get; init; }
    public long TotalSpaceBytes { get; init; }
    public bool IsDefault { get; internal set; }
    // ReSharper disable once PossibleLossOfFraction
    public decimal UsagePercentage => TotalSpaceBytes > 0 ? UsedSpaceBytes / TotalSpaceBytes : -1.0M;
}
#endif
