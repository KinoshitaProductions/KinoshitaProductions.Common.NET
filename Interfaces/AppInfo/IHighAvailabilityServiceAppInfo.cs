namespace KinoshitaProductions.Common.Interfaces.AppInfo;

/// <summary>
/// Extends <see cref="INetAppInfo"/> by adding a callback 
/// </summary>
public interface IHighAvailabilityServiceAppInfo : IServiceAppInfo
{
    /// <summary>
    /// Checks and updates the ApiUrl with the most proper one available.
    /// For example: deciding between a main or a backup URL.
    /// </summary>
    /// <returns></returns>
    Task RevalidateEndpoints();
}
