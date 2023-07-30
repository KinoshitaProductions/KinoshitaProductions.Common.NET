// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
#if ANDROID
using Android.Content;
#endif

namespace KinoshitaProductions.Common.Services;

using Newtonsoft.Json;
using Serilog;

/// <summary>
/// The following class is meant to provide simple cross-platform methods to perform file operations.
/// </summary>
public static class FileManager
{
#if ANDROID
    private sealed class FileReference : IEquatable<FileReference>
    {
        internal FileReference(AppFolder folder, string relativePath)
        {
                Folder = folder;
                RelativePath = relativePath;
        } 
        // ReSharper disable once MemberCanBePrivate.Local
        public AppFolder Folder { get; init; }
        // ReSharper disable once MemberCanBePrivate.Local
        public string RelativePath { get; init; }
        public override int GetHashCode()
        {
            return this.Folder.GetHashCode() ^ this.RelativePath.GetHashCode();
        }
        public bool Equals(FileReference? other)
        {
            if (other == null) return false;
            return this.Folder == other.Folder && this.RelativePath == other.RelativePath;
        }
        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (this == obj) return true;
            if (obj is FileReference other)
            {
                return this.Folder == other.Folder && this.RelativePath == other.RelativePath;
            }
            return false;
        }
    }

    private static readonly Dictionary<FileReference, global::Android.Net.Uri> PendingFiles = new Dictionary<FileReference, global::Android.Net.Uri>();
#endif
    private static readonly SortedDictionary<AppFolder, string> FolderPaths = new SortedDictionary<AppFolder, string>();
    private static readonly SortedDictionary<AppFolder, string> RootlessPaths = new SortedDictionary<AppFolder, string>();
    private static string AppName { get; set; } = string.Empty;

    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public static bool CanSaveDownloads { get; set; }
    /// <summary>
    /// Since some boorus use special characters such as :, or ?, or other symbols in filenames, which are not valid, we must escape them into an URI name
    /// </summary>
    /// <param name="filename"></param>
    /// <returns></returns>
    public static string GetValidFilenameFor(string filename)
    {
        bool validFilename = true;
        foreach (var c in filename)
        {
            switch (c)
            {
                case '"':
                case '*':
                case '<':
                case '>':
                case '?':
                case '\\':
                case '|':
                case '/':
                case ':':
                    validFilename = false;
                    break;
            }
            if (!validFilename)
                break;
        }
        if (!validFilename)
        {
            var extension = Path.GetExtension(filename);
            return filename.Substring(0, filename.Length - extension.Length) // Cannot use Path.GetFilename since it's cropped due to illegal characters
                .Replace("\"", "%22")
                .Replace("*", "%2A")
                .Replace("<", "%3C")
                .Replace(">", "%3E")
                .Replace("?", "%3F")
                .Replace("\\", "%5C")
                .Replace("|", "%7C")
                .Replace("/", "%2F")
                .Replace(":", "%3A") + extension;
        }
        else
            return filename;
    }

