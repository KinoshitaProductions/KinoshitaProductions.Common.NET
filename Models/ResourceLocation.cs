namespace KinoshitaProductions.Common.Models;

using Newtonsoft.Json;
using KinoshitaProductions.Common.Interfaces.AppInfo;
using System;
using System.Threading.Tasks;

[JsonObject(MemberSerialization = MemberSerialization.OptIn)]
public class ResourceLocation
{
    [JsonProperty("t")]
    public ResourceLocationTypes Types { get; set; }
    [JsonProperty("a")]
    public AppFolder AppFolder { get; set; }
    [JsonProperty("l")]
    public string? LocalPath { get; set; }
    [JsonProperty("s")]
    public string? SettingsPath{ get; set; }
    [JsonProperty("r")]
    public string? RemotePath { get; set; }
    public async Task<ResourceLocationTypes> ExistsAt(bool testRemote = false, INetAppInfo? appInfo = null)
    {
        if (Types == ResourceLocationTypes.None) return ResourceLocationTypes.None;
        if (Types.HasFlag(ResourceLocationTypes.Local) && LocalPath != null && await FileManager.ExistsAsync(AppFolder, LocalPath).ConfigureAwait(false)) return ResourceLocationTypes.Local;
        if (Types.HasFlag(ResourceLocationTypes.Settings) && SettingsPath != null && await FileManager.ExistsAsync(AppFolder.Settings, SettingsPath).ConfigureAwait(false)) return ResourceLocationTypes.Settings;
        if (!testRemote || !Types.HasFlag(ResourceLocationTypes.Remote) || RemotePath == null) return ResourceLocationTypes.None;
        if (appInfo == null)
        {
            throw new ArgumentException("INetAppInfo is required to check if a remote resource exists",
                nameof(appInfo));
        }
        return (await Web.SendTestRequest(appInfo.HttpClient, appInfo.GetHttpRequestTo(new Uri(RemotePath))).ConfigureAwait(false)).IsSuccess ? ResourceLocationTypes.Remote : ResourceLocationTypes.None;
    }
    public async Task<bool> Exists(bool testRemote = false, INetAppInfo? appInfo = null)
    {
        return await ExistsAt(testRemote, appInfo) != ResourceLocationTypes.None;
    }
    public string? Url => Uri?.AbsoluteUri;
    public Uri? Uri
    {
        get
        {
            if (Types.HasFlag(ResourceLocationTypes.Local) && LocalPath != null) return new Uri(FileManager.GetItemPath(AppFolder, LocalPath));
            if (Types.HasFlag(ResourceLocationTypes.Settings) && SettingsPath != null) return new Uri(FileManager.GetItemPath(AppFolder.Settings, SettingsPath));
            if (Types.HasFlag(ResourceLocationTypes.Remote) && RemotePath != null) return new Uri(RemotePath);
            return null;
        }
    }
}
