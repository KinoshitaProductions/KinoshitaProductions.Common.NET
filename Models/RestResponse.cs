// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace KinoshitaProductions.Common.Models;

#if WINDOWS_UWP
using Windows.Web.Http;
#else
using System.Net;
#endif

public interface IRestResponse
{
    public bool IsSuccessStatusCode { get; }
    HttpStatusCode Status { get; }
    string? ActionRequiredCode { get; }
    string? Message { get; }
    string? Error { get; }
}
public interface IRestResponse<out T> : IRestResponse
{
    T? Result { get; }
}
public class RestResponse : IRestResponse
{
    public static RestResponse BlankResponse => new RestResponse();
#if WINDOWS_UWP
    public bool IsSuccessStatusCode => Status == HttpStatusCode.Ok;
#else
    public bool IsSuccessStatusCode => Status == HttpStatusCode.OK;
#endif
    public HttpStatusCode Status { get; 
#if NET7_0_OR_GREATER
        init
#else
        set
#endif
        ; }
    public string? ActionRequiredCode { get; 
#if NET7_0_OR_GREATER
        init
#else
        set
#endif
        ; }
    public string? Message { get; 
#if NET7_0_OR_GREATER
        init
#else
        set
#endif
        ; }
    public string? Error { get; 
#if NET7_0_OR_GREATER
        init
#else
        set
#endif
        ; }
}
public class RestResponse<T> : IRestResponse<T> where T : class
{
    public static RestResponse<T> BlankResponse => new RestResponse<T>();
#if WINDOWS_UWP
    public bool IsSuccessStatusCode => Status == HttpStatusCode.Ok;
#else
    public bool IsSuccessStatusCode => Status == HttpStatusCode.OK;
#endif
    public HttpStatusCode Status { get; 
#if NET7_0_OR_GREATER
        init
#else
        set
#endif
        ; }
    public string? ActionRequiredCode { get; 
#if NET7_0_OR_GREATER
        init
#else
        set
#endif
        ; }
    public string? Message { get; 
#if NET7_0_OR_GREATER
        init
#else
        set
#endif
        ; }
    public string? Error { get; 
#if NET7_0_OR_GREATER
        init
#else
        set
#endif
        ; }
    public T? Result { get; 
#if NET7_0_OR_GREATER
        init
#else
        set
#endif
        ; }
}

public class RestResponseDisposable<T> : IRestResponse<T> where T : class, IDisposable
{
    public static RestResponse<T> BlankResponse => new RestResponse<T>();
#if WINDOWS_UWP
    public bool IsSuccessStatusCode => Status == HttpStatusCode.Ok;
#else
    public bool IsSuccessStatusCode => Status == HttpStatusCode.OK;
#endif
    public HttpStatusCode Status { get; 
#if NET7_0_OR_GREATER
        init
#else
        set
#endif
        ; }
    public string? ActionRequiredCode { get; 
#if NET7_0_OR_GREATER
        init
#else
        set
#endif
        ; }
    public string? Message { get; 
#if NET7_0_OR_GREATER
        init
#else
        set
#endif
        ; }
    public string? Error { get; 
#if NET7_0_OR_GREATER
        init
#else
        set
#endif
        ; }
    public T? Result { get; 
#if NET7_0_OR_GREATER
        init
#else
        set
#endif
        ; }
}