#if ANDROID
    public static async Task UpdateStorageInfo()
    {
        // Wait for activity to be available
        var attemptCount = 0;
        while (attemptCount++ < 1000 && _getCurrentActivityFn() == null)
        {
            await Task.Delay(20);
        }
        var activity = _getCurrentActivityFn();
        if (activity == null) return;
        // this feature is currently only supported in Android
        lock (StorageDeviceInfos)
        {
            StorageDeviceInfos.Clear();

            try
            {
                var internalStorage = true;
                // GET EXTERNAL STORAGE SIZE
                var dir = AndroidX.Core.Content.ContextCompat.GetExternalFilesDirs(activity, Android.OS.Environment.DirectoryPictures);
                foreach (var roPath in dir.Select(x => x.AbsolutePath))
                {
                    var path = roPath;
                    var stat = new Android.OS.StatFs(path);
                    var externalStorageUsedBytes = stat.TotalBytes - stat.AvailableBytes;
                    var fallbackPath = path;

                    if (path.Contains("/Android/"))
                    {
                        path = path.Substring(0, path.IndexOf("/Android/", StringComparison.Ordinal) + 1);
                    }

                    if (internalStorage)
                    {
                        StorageDeviceInfos.Add(new StorageDeviceInfo
                        {
                            Name = "Internal storage",
                            Path = "/",
                            DisplayPath = "/",
                            FallbackPath = "/",
                            UsedSpaceBytes = externalStorageUsedBytes,
                            TotalSpaceBytes = stat.TotalBytes,
                        });
                        internalStorage = false;
                    }
                    else
                    {
                        StorageDeviceInfos.Add(new StorageDeviceInfo
                        {
                            Name = "External storage",
                            Path = path,
                            DisplayPath = path,
                            FallbackPath = fallbackPath + Path.DirectorySeparatorChar,
                            UsedSpaceBytes = externalStorageUsedBytes,
                            TotalSpaceBytes = stat.TotalBytes,
                        });
                    }
                }

                var defaultStorage = StorageDeviceInfos.Find(x => _checkIfIsDefaultStorage(x));
                if (defaultStorage != null)
                {
                    defaultStorage.IsDefault = true;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to read storage info");
            }
            _notifyStorageDeviceInfosUpdated(StorageDeviceInfos);
        }
    }
#endif
    // Allows loading settings
    public static async Task Preinitialize(string appName, FileManagerOptions options)
    {
        FileManager.AppName = appName;
        FileManager._settingsLoadedSuccessfully = options.SettingsLoadedSuccessfullyFn;
#if WINDOWS_UWP || ANDROID    
        FileManager._getNextStoragePath = options.GetNextStoragePathFn;
#endif
#if ANDROID
        FileManager._notifyStorageEvent = options.NotifyStorageEventFn;
        FileManager._notifyStorageDeviceInfosUpdated = options.NotifyStorageDeviceInfosUpdatedFn;
        FileManager._saveMediaToDownloadsFolder = options.SaveMediaToDownloadsFolderFn;
        FileManager._checkIfIsDefaultStorage = options.CheckIfIsDefaultStorageFn;
        FileManager._getCurrentActivityFn = options.GetCurrentActivityFn;
        FileManager._getCurrentContextFn = options.GetCurrentContextFn;
#endif
#if WINDOWS_UWP
        FileManager._getStorageToken = options.GetStorageTokenFn;
#endif
        FileManager._changeToFallbackStorage = options.ChangeToFallbackStorageFn;
#if ANDROID
        // this feature is currently only supported in Android
        await UpdateStorageInfo();
#endif

#if ANDROID
        var appPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + Path.DirectorySeparatorChar;
#elif WINDOWS_UWP
        var appPath = Windows.Storage.ApplicationData.Current.LocalFolder.Path + Path.DirectorySeparatorChar;
#else
        var appPath = Environment.CurrentDirectory + Path.DirectorySeparatorChar;
#endif

        RootlessPaths.Clear();
        FolderPaths.Clear();
        RootlessPaths.Add(AppFolder.Private, string.Empty);
        FolderPaths.Add(AppFolder.Private, appPath);
        RootlessPaths.Add(AppFolder.State, "State" + Path.DirectorySeparatorChar);
        FolderPaths.Add(AppFolder.State, appPath + "State" + Path.DirectorySeparatorChar);
        RootlessPaths.Add(AppFolder.Data, "Data" + Path.DirectorySeparatorChar);
        FolderPaths.Add(AppFolder.Data, appPath + "Data" + Path.DirectorySeparatorChar);
        RootlessPaths.Add(AppFolder.DownloadsInfo, "DownloadsInfo" + Path.DirectorySeparatorChar);
        FolderPaths.Add(AppFolder.DownloadsInfo, appPath + "DownloadsInfo" + Path.DirectorySeparatorChar);
        RootlessPaths.Add(AppFolder.Settings, "Settings" + Path.DirectorySeparatorChar);
        FolderPaths.Add(AppFolder.Settings, appPath + "Settings" + Path.DirectorySeparatorChar);
        RootlessPaths.Add(AppFolder.ErrorLogs, "ErrorLogs" + Path.DirectorySeparatorChar);
        FolderPaths.Add(AppFolder.ErrorLogs, appPath + "ErrorLogs" + Path.DirectorySeparatorChar);

        await CreateDefaultFolders();
    }

    private static Func<bool> _settingsLoadedSuccessfully = () => true;
#if ANDROID
    private static Action<string> _notifyStorageEvent = (_) => {};
#endif
    private static Action<string> _changeToFallbackStorage = (_) => {};
#if WINDOWS_UWP || ANDROID    
    private static Func<string?> _getNextStoragePath = () => null;
#endif
#if WINDOWS_UWP
    private static Func<string?> _getStorageToken = () => null;
#endif
#if ANDROID
    // this feature is currently only supported in Android
    private static readonly List<StorageDeviceInfo> StorageDeviceInfos = new List<StorageDeviceInfo>(); 
    private static Action<List<StorageDeviceInfo>> _notifyStorageDeviceInfosUpdated = (_) => {};
    private static Func<bool> _saveMediaToDownloadsFolder = () => false;
    private static Func<StorageDeviceInfo, bool> _checkIfIsDefaultStorage = (_) => false;
    private static Func<Activity?> _getCurrentActivityFn = () => null;
    private static Func<Context?> _getCurrentContextFn = () => null;
#endif
#if ANDROID
    private static string? GetPicturesPath()
    {
        string? picturesPath = null;
        if ((_getNextStoragePath() ?? "/").Length > 1)
        {
            picturesPath = (_getNextStoragePath() ?? "/");

            if (!Directory.Exists(picturesPath))
            {
                picturesPath = null; // device ejected? return to default
            }
        }

        picturesPath ??= Android.OS.Environment.ExternalStorageState == Android.OS.Environment.MediaMounted &&
                         Android.OS.Environment.ExternalStorageDirectory != null
            ? // Has SD card?
            Android.OS.Environment.ExternalStorageDirectory.AbsolutePath + Path.DirectorySeparatorChar +
            Android.OS.Environment.DirectoryPictures + Path.DirectorySeparatorChar
            : null;

        var activity = _getCurrentActivityFn();
        if (activity == null) return picturesPath;
        if (picturesPath == null) // no SD card
        {
            var dir = AndroidX.Core.Content.ContextCompat.GetExternalFilesDirs(activity, Android.OS.Environment.DirectoryPictures);
            picturesPath = dir[0].AbsolutePath;

            if (picturesPath.Last() != Path.DirectorySeparatorChar && picturesPath.Last() != Path.AltDirectorySeparatorChar)
                picturesPath += Path.DirectorySeparatorChar; // add missing "/" if required
        }

        return picturesPath;
    }
#endif
    // Sets download folders (for setting a different storage device for pictures)
    public static async Task Initialize(string? picturesPath = null, string? fallbackPath = null, bool notifyStorageChange = false)
    {
        if (!_settingsLoadedSuccessfully())
        {
            return; // CRITICAL ERROR (?), settings should have been loaded before this point unless there was a lack of permissions
        }
        try
        {
#if WINDOWS_UWP
            picturesPath ??= _getStorageToken() != null ? _getNextStoragePath() + Path.DirectorySeparatorChar : Windows.Storage.UserDataPaths.GetDefault().Pictures + Path.DirectorySeparatorChar;
#elif ANDROID
            picturesPath ??= GetPicturesPath();
            if (picturesPath == null) 
            {
                _notifyStorageEvent("WARNING: Couldn't detect storage to save pictures");
                return;
            }
#else
            // nothing to do here on this platform
            picturesPath ??= string.Empty;
#endif
            lock (FolderPaths)
            {
                RootlessPaths[AppFolder.Pictures] = string.Empty;
                FolderPaths[AppFolder.Pictures] = picturesPath;
                if (string.IsNullOrEmpty(AppName)) {
                    RootlessPaths[AppFolder.AppPictures] = RootlessPaths[AppFolder.Pictures];
                    FolderPaths[AppFolder.AppPictures] = FolderPaths[AppFolder.Pictures];
                } 
                else 
                {
                    RootlessPaths[AppFolder.AppPictures] = AppName + Path.DirectorySeparatorChar;
                    FolderPaths[AppFolder.AppPictures] = picturesPath + AppName + Path.DirectorySeparatorChar;
                }
            }
            if (!await CreateDefaultFolders(fallbackPath))
            {
                // ReSharper disable once RedundantJumpStatement
                return; // interrupt, another attempt will be performed
            }
#if ANDROID
            // this test only works for Android < 9, since else, MediaStore guarantees accessibility 
            if (Android.OS.Build.VERSION.SdkInt < Android.OS.BuildVersionCodes.Q)
            {
#endif
                await CreateFileAsyncSafe(AppFolder.AppPictures, "permissions_test", Array.Empty<byte>());
                await DeleteAsync(AppFolder.AppPictures, "permissions_test");
#if ANDROID
            }
#endif

#if ANDROID
            if (notifyStorageChange)
            {
                _notifyStorageEvent("Saving pictures to " + FolderPaths[AppFolder.Pictures]);
            }
#endif
        }
        catch (Exception ex)
        {
#if ANDROID
            if (fallbackPath == null)
            {
                _notifyStorageEvent("WARNING: Cannot save pictures to selected storage device\n(folder write permissions issue?)");
            }
#endif
            if (fallbackPath != null)
            {
                _changeToFallbackStorage(fallbackPath);
                await Initialize(fallbackPath: null);
            }
            Log.Error(ex, "Failed to initialize storage");
        }
    }

    public static async Task<bool> CreateDefaultFolders(string? fallbackPath = null)
    {
        // Folders must be created manually to avoid issues with defined "non-existing" folders
        try
        {
            if (FolderPaths.ContainsKey(AppFolder.Private))
            {
                await CreateOrOpenFolderAsync(AppFolder.Private, "State");
                await CreateOrOpenFolderAsync(AppFolder.Private, "DownloadsInfo");
                await CreateOrOpenFolderAsync(AppFolder.Private, "Settings");
                await CreateOrOpenFolderAsync(AppFolder.Private, "ErrorLogs");
            }
            if (FolderPaths.ContainsKey(AppFolder.Pictures))
            {
                CanSaveDownloads = false;
                await CreateOrOpenFolderAsync(AppFolder.Pictures, "");
                if (!string.IsNullOrEmpty(AppName))
                {
                    await CreateOrOpenFolderAsync(AppFolder.AppPictures, "");
                }
                CanSaveDownloads = true;
            }
            return true;
        }
        catch (Exception ex)
        {
            // COULD NOT GET DIR TO SAVE PICTURES
            Log.Error(ex, "Failed to create default folders in storage");
#if ANDROID
            if (fallbackPath == null)
            {
                _notifyStorageEvent("WARNING: Cannot save pictures to selected storage device\n(folder write permissions issue?)");
                await Initialize(picturesPath: null, fallbackPath: null);
            }
#endif
            if (fallbackPath != null)
            {
                _changeToFallbackStorage(fallbackPath);
                await Initialize(picturesPath: fallbackPath, fallbackPath: null);
            }
        }
        return false;
    }

#if WINDOWS_UWP
    public static async Task<Windows.Storage.StorageFile?> OpenFileAsync(AppFolder appFolder, string relativePath)
    {
        var folder = await CreateOrOpenFolderAsync(appFolder, Path.GetDirectoryName(relativePath)).ConfigureAwait(false);
        if (folder == null) return null;
        return await folder.GetFileAsync(Path.GetFileName(relativePath));
    }
#endif

    public static 
#if WINDOWS_UWP
        async
        Task<Windows.Storage.StorageFolder?>
#else
        Task
#endif
        CreateOrOpenFolderAsync(AppFolder appFolder, string? relativePath)
    {
#if ANDROID
        if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Q && (appFolder == AppFolder.AppPictures || appFolder == AppFolder.Pictures))
            return Task.CompletedTask; // nothing to do, these folders are automatic
#endif
#if WINDOWS_UWP
        // For UWP, we need to get the root folder through we can reach that location
        Windows.Storage.StorageFolder currentFolder;
        switch (appFolder)
        {
            case AppFolder.Private:
            case AppFolder.State:
            case AppFolder.DownloadsInfo:
            case AppFolder.Settings:
            case AppFolder.ErrorLogs:
                currentFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
                break;

            case AppFolder.AppPictures:
            case AppFolder.Pictures:
                var storageToken = _getStorageToken();
                if (storageToken != null)
                {
                    currentFolder = await Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.GetFolderAsync(storageToken);
                }
                else
                {
                    currentFolder = (await Windows.Storage.StorageLibrary.GetLibraryAsync(Windows.Storage.KnownLibraryId.Pictures)).SaveFolder;
                }
                break;

            default:
                throw new ArgumentException("UNHANDLED STORAGE FOLDER " + appFolder.ToString());
        }

        // If are not yet in the specified AppFolder, we'll navigate to it
        var pendingToNavigate = FolderPaths[appFolder].Substring(currentFolder.Path.Length + Path.DirectorySeparatorChar.ToString().Length);
        foreach (var folderName in pendingToNavigate.Split(Path.DirectorySeparatorChar))
            if (!string.IsNullOrWhiteSpace(folderName))
            {
                currentFolder = await currentFolder.CreateFolderAsync(folderName, Windows.Storage.CreationCollisionOption.OpenIfExists);
            }

        // Afterwards, finally, we start creating folder
        if (!string.IsNullOrEmpty(relativePath))
        {
            pendingToNavigate = relativePath;
#pragma warning disable CS8602
            foreach (var folderName in pendingToNavigate.Split(Path.DirectorySeparatorChar))
#pragma warning restore CS8602
                if (!string.IsNullOrWhiteSpace(folderName))
                {
                    currentFolder = await currentFolder.CreateFolderAsync(folderName, Windows.Storage.CreationCollisionOption.OpenIfExists);
                }
        }
        return currentFolder;
#else
        string currentPath;
        lock (FolderPaths)
        {
            currentPath = FolderPaths[appFolder];
        }

        if (!Directory.Exists(currentPath))
            Directory.CreateDirectory(currentPath);
        
        if (relativePath == null) return Task.CompletedTask;
        foreach (var folderName in relativePath.Split(Path.DirectorySeparatorChar))
        {
            var creatingFolderPath = currentPath + folderName;

            if (!string.IsNullOrWhiteSpace(folderName) && !Directory.Exists(creatingFolderPath))
                Directory.CreateDirectory(creatingFolderPath);

            currentPath = creatingFolderPath + Path.DirectorySeparatorChar;
        }

        return Task.CompletedTask;
#endif
    }

    public static 
