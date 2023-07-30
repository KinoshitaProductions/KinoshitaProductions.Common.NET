// ReSharper disable MemberCanBePrivate.Global
namespace KinoshitaProductions.Common.Services;

#if WINDOWS_UWP
using Windows.Storage.Streams;
#endif

public class LruCache<TCacheType> where TCacheType : notnull
{
    protected virtual long GetCacheLimitInBytesFor(TCacheType type)
    {
        switch (type)
        {
            default:
                return 0;
        }
    }

    private static readonly Dictionary<TCacheType, Dictionary<string, CacheRegister>> CacheIndex = new ();

    public bool TryRemove(string key)
    {
        List<Dictionary<string, CacheRegister>> knownDicts;
        lock (CacheIndex)
        {
            knownDicts = CacheIndex.Select(tuple => tuple.Value).ToList();
        }
        foreach (var dict in knownDicts)
        {
            lock (dict)
            {
                if (dict.TryGetValue(key, out var cacheRegister))
                {
                    dict.Remove(key);
                    return true;
                }
            }
        }
        return false;
    }
    public bool TryLookup(Uri key, out byte[] bytes)
    {
        bytes = Lookup(key.AbsoluteUri);
        return bytes.Any();
    }
    public byte[] Lookup(Uri key)
    {
        return Lookup(key.AbsoluteUri);
    }
    public static bool TryLookup(string key, out byte[] bytes)
    {
        bytes = Lookup(key);
        return bytes.Any();
    }
    public static byte[] Lookup(string key)
    {
        List<Dictionary<string, CacheRegister>> knownDicts;
        lock (CacheIndex)
        {
            knownDicts = CacheIndex.Select(tuple => tuple.Value).ToList();
        }
        foreach (var dict in knownDicts)
        {
            lock (dict)
            {
                if (dict.TryGetValue(key, out var cacheRegister))
                {
                    //we found one, return it!
                    return cacheRegister.Item ?? Array.Empty<byte>();
                }
            }
        }
        return Array.Empty<byte>();
    }

#if WINDOWS_UWP
    public async Task<bool> Store(TCacheType type, IRandomAccessStream item, Uri key)
    {
        return await Store(type, item, key.AbsoluteUri).ConfigureAwait(false);
    }
    public async Task<bool> Store(TCacheType type, IRandomAccessStream item, string key)
    {
        return await Store(type, item.AsStream(), key).ConfigureAwait(false);
    }
#endif

    public async Task<bool> Store(TCacheType type, Stream item, Uri key)
    {
        return await Store(type, item, key.AbsoluteUri).ConfigureAwait(false);
    }

    public async Task<bool> Store(TCacheType type, Stream item, string key)
    {
        // check if it can be cached
        if (GetCacheLimitInBytesFor(type) <= 0 || GetCacheLimitInBytesFor(type) / 2 < item.Length)
        {
            return false;
        }

        // read bytes for caching
        var bytes = await StreamHelper.ReadFullyAsBytesAsync(item).ConfigureAwait(false);

        // seek to origin since it's probably still to be used somewhere else
        item.Seek(0, SeekOrigin.Begin);

        // create new stream for result
        return Store(type, bytes, key);
    }

    public bool Store(TCacheType type, byte[] item, string key)
    {
        if (GetCacheLimitInBytesFor(type) <= 0 || GetCacheLimitInBytesFor(type) / 2 < item.LongLength)
        {
            return false;
        }

        Dictionary<string, CacheRegister>? dict;

        lock (CacheIndex)
        {
            //first, we lookup for the data type, and if exists, get the list
            if (!CacheIndex.TryGetValue(type, out dict))
            {
                //if it does not exist, we create a new one and add it
                dict = new Dictionary<string, CacheRegister>();
                CacheIndex.Add(type, dict);
            }
        }

        lock (dict)
        {
            //then, create the cache register
            var cacheRegister = new CacheRegister(item);

            //check if there is enough space
            var usedBytes = dict.Values.Sum(x => x.SizeInBytes);
            long requiredSpace = usedBytes + cacheRegister.SizeInBytes - GetCacheLimitInBytesFor(type);
            
            //if not enough space, free up some
            if (requiredSpace > 0)
            {
                var sortedValues = dict.OrderBy(entry => entry.Value.LastUsed).ToList(); //idk if this sorts inversely
                foreach (var removing in sortedValues)
                {
                    if (requiredSpace <= 0) break;
                    requiredSpace -= removing.Value.SizeInBytes;
                    dict.Remove(removing.Key);
                }
            }

            //add it to cache list
            dict.TryAdd(key, cacheRegister);

            return true;
        }
    }
    
    private sealed class CacheRegister
    {
        public CacheRegister(byte[] item)
        {
            Item = item;
            LastUsed = DateTime.Now;
        }
        
        private byte[]? _item;

        /// <summary>
        /// Gets the cached item.
        /// It must not be an stream for multithreading safety, and preferably compressed for memory saving.
        /// </summary>
        public byte[]? Item
        {
            get
            {
                LastUsed = DateTime.Now; // update last used time
                return _item;
            }
            private set => _item = value;
        }
        /// <summary>
        /// Gets the last time used date.
        /// </summary>
        public DateTime LastUsed { get; private set; }
        
        public long SizeInBytes => Item?.LongLength ?? 64;
    }
}
