// ReSharper disable MemberCanBePrivate.Global

namespace KinoshitaProductions.Common.Services;

using Newtonsoft.Json;
using Serilog;

public static class SettingsManager
{
    public static async Task SaveSettings<T>(T settings, string path) where T : class, new()
    {
        var json = JsonConvert.SerializeObject(settings);

        // would be nice to make this more efficient, by using a IChangeTimestampTracker that triggers when setting properties in an ObservableObject
        var settingsJson = ElementCacheDict<string, string>.GetSharedElement(
            (!string.IsNullOrWhiteSpace(path) ? path + Path.DirectorySeparatorChar : string.Empty) + "Settings.json");

        if (settingsJson == null || string.CompareOrdinal(settingsJson, json) != 0)
        {
            ElementCacheDict<string, string>.StoreSharedElement(
                (!string.IsNullOrWhiteSpace(path) ? path + Path.DirectorySeparatorChar : string.Empty) +
                "Settings.json", json);
            await FileManager.CreateFileAsyncSafe(AppFolder.Settings,
                (!string.IsNullOrWhiteSpace(path) ? path + Path.DirectorySeparatorChar : string.Empty) +
                "Settings.json", System.Text.Encoding.UTF8.GetBytes(json));
        }
    }

    public static async Task<T> LoadSettings<T>(string path) where T : class, new()
    {
        try
        {
            var json = await FileManager.ReadFileToStringAsync(AppFolder.Settings,
                (!string.IsNullOrWhiteSpace(path) ? path + Path.DirectorySeparatorChar : string.Empty) +
                "Settings.json");
            if (json != null)
            {
                var settings = JsonConvert.DeserializeObject<T>(json);
                if (settings != null)
                {
                    ElementCacheDict<string, string>.StoreSharedElement(
                        (!string.IsNullOrWhiteSpace(path) ? path + Path.DirectorySeparatorChar : string.Empty) +
                        "Settings.json", json);
                    return settings;
                }
            }
        }
        catch (Exception ex)
        {
            //NOT CRITICAL, IT'S A NORMAL ERROR
            Log.Debug(ex, "Failed to load settings, returning initial instead");
        }

        return new T();
    }

    /// <summary>
    /// Checks whether the setting file is at the target path.
    /// NOTE: It will look up for the file, or for a .bak file (returns true if real or .bak file was found).
    /// </summary>
    /// <param name="appFolder"></param>
    /// <param name="relativePathAndFileName"></param>
    /// <returns></returns>
    public static async Task<FilePresence> ExistsAsync(AppFolder appFolder, string relativePathAndFileName = "")
    {
        if (await FileManager.ExistsAsync(appFolder, relativePathAndFileName).ConfigureAwait(false))
            return FilePresence.Found;
        else if (await FileManager.ExistsAsync(appFolder, $"{relativePathAndFileName}.bak").ConfigureAwait(false))
            return FilePresence.BackupFound;
        else
            return FilePresence.NotFound;
    }

    public static Task<FilePresence> ExistsAsync(string relativePathAndFileName) =>
        ExistsAsync(AppFolder.Settings, relativePathAndFileName);

    public static async Task DeleteAsync(AppFolder appFolder, string relativePathAndFileName, FilePresence filePresence)
    {
        switch (filePresence)
        {
            case FilePresence.Found:
                await FileManager.DeleteAsync(appFolder, relativePathAndFileName).ConfigureAwait(false);
                break;

            case FilePresence.BackupFound:
                await FileManager.DeleteAsync(appFolder, $"{relativePathAndFileName}.bak").ConfigureAwait(false);
                break;
        }
    }

    public static Task DeleteAsync(string relativePathAndFileName, FilePresence filePresence) =>
        DeleteAsync(AppFolder.Settings, relativePathAndFileName, filePresence);

    public static async Task ForceDeleteAsync(AppFolder appFolder, string relativePathAndFileName)
    {
        if (await FileManager.ExistsAsync(appFolder, relativePathAndFileName).ConfigureAwait(false))
            await FileManager.DeleteAsync(appFolder, relativePathAndFileName).ConfigureAwait(false);
        if (await FileManager.ExistsAsync(appFolder, $"{relativePathAndFileName}.bak").ConfigureAwait(false))
            await FileManager.DeleteAsync(appFolder, $"{relativePathAndFileName}.bak").ConfigureAwait(false);
    }

    public static Task ForceDeleteAsync(string relativePathAndFileName) =>
        ForceDeleteAsync(AppFolder.Settings, relativePathAndFileName);