#if WINDOWS_UWP
        async
#endif
    Task<long> GetFileSizeAsync(AppFolder appFolder, string relativePathAndFileName)
    {
#if ANDROID
        if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Q && (appFolder == AppFolder.AppPictures || appFolder == AppFolder.Pictures))
        {
            var operationDataOpt = GetOperationDataFor(appFolder, relativePathAndFileName);
            if (!operationDataOpt.HasValue) return Task.FromResult(0L);
            var operationData = operationDataOpt.Value;

            var query = Android.Provider.MediaStore.IMediaColumns.RelativePath + " = ? and " + Android.Provider.MediaStore.IMediaColumns.DisplayName + " = ?";
            var queryArgs = new [] { $"{Path.GetDirectoryName(operationData.RelativeLocation) + Path.DirectorySeparatorChar}", $"{Path.GetFileName(operationData.RelativeLocation)}" };

            var projection = new [] { Android.Provider.MediaStore.IMediaColumns.Size};
            var resultsCursor = operationData.ContentResolver.Query(operationData.ExternalUri, projection, query, queryArgs, null);
            if (resultsCursor is { Count: > 0 } && resultsCursor.MoveToNext())
                return Task.FromResult(resultsCursor.GetLong(0));
            else 
                return Task.FromResult(0L);
        }
