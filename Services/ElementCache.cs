namespace KinoshitaProductions.Common.Services;

// THIS CLASS IS MEANT TO STORE UNIQUE ITEMS WHICH ARE SHARED (NOT REUSABLE) BY THEIR TYPE AND SETTINGS
public static class ElementCacheDict<TU, T> where T : class where TU : notnull
{
    private static readonly Dictionary<TU, T> SharedElementCache = new();
    public static T? GetSharedElement(TU key)
    {
        lock (SharedElementCache)
        {
            return SharedElementCache.TryGetValue(key, out var value) ? value : null;
        }
    }
    public static T StoreSharedElement(TU key, T element)
    {
        lock (SharedElementCache)
        {
            return SharedElementCache[key] = element;
        }
    }
}

public static class ElementCache<T> where T : class
{
    private static T? _sharedElementCacheSingleton;
    public static T? GetSharedElement()
    {
        return _sharedElementCacheSingleton;
    }
    public static T StoreSharedElement(T element)
    {
        return _sharedElementCacheSingleton = element;
    }
}

public static class ElementCache
{
    public static T GetSharedOrStoreNewElement<TU, T>(TU key) where T : class, new() where TU : notnull
    {
        return ElementCacheDict<TU, T>.GetSharedElement(key) ?? ElementCacheDict<TU, T>.StoreSharedElement(key, new T());
    }

    public static T GetSharedOrStoreNewElement<T>() where T : class, new()
    {
        return ElementCache<T>.GetSharedElement() ?? ElementCache<T>.StoreSharedElement(new T());
    }
}
