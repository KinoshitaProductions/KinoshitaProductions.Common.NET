// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable MemberCanBePrivate.Global
namespace KinoshitaProductions.Common.Models;

public class ObjectAndJsonTuple<T>
{
    internal ObjectAndJsonTuple(T @object, string json) {
        Object = @object;
        Json = json;
    }
    public T Object { get; 
#if NET7_0_OR_GREATER
        init
#else
        set
#endif
        ; }
    public string Json { get; 
#if NET7_0_OR_GREATER
        init
#else
        set
#endif
        ; }
}