#endif

        try
        {
#if WINDOWS_UWP
            var folder = await CreateOrOpenFolderAsync(appFolder, Path.GetDirectoryName(relativePathAndFileName));
            if (folder == null) return 0L;
            var file = await folder.GetFileAsync(Path.GetFileName(relativePathAndFileName));
            var basicProperties = await file.GetBasicPropertiesAsync();
            return (long)basicProperties.Size; // truncate to long, since ulong sized it's unlikely 
#elif ANDROID
            return Task.FromResult(new Java.IO.File(GetItemPath(appFolder, relativePathAndFileName)).Length());
#else
            return Task.FromResult(new FileInfo(GetItemPath(appFolder, relativePathAndFileName)).Length);
#endif
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to read file size");
        }
#if WINDOWS_UWP
        return 0L;
#else
         return Task.FromResult(0L);
#endif
    }

    /// <summary>
    /// Imitates UNIX touch command (creates empty file if success, returns false if fails).
    /// </summary>
    /// <param name="appFolder"></param>
    /// <param name="relativePathAndFileName"></param>
    /// <returns></returns>
    public static async Task<bool> TryTouchFileAsync(AppFolder appFolder, string relativePathAndFileName)
    {
        try
        {
            if (await ExistsAsync(appFolder, relativePathAndFileName).ConfigureAwait(false))
                return true;
            using var stream = new MemoryStream();
            await CreateFileAsync(appFolder, relativePathAndFileName, stream).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to create empty file");
        }
        return false;
    }

#if ANDROID
    private static string GetMimeFromExtension(string extension)
    {
        switch (extension)
        {
            case "avi":
                return "video/x-msvideo";
            case "gif":
                return "image/gif";
            case "jpg":
                return "image/jpeg";
            case "mkv":
                return "video/webm"; // correct is x-matroska, but might not work
            case "m4a":
            case "mov":
            case "mp4":
                return "video/mp4";
            case "mpe":
            case "mpg":
            case "mpeg":
                return "video/mpeg";
            case "ogv":
                return "video/ogg";
            case "svg":
                return "image/svg+xml";
            case "tif":
                return "image/tiff";
            case "webm":
                return "video/webm";
            case "3gp":
                return "video/3gpp";
            case "3g2":
                return "video/3gpp2";
            default:
                return "image/" + extension;
        }
    }

    private static bool IsVideoMime(string mime) => mime.StartsWith('v');
    private static AndroidStoreFileOperation? GetOperationDataFor(AppFolder appFolder, string relativePathAndFileName)
    {
        var extension = Path.GetExtension((relativePathAndFileName.EndsWith(".part")
                    ? relativePathAndFileName.Substring(0, relativePathAndFileName.Length - 5)
                    : relativePathAndFileName
                )).Substring(1).ToLower();
        var mimeType = GetMimeFromExtension(extension);

        var context = _getCurrentContextFn();
        if (context == null)
        {
            Log.Error("Failed to get context for a file operation!");
            return null; // should never happen, but if it does, needs to be handled, might need to replace with a throw later?
        }

        var volumes = Android.Provider.MediaStore.GetExternalVolumeNames(context);
        var targetVolume = Android.Provider.MediaStore.VolumeExternalPrimary;
        lock (StorageDeviceInfos)
        {
            if (volumes.Count > 1 && StorageDeviceInfos.Count > 1 && (GetItemPath(appFolder, string.Empty).StartsWith(StorageDeviceInfos[1].Path) || GetItemPath(appFolder, string.Empty).StartsWith(StorageDeviceInfos[1].FallbackPath)))
            {
                targetVolume = volumes.FirstOrDefault(x => !string.Equals(x, Android.Provider.MediaStore.VolumeExternalPrimary, StringComparison.Ordinal)) ?? Android.Provider.MediaStore.VolumeExternalPrimary;
            }
        }
        
        Android.Net.Uri? externalUri;
        string? relativeLocation;
        if (_saveMediaToDownloadsFolder())
        {
            externalUri = Android.Provider.MediaStore.Downloads.GetContentUri(targetVolume);
            relativeLocation = Android.OS.Environment.DirectoryDownloads;
        }
        else
        {
            externalUri = IsVideoMime(mimeType)
                ? Android.Provider.MediaStore.Video.Media.GetContentUri(targetVolume)
                : Android.Provider.MediaStore.Images.Media.GetContentUri(targetVolume);
            relativeLocation = IsVideoMime(mimeType) 
                ? Android.OS.Environment.DirectoryMovies 
                : Android.OS.Environment.DirectoryPictures;
        }

        relativeLocation = Path.Join(relativeLocation, GetItemPath(appFolder, relativePathAndFileName, false));
            
        if (externalUri == null || context.ContentResolver == null)
        {
            Log.Error("Failed to compile metadata for a file operation!");
            return null; // should never happen, but if it does, needs to be handled, might need to replace with a throw later?
        }
        return new AndroidStoreFileOperation
        {
            ExternalUri = externalUri,
            RelativeLocation = relativeLocation,
            MimeType = mimeType,
            ContentResolver = context.ContentResolver,
        };
    }

    private struct AndroidStoreFileOperation
    {
        public Android.Net.Uri ExternalUri;
        public string RelativeLocation;
        public string MimeType;
        public ContentResolver ContentResolver;
    }