    /// <summary>
    /// Reads an IStatefulJson object of type <typeparamref name="T"/>, from the specified folder according to the <seealso cref="FilePresence"/> provided. If successfully read, it'll fill in the StateJson property.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="appFolder"></param>
    /// <param name="relativePathAndFileName"></param>
    /// <param name="filePresence"></param>
    /// <param name="compressionAlgorithm"></param>
    /// <returns></returns>
    public static async Task<T?> TryLoadingStatefulAsJson<T>(AppFolder appFolder, string relativePathAndFileName,
        FilePresence filePresence, CompressionAlgorithm compressionAlgorithm = CompressionAlgorithm.None)
        where T : class, IStatefulAsJson, new()
    {
        if (filePresence == FilePresence.NotFound)
        {
            Log.Debug($"Attempted to load a non existing file from {appFolder}/{relativePathAndFileName}");
            return null;
        }

        if (filePresence == FilePresence.BackupFound)
        {
            relativePathAndFileName += ".bak";
        }

        try
        {
#if NET7_0_OR_GREATER
            await
#endif
                using var stream = await FileManager.ReadFileToStreamAsync(appFolder, relativePathAndFileName)
                    .ConfigureAwait(false);
            if (stream == null)
            {
                return null;
            }

            switch (compressionAlgorithm)
            {
                case CompressionAlgorithm.None:
                    var json = await StreamHelper.ReadFullyAsStringAsync(stream).ConfigureAwait(false);
                    var @object = JsonConvert.DeserializeObject<T>(json);
                    if (@object != null)
                    {
                        @object.StateJson = json;
                        return @object;
                    }

                    break;
                default:
                    json = await StreamHelper
                        .ReadFullyAsStringAsync(Compression.GetDecompressionStreamFor(stream, compressionAlgorithm))
                        .ConfigureAwait(false);
                    @object = JsonConvert.DeserializeObject<T>(json);
                    if (@object != null)
                    {
                        @object.StateJson = json;
                        return @object;
                    }

                    break;
            }

            return null;
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Failed to load or get new item from file");
            return null; // expected if not found or damaged
        }
    }

    public static Task<T?> TryLoadingStatefulAsJson<T>(string relativePathAndFileName, FilePresence filePresence,
        CompressionAlgorithm compressionAlgorithm = CompressionAlgorithm.None) where T : class, IStatefulAsJson, new()
        => TryLoadingStatefulAsJson<T>(AppFolder.Settings, relativePathAndFileName, filePresence, compressionAlgorithm);

    public static async Task<T?> TryLoadingJson<T>(AppFolder appFolder, string relativePathAndFileName,
        FilePresence filePresence, CompressionAlgorithm compressionAlgorithm = CompressionAlgorithm.None)
        where T : class, new()
    {
        if (filePresence == FilePresence.NotFound)
        {
            Log.Debug($"Attempted to load a non existing file from {appFolder}/{relativePathAndFileName}");
            return null;
        }
        else if (filePresence == FilePresence.BackupFound)
        {
            relativePathAndFileName += ".bak";
        }

        try
        {
#if NET7_0_OR_GREATER
            await
#endif
                using var stream = await FileManager.ReadFileToStreamAsync(appFolder, relativePathAndFileName)
                    .ConfigureAwait(false);
            if (stream == null)
                return null;
            switch (compressionAlgorithm)
            {
                case CompressionAlgorithm.None:
                    var json = await StreamHelper.ReadFullyAsStringAsync(stream).ConfigureAwait(false);
                    var @object = JsonConvert.DeserializeObject<T>(json);
                    return @object;
                default:
                    json = await StreamHelper
                        .ReadFullyAsStringAsync(Compression.GetDecompressionStreamFor(stream, compressionAlgorithm))
                        .ConfigureAwait(false);
                    @object = JsonConvert.DeserializeObject<T>(json);
                    return @object;
            }
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Failed to load or get new item from file");
            return null; // expected if not found or damaged
        }
    }

    public static Task<T?> TryLoadingJson<T>(string relativePathAndFileName, FilePresence filePresence,
        CompressionAlgorithm compressionAlgorithm = CompressionAlgorithm.None) where T : class, new()
        => TryLoadingJson<T>(AppFolder.Settings, relativePathAndFileName, filePresence, compressionAlgorithm);

    /// <summary>
    /// Tries saving an object without worrying about locking.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="objectToSave"></param>
    /// <returns></returns>
    public static (bool Success, bool ChangesDetected, string? json)
        TrySavingLockedStatefulAsJsonStep1<T>(T objectToSave) where T : class, IStatefulAsJson, new()
    {
        try
        {
            var json = JsonConvert.SerializeObject(objectToSave);
            return (true, objectToSave.StateJson == null || string.CompareOrdinal(objectToSave.StateJson, json) != 0,
                json);
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Failed to serialize state");
        }

        return (false, false, null);
    }

