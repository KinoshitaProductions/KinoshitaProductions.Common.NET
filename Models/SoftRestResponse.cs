// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace KinoshitaProductions.Common.Models;

#if WINDOWS_UWP
using Windows.Web.Http;
#else
using System.Net;
#endif

public class JsonRestResponse<T> where T : class
{
    public HttpStatusCode Status { get; set; }
    private string? _message;
    public string? Message
    {
        get => _message;
        set
        {
            _message = value;
            if (!string.IsNullOrWhiteSpace(value) && string.Compare("OK", value, StringComparison.Ordinal) != 0)
            {
#pragma warning disable CS8604
                Result = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(value);
#pragma warning restore CS8604
            }
        }
    }
    public string? Error { get; set; }
    public T? Result { get; set; }
    public bool IsSuccess => Error == null && (int)Status != 0 && (int)Status <= 300;
}