#endif

    public static async Task<Stream
#if ANDROID
    ?
#endif
    > CreateFileStreamAsync(AppFolder appFolder, string relativePathAndFileName)
    {
#if ANDROID
        if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Q && (appFolder == AppFolder.AppPictures || appFolder == AppFolder.Pictures))
        {
             var operationDataOpt = GetOperationDataFor(appFolder, relativePathAndFileName);
            if (!operationDataOpt.HasValue) return null;
            var operationData = operationDataOpt.Value;

            var contentValues = new ContentValues();
            contentValues.Put(Android.Provider.MediaStore.IMediaColumns.DisplayName, Path.GetFileName(relativePathAndFileName));
            contentValues.Put(Android.Provider.MediaStore.IMediaColumns.MimeType, operationData.MimeType);
            contentValues.Put(Android.Provider.MediaStore.IMediaColumns.DateAdded, Java.Lang.JavaSystem.CurrentTimeMillis() / 1000);
            contentValues.Put(Android.Provider.MediaStore.IMediaColumns.RelativePath, Path.GetDirectoryName(operationData.RelativeLocation));
            var isPending = operationData.RelativeLocation.EndsWith(".part");
            contentValues.Put(Android.Provider.MediaStore.IMediaColumns.IsPending, isPending);

            if (isPending && await ExistsAsync(appFolder, relativePathAndFileName.Substring(0, relativePathAndFileName.Length - ".part".Length)))
                await DeleteAsync(appFolder, relativePathAndFileName.Substring(0, relativePathAndFileName.Length - ".part".Length));

            var uri = operationData.ContentResolver.Insert(operationData.ExternalUri, contentValues);
            if (uri == null) return null;

            // add it pending to clear
            if (isPending)
                FileManager.PendingFiles.TryAdd(new FileReference(appFolder, relativePathAndFileName), uri);

            return operationData.ContentResolver.OpenOutputStream(uri);
        }
#endif
        // Create missing folders
        string pendingFolderPath = relativePathAndFileName;
        while (pendingFolderPath.Contains(Path.DirectorySeparatorChar.ToString())) // If we also require creating some folders
        {
            string relativePath = relativePathAndFileName.Substring(0, pendingFolderPath.LastIndexOf(Path.DirectorySeparatorChar)); // Let's create them
            await CreateOrOpenFolderAsync(appFolder, relativePath);
            pendingFolderPath = relativePathAndFileName.Substring(relativePath.Length + Path.DirectorySeparatorChar.ToString().Length);
        }

        // Save file
#if WINDOWS_UWP
        var folder = await CreateOrOpenFolderAsync(appFolder, Path.GetDirectoryName(relativePathAndFileName));
        var fs = await folder.OpenStreamForWriteAsync(Path.GetFileName(relativePathAndFileName), Windows.Storage.CreationCollisionOption.ReplaceExisting);
#else
        const int bufferSize = 16 * 1024; // Probably the best is 128KB
        string absolutePathAndFileName = GetItemPath(appFolder, relativePathAndFileName);
        var fs = File.Create(absolutePathAndFileName, bufferSize, FileOptions.Asynchronous);
#endif
        return fs;
    }
    
    public static async Task CreateFileAsync(AppFolder appFolder, string relativePathAndFileName, Stream byteStream, CompressionAlgorithm compressionAlgorithm = CompressionAlgorithm.None)
    {
#if NET7_0_OR_GREATER
        await
 #endif
        using var fs = await CreateFileStreamAsync(appFolder, relativePathAndFileName).ConfigureAwait(false);
#if ANDROID
        if (fs == null) return; // nothing to do
#endif
        var cs = Compression.GetCompressionStreamFor(fs, compressionAlgorithm);
        await byteStream.CopyToAsync(cs).ConfigureAwait(false); // write it
        if (cs != fs) 
#if NET7_0_OR_GREATER
            await cs.DisposeAsync(); // the decompression stream may be the same than the file stream if no compression was applied
#else
            cs.Dispose();
#endif
    }

    public static async Task CreateFileAsyncSafe(AppFolder appFolder, string relativePathAndFileName, Stream byteStream, CompressionAlgorithm compressionAlgorithm = CompressionAlgorithm.None)
    {
        // 1. define vars
        bool shouldCreateBackupFile = await ExistsAsync(appFolder, relativePathAndFileName);

        // 2. if .part file existed previously, delete it
        if (await ExistsAsync(appFolder, relativePathAndFileName + ".part"))
            await DeleteAsync(appFolder, relativePathAndFileName + ".part");

        // 3. create "file.part"
        await CreateFileAsync(appFolder, relativePathAndFileName + ".part", byteStream, compressionAlgorithm);

        // 4. if file existed previously, rename to "file.backup"
        if (shouldCreateBackupFile)
        {
            // if file backup existed previously, delete old backup
            if (await ExistsAsync(appFolder, relativePathAndFileName + ".backup"))
                await DeleteAsync(appFolder, relativePathAndFileName + ".backup");

            await RenameAsync(appFolder, relativePathAndFileName, Path.GetFileName(relativePathAndFileName) + ".backup");
        }

        // 5. rename "file.part" to "file"
        await RenameAsync(appFolder, relativePathAndFileName + ".part", Path.GetFileName(relativePathAndFileName));

        // 6. if "file.backup" exists, delete it
        if (shouldCreateBackupFile)
            await DeleteAsync(appFolder, relativePathAndFileName + ".backup");
    }

    public static async Task CreateFileAsyncSafe(AppFolder appFolder, string relativePathAndFileName, byte[] bytes, CompressionAlgorithm compressionAlgorithm = CompressionAlgorithm.None)
    {
        using var ms = new MemoryStream(bytes);
        await CreateFileAsyncSafe(appFolder, relativePathAndFileName, ms, compressionAlgorithm);
    }
    private static int _additionalPossibleDelays = 3;
    public static async Task<Stream?> ReadFileToStreamAsync(AppFolder appFolder, string relativePathAndFileName)
    {
        var fileExists = await ExistsAsync(appFolder, relativePathAndFileName);
        var backupExists = await ExistsAsync(appFolder, relativePathAndFileName + ".backup");
        // If no file found, wait a bit to see if it appears afterwards
        if (!fileExists && !backupExists && _additionalPossibleDelays > 0)
        {
            --_additionalPossibleDelays;
            await Task.Delay(1000); // Wait a bit in case of permissions/filesystem/device issues
        }
        // if backup exists, restore backup
        if (backupExists)
        {
            if (await ExistsAsync(appFolder, relativePathAndFileName))
            {
                await DeleteAsync(appFolder, relativePathAndFileName);
            }
            await RenameAsync(appFolder, relativePathAndFileName + ".backup", Path.GetFileName(relativePathAndFileName));
            fileExists = true;
        }
        if (!fileExists) return null; // file does not exist
        
#if ANDROID
        if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Q && (appFolder == AppFolder.AppPictures || appFolder == AppFolder.Pictures))
        {
            var operationDataOpt = GetOperationDataFor(appFolder, relativePathAndFileName);
            if (!operationDataOpt.HasValue) return null;
            var operationData = operationDataOpt.Value;

            var targetExternalUri = GetMsUri(operationData);
            if (targetExternalUri == null) return null;

            var inputStream = operationData.ContentResolver.OpenInputStream(targetExternalUri);
            return inputStream;
        }
#endif

#if WINDOWS_UWP
        var folder = await CreateOrOpenFolderAsync(appFolder, Path.GetDirectoryName(relativePathAndFileName));
        var stream = await folder.OpenStreamForReadAsync(Path.GetFileName(relativePathAndFileName));
#elif ANDROID
        var stream = File.Open(GetItemPath(appFolder, relativePathAndFileName), FileMode.Open, FileAccess.Read, FileShare.Read);
#else
        var stream = File.Open(GetItemPath(appFolder, relativePathAndFileName), FileMode.Open, FileAccess.Read, FileShare.Read);
#endif

        return stream;
    }

    public static async Task<string?> ReadFileToStringAsync(AppFolder appFolder, string relativePathAndFileName)
    {
#if NET7_0_OR_GREATER
        await
#endif
        using var stream = await ReadFileToStreamAsync(appFolder, relativePathAndFileName);

        if (stream != null)
        {
            var str = await StreamHelper.ReadFullyAsStringAsync(stream);
            return str;
        }
        else
            return null; // file does not exist
    }

    // helper to simplify certain operations (too much knowledge of serialization on other services)
    // DOES NOT HAVE CACHE, SO, IT'S NOT MEANT FOR MULTIPLE READ/WRITE OPERATIONS!!!

    public static T? GetDuplicateOfObjectUsingJson<T>(T objectToDuplicate) where T : class, new()
    {
        var json = JsonConvert.SerializeObject(objectToDuplicate);

        return JsonConvert.DeserializeObject<T>(json);
    }


    public static async Task<IEnumerable<(string FilePath, T Item)>> ReadObjectsFromJsonFolderAsync<T>(AppFolder appFolder, string relativePathAndFileName = "", CompressionAlgorithm compressionAlgorithm = CompressionAlgorithm.None) where T : class, new()
    {
        string absolutePathAndFileName = GetItemPath(appFolder, relativePathAndFileName);
        if (Directory.Exists(absolutePathAndFileName))
        {
            var list = new List<(string, T)>();
            // deserialize every file, no need for "*.json" as there is no reason for other kind of file be present in the folder!
            foreach (var filename in Directory.EnumerateFiles(absolutePathAndFileName, "*"))
            {
                var filePath = filename.Replace(FolderPaths[appFolder], "", StringComparison.Ordinal);
                var item = await SettingsManager.TryLoading<T>(appFolder, filePath, compressionAlgorithm).ConfigureAwait(false);
                if (item != null)
                    list.Add((filePath, item));
            }
            return list;
        }
        else
            return Enumerable.Empty<(string, T)>();
    }

    public static string GetItemPath(AppFolder appFolder, string relativePathAndItemName, bool includeRootPath = true)
    {
        return includeRootPath ? FolderPaths[appFolder] + relativePathAndItemName : RootlessPaths[appFolder] + relativePathAndItemName;
    }

#if ANDROID
    /// <summary>
    /// Gets the Android Uri for the item (for legacy updating)
    /// </summary>
    /// <param name="appFolder"></param>
    /// <param name="relativePathAndItemName"></param>
    /// <returns></returns>
    public static global::Android.Net.Uri? GetItemAUri(AppFolder appFolder, string relativePathAndItemName)
    {
#if ANDROID
        if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Q && (appFolder == AppFolder.AppPictures || appFolder == AppFolder.Pictures))
        {
            var operationDataOpt = GetOperationDataFor(appFolder, relativePathAndItemName);
            if (!operationDataOpt.HasValue) return null;
            var operationData = operationDataOpt.Value;

            var targetExternalUri = GetMsUri(operationData);

            return targetExternalUri;
        }
#endif
        return Android.Net.Uri.Parse(
#if ANDROID
            "file://" +
#endif
            GetItemPath(appFolder, relativePathAndItemName, includeRootPath: true));
    }
#endif
    
#if !WINDOWS_UWP
    private static async Task<string?> GetItemPathIfExists(AppFolder appFolder, string relativePathAndItemName)
    {
        if (await ExistsAsync(appFolder, relativePathAndItemName))
            return GetItemPath(appFolder, relativePathAndItemName);
        return null;
    }
#endif

    public static 
#if WINDOWS_UWP
        async 
#endif
        Task<bool> ExistsAsync(AppFolder appFolder, string
#if WINDOWS_UWP
            ? 
#endif
            relativePathAndItemName)
    {
#if ANDROID
        if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Q && (appFolder == AppFolder.AppPictures || appFolder == AppFolder.Pictures))
        {
            if (PendingFiles.ContainsKey(new FileReference(appFolder, relativePathAndItemName)))
                return Task.FromResult(true); // exists as non-final

            var operationDataOpt = GetOperationDataFor(appFolder, relativePathAndItemName);
            if (!operationDataOpt.HasValue) return Task.FromResult(false);
            var operationData = operationDataOpt.Value;

            var query = Android.Provider.MediaStore.IMediaColumns.RelativePath + " = ? and " + Android.Provider.MediaStore.IMediaColumns.DisplayName + " = ?";
            var queryArgs = new [] { $"{Path.GetDirectoryName(operationData.RelativeLocation) + Path.DirectorySeparatorChar}", $"{Path.GetFileName(operationData.RelativeLocation)}" };

            var projection = Array.Empty<string>();
            var resultsCursor = operationData.ContentResolver.Query(operationData.ExternalUri, projection, query, queryArgs, null);
            
            return Task.FromResult(resultsCursor is { Count: > 0 }); // file found!
        }
#endif

#if WINDOWS_UWP
        try
        {
            var folder = await CreateOrOpenFolderAsync(appFolder, Path.GetDirectoryName(relativePathAndItemName));
            var item = folder == null ? null : (await folder.TryGetItemAsync(Path.GetFileName(relativePathAndItemName)));
            return item != null;
        }
        catch (UnauthorizedAccessException ex)
        {
            // permissions issue
            Log.Error(ex, "Failed to check if file exists");
        }
        return false; // permissions issue
#elif ANDROID
        var absolutePath = GetItemPath(appFolder, relativePathAndItemName);
        var oldFile = new Java.IO.File(Path.GetDirectoryName(absolutePath), Path.GetFileName(relativePathAndItemName));
        return Task.FromResult(oldFile.Exists());
#else
        var absolutePath = GetItemPath(appFolder, relativePathAndItemName);
        return Task.FromResult(File.Exists(absolutePath));
#endif
    }

#if ANDROID
    private static global::Android.Net.Uri? GetMsUri(AndroidStoreFileOperation operationData)
    {
        var query = Android.Provider.MediaStore.IMediaColumns.RelativePath + " = ? and " + Android.Provider.MediaStore.IMediaColumns.DisplayName + " = ?";
        var queryArgs = new [] { $"{Path.GetDirectoryName(operationData.RelativeLocation) + Path.DirectorySeparatorChar}", $"{Path.GetFileName(operationData.RelativeLocation)}" };

        var projection = new [] { Android.Provider.IBaseColumns.Id };
        var resultsCursor = operationData.ContentResolver.Query(operationData.ExternalUri, projection, query, queryArgs, null);
        if (resultsCursor != null) 
        {
            resultsCursor.MoveToNext();
            return ContentUris.WithAppendedId(operationData.ExternalUri, resultsCursor.GetLong(0));
        } 
        else 
        {
            return null;
        }
    }
#endif

    public static async Task<bool> RenameAsync(AppFolder appFolder, string relativePathAndItemName, string newFileName)
    {
#if ANDROID
        if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Q && (appFolder == AppFolder.AppPictures || appFolder == AppFolder.Pictures))
        {
            var operationDataOpt = GetOperationDataFor(appFolder, relativePathAndItemName);
            if (!operationDataOpt.HasValue) return false;
            var operationData = operationDataOpt.Value;

            var newValues = new ContentValues();
            newValues.Put(Android.Provider.MediaStore.IMediaColumns.DisplayName, newFileName);
            bool wasPending = relativePathAndItemName.EndsWith(".part") && !newFileName.EndsWith(".part");

            if (wasPending && PendingFiles.TryGetValue(new FileReference(appFolder, relativePathAndItemName), out var pendingUri))
            {
                PendingFiles.Remove(new FileReference(appFolder, relativePathAndItemName)); // remove it from pending list
                newValues.Put(Android.Provider.MediaStore.IMediaColumns.IsPending, false); // mark it as not pending

                // clear the pending flag or the query won't find it
                var result = operationData.ContentResolver.Update(pendingUri, newValues, null, null);
                return result > 0;
            }
            else
            {
                var targetExternalUri = GetMsUri(operationData);
                if (targetExternalUri == null) return false;

                var result = operationData.ContentResolver.Update(targetExternalUri, newValues, null, null);
                return result > 0;
            }
        }
#endif

#if WINDOWS_UWP
        var folder = await CreateOrOpenFolderAsync(appFolder, Path.GetDirectoryName(relativePathAndItemName));
        var item = folder == null ? null : (await folder.TryGetItemAsync(Path.GetFileName(relativePathAndItemName)));
        if (item != null)
        {
            await item.RenameAsync(newFileName);
            return true;
        }
#elif ANDROID
        var absolutePath = await GetItemPathIfExists(appFolder, relativePathAndItemName);
        if (absolutePath != null)
        {
            var oldFile = new Java.IO.File(Path.GetDirectoryName(absolutePath), Path.GetFileName(relativePathAndItemName));
            var newFile = new Java.IO.File(Path.GetDirectoryName(absolutePath), newFileName);
            oldFile.RenameTo(newFile);
            return true;
        }
#else
        var absolutePath = await GetItemPathIfExists(appFolder, relativePathAndItemName);
        if (absolutePath != null)
        {
            File.Move(absolutePath, Path.GetDirectoryName(absolutePath) + Path.DirectorySeparatorChar + newFileName);
            return true;
        }
#endif
        else
        {
            Log.Debug($"Attempted to rename non existing file or folder: {appFolder}/{relativePathAndItemName}");
            return false;
        }
    }

    public static async Task<bool> MoveFileAsync(AppFolder appFolder, string relativePathAndItemName, AppFolder newAppFolder, string newRelativePathAndItemName, bool replaceIfExists = true)
    {
#if WINDOWS_UWP
        if (!await ExistsAsync(appFolder, relativePathAndItemName).ConfigureAwait(false))
        {
            Log.Debug($"Attempted to rename non existing file: {appFolder}/{relativePathAndItemName}");
            return false; // do nothing, source file doesn't exist
        }
        if (!await ExistsAsync(newAppFolder, Path.GetDirectoryName(newRelativePathAndItemName)).ConfigureAwait(false))
        {
            Log.Debug($"Attempted to move to non-existing folder: {newAppFolder}/{newRelativePathAndItemName}");
            return false; // do nothing, target folder doesn't exist
        }

        var oldFolder = await CreateOrOpenFolderAsync(appFolder, Path.GetDirectoryName(relativePathAndItemName)).ConfigureAwait(false);
        var item = oldFolder == null ? null : (await oldFolder.GetFileAsync(Path.GetFileName(relativePathAndItemName)));
        if (item != null)
        {
            var newFolder = await CreateOrOpenFolderAsync(newAppFolder, Path.GetDirectoryName(newRelativePathAndItemName)).ConfigureAwait(false);
            if (newFolder == null) return false;
            if (replaceIfExists && await ExistsAsync(newAppFolder, newRelativePathAndItemName).ConfigureAwait(false))
                await item.MoveAndReplaceAsync(await newFolder.GetFileAsync(Path.GetFileName(newRelativePathAndItemName)));
            else
                await item.MoveAsync(newFolder);
            return true;
        }
#elif ANDROID
        var absolutePath = await GetItemPathIfExists(appFolder, relativePathAndItemName);
        var targetDirectoryPath = GetItemPath(newAppFolder, Path.GetDirectoryName(newRelativePathAndItemName) ?? string.Empty);
        if (absolutePath != null)
        {
            var oldFile = new Java.IO.File(Path.GetDirectoryName(absolutePath), Path.GetFileName(relativePathAndItemName));
            var newFile = new Java.IO.File(targetDirectoryPath, Path.GetFileName(newRelativePathAndItemName));
            oldFile.RenameTo(newFile);
            return true;
        }
#else
        var absolutePath = await GetItemPathIfExists(appFolder, relativePathAndItemName);
        var targetAbsolutePath = GetItemPath(newAppFolder, newRelativePathAndItemName);
        if (absolutePath != null)
        {
            File.Move(absolutePath, targetAbsolutePath);
            return true;
        }
#endif
        else
        {
            Log.Debug($"Attempted to rename non existing file or folder: {appFolder}/{relativePathAndItemName}");
            return false;
        }
    }

    public static async Task<bool> DeleteAsync(AppFolder appFolder, string relativePathAndItemName)
    {
#if ANDROID
        if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Q && (appFolder == AppFolder.AppPictures || appFolder == AppFolder.Pictures))
        {
            var operationDataOpt = GetOperationDataFor(appFolder, relativePathAndItemName);
            if (!operationDataOpt.HasValue) return false;
            var operationData = operationDataOpt.Value;

            var targetExternalUri = GetMsUri(operationData);
            if (targetExternalUri == null) return false;

            int result = operationData.ContentResolver.Delete(targetExternalUri, null, null);
            return result > 0;
        }
#endif
#if WINDOWS_UWP
        var folder = await CreateOrOpenFolderAsync(appFolder, Path.GetDirectoryName(relativePathAndItemName));
        var item = folder == null ? null : (await folder.TryGetItemAsync(Path.GetFileName(relativePathAndItemName)));
        if (item != null)
        {
            await item.DeleteAsync(Windows.Storage.StorageDeleteOption.PermanentDelete);
            return true;
        }
#elif ANDROID
        var absolutePath = await GetItemPathIfExists(appFolder, relativePathAndItemName);
        if (absolutePath != null)
        {
            var oldFile = new Java.IO.File(Path.GetDirectoryName(absolutePath), Path.GetFileName(relativePathAndItemName));

            if (oldFile.IsDirectory)
            {
                foreach (var file in (await oldFile.ListFilesAsync()) ?? Enumerable.Empty<Java.IO.File>())
                {
                    file.Delete();
                    await Task.Yield();
                }
            }
            oldFile.Delete();
            return true;
        }
#else
        var absolutePath = await GetItemPathIfExists(appFolder, relativePathAndItemName);
        if (absolutePath != null)
        {
            File.Delete(absolutePath);
            return true;
        }
#endif
        else
        {
            Log.Debug($"Attempted to delete non existing file or folder: {appFolder}/{relativePathAndItemName}");
            return false;
        }
    }
#if !WINDOWS_UWP
    public static async Task<bool> UpdateModifiedTimestampAsync(AppFolder appFolder, string relativePathAndItemName)
#else
    public static Task<bool> UpdateModifiedTimestampAsync(AppFolder appFolder, string relativePathAndItemName)
#endif
    {
#if ANDROID
        if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Q && (appFolder == AppFolder.AppPictures || appFolder == AppFolder.Pictures))
        {
            var operationDataOpt = GetOperationDataFor(appFolder, relativePathAndItemName);
            if (!operationDataOpt.HasValue) return false;
            var operationData = operationDataOpt.Value;
            var targetExternalUri = GetMsUri(operationData);
            if (targetExternalUri == null) return false;

            var newValues = new ContentValues();
            newValues.Put(Android.Provider.MediaStore.IMediaColumns.IsPending, true); // there is no reason to rename a pending file
            var result = operationData.ContentResolver.Update(targetExternalUri, newValues, null, null); // required to toggle to notify a change will be performed
            if (result <= 0) return false;
            
            newValues.Put(Android.Provider.MediaStore.IMediaColumns.DateAdded, Java.Lang.JavaSystem.CurrentTimeMillis() / 1000);
            newValues.Put(Android.Provider.MediaStore.IMediaColumns.DateModified, Java.Lang.JavaSystem.CurrentTimeMillis() / 1000);
            newValues.Put(Android.Provider.MediaStore.IMediaColumns.IsPending, false); // there is no reason to rename a pending file
            result = operationData.ContentResolver.Update(targetExternalUri, newValues, null, null);
            return result > 0;
        }
#endif

#if ANDROID
        var absolutePath = await GetItemPathIfExists(appFolder, relativePathAndItemName);
        if (absolutePath != null)
        {
            var oldFile = new Java.IO.File(Path.GetDirectoryName(absolutePath), Path.GetFileName(relativePathAndItemName));
            oldFile.SetLastModified(Java.Lang.JavaSystem.CurrentTimeMillis());
            return true;
        }
        else
        {
            return false;
        }
#elif WINDOWS_UWP
        return Task.FromResult(true); // nothing to do, would throw an unauthorized access due to using this API for it
#else
        var item = await FileManager.GetItemPathIfExists(appFolder, relativePathAndItemName);
        try
        {
            if (item != null)
            {
                File.SetCreationTime(item, DateTime.Now);
                File.SetLastWriteTime(item, DateTime.Now);
                return true;
            }
            else
            {
                Log.Debug($"Attempted to update timestamp for non existing file or folder: {appFolder}/{relativePathAndItemName}");
                return false;
            }
        } 
        catch (Exception ex)
        {
            Log.Debug(ex, "Failed to update timestamp for file");
            return false;
        }
#endif
    }
}
