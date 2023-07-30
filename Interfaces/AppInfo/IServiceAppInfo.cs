namespace KinoshitaProductions.Common.Interfaces.AppInfo;

/// <summary>
/// Extends <see cref="INetAppInfo"/> by adding an API url for app's services.
/// </summary>
public interface IServiceAppInfo : INetAppInfo
{
    /// <summary>
    /// Gets the main service URL for the app services.
    /// </summary>
    string ApiUrl { get; }

    /// <summary>
    /// Gets the main service <see cref="Uri"/> for the app services.
    /// </summary>
    Uri ApiUri { get; }

    /// <summary>
    /// Gets the main URL for app information.
    /// </summary>
    string SiteUrl { get; }

    /// <summary>
    /// Gets the main <see cref="Uri"/> for the app information.
    /// </summary>
    Uri SiteUri { get; }
}