    public static async Task<bool> TrySavingLockedStatefulAsJsonStep2<T>(T objectToSave, string json,
        AppFolder appFolder, string relativePathAndFileName,
        CompressionAlgorithm compressionAlgorithm = CompressionAlgorithm.None) where T : class, IStatefulAsJson, new()
    {
        try
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);
            await FileManager.CreateFileAsyncSafe(appFolder, relativePathAndFileName, bytes, compressionAlgorithm);
            objectToSave.StateJson = json;
            return true;
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Failed to save file");
        }

        return false;
    }

    public static async Task<bool> TrySavingStatefulAsJson<T>(T objectToSave, AppFolder appFolder,
        string relativePathAndFileName, CompressionAlgorithm compressionAlgorithm = CompressionAlgorithm.None)
        where T : class, IStatefulAsJson, new()
    {
        try
        {
            var json = JsonConvert.SerializeObject(objectToSave);
            if (objectToSave.StateJson != null && string.CompareOrdinal(objectToSave.StateJson, json) == 0) return true;
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);
            await FileManager.CreateFileAsyncSafe(appFolder, relativePathAndFileName, bytes, compressionAlgorithm)
                .ConfigureAwait(false);
            objectToSave.StateJson = json;
            return true;
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Failed to save file");
        }

        return false;
    }

    public static async Task<bool> TrySavingStatefulAsJsonWithTimestamp<T>(T objectToSave, AppFolder appFolder,
        string relativePathAndFileName, CompressionAlgorithm compressionAlgorithm = CompressionAlgorithm.None,
        bool forceTimestampUpdate = false) where T : class, IStatefulAsJsonWithTimestamp, new()
    {
        try
        {
            var json = JsonConvert.SerializeObject(objectToSave);
            if (objectToSave.StateJson != null && string.CompareOrdinal(objectToSave.StateJson, json) == 0 &&
                !forceTimestampUpdate) return true;
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);
            await FileManager.CreateFileAsyncSafe(appFolder, relativePathAndFileName, bytes, compressionAlgorithm)
                .ConfigureAwait(false);
            objectToSave.Timestamp = DateTime.Now;
            objectToSave.StateJson = json;
            return true;
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Failed to save file");
        }

        return false;
    }

    public static async Task<bool> TrySavingJson<T>(T objectToSave, AppFolder appFolder, string relativePathAndFileName,
        CompressionAlgorithm compressionAlgorithm = CompressionAlgorithm.None)
    {
        try
        {
            var json = JsonConvert.SerializeObject(objectToSave);
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);
            await FileManager.CreateFileAsyncSafe(appFolder, relativePathAndFileName, bytes, compressionAlgorithm)
                .ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Failed to save file");
        }

        return false;
    }

    public static Task<bool> TrySavingJson<T>(T objectToSave, string relativePathAndFileName,
        CompressionAlgorithm compressionAlgorithm = CompressionAlgorithm.None)
        => TrySavingJson(objectToSave, AppFolder.Settings, relativePathAndFileName, compressionAlgorithm);

    // This function is meant to try loading a previously stored class. Else, it will get return null. 
    public static async Task<T?> TryLoading<T>(AppFolder appFolder, string relativePathAndFileName,
        CompressionAlgorithm compressionAlgorithm = CompressionAlgorithm.None) where T : class, new()
    {
        try
        {
#if NET7_0_OR_GREATER
            await
#endif
                using var stream = await FileManager.ReadFileToStreamAsync(appFolder, relativePathAndFileName)
                    .ConfigureAwait(false);

            if (stream == null)
                return null; // file didn't exist

            switch (compressionAlgorithm)
            {
                case CompressionAlgorithm.None:
                    var json = await StreamHelper.ReadFullyAsStringAsync(stream).ConfigureAwait(false);
                    return JsonConvert.DeserializeObject<T>(json);

                default:
                    json = await StreamHelper
                        .ReadFullyAsStringAsync(Compression.GetDecompressionStreamFor(stream, compressionAlgorithm))
                        .ConfigureAwait(false);
                    return JsonConvert.DeserializeObject<T>(json);
            }
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Failed to load or get new item from file");
            return null; // expected if not found or damaged
        }
    }

    public static async Task<ObjectAndJsonTuple<T>> TryLoadingOrGetNew<T>(AppFolder appFolder,
        string relativePathAndFileName) where T : class, new()
    {
        try
        {
            var json = await FileManager.ReadFileToStringAsync(appFolder, relativePathAndFileName)
                .ConfigureAwait(false);
            if (json != null)
                return new ObjectAndJsonTuple<T>(JsonConvert.DeserializeObject<T>(json) ?? new T(), json);
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Failed to load or get new item from file");
        }

        var newObject = new T();
        return new ObjectAndJsonTuple<T>(newObject, JsonConvert.SerializeObject(newObject));
    }

    // This function is meant to try loading a previously saved class. returns saved JSON.
    public static async Task<(bool Success, string? StoredJson)> TrySaving<T>(T objectToSave, string? cachedJson,
        AppFolder appFolder,
        string relativePathAndFileName, CompressionAlgorithm compressionAlgorithm = CompressionAlgorithm.None)
        where T : class, new()
    {
        try
        {
            var json = JsonConvert.SerializeObject(objectToSave);
            if (cachedJson != null && string.CompareOrdinal(cachedJson, json) == 0) return (true, cachedJson);
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);
            await FileManager.CreateFileAsyncSafe(appFolder, relativePathAndFileName, bytes, compressionAlgorithm);
            return (true, json);
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Failed to load or get new item from file");
        }

        return (false, null);
    }
}