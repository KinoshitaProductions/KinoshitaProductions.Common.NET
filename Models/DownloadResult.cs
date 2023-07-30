namespace KinoshitaProductions.Common.Models;

#if WINDOWS_UWP
using Windows.Storage.Streams;
#endif

public sealed class DownloadResult : IDisposable
#if !WINDOWS_UWP
, IAsyncDisposable
#endif
{
    /// <summary>
    /// If true, the Result stream will not be disposed together the object.
    /// </summary>
    public bool LeaveOpen { get; set; }
    // ReSharper disable once MemberCanBePrivate.Global
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public DownloadResultStatus Status { get;
        private
#if NET7_0_OR_GREATER
        init;
#else
        set;
#endif
    }
#if WINDOWS_UWP
    private IRandomAccessStream? _result;
    // ReSharper disable once MemberCanBePrivate.Global
    public IRandomAccessStream? Result { get => this._result; private 
#if NET7_0_OR_GREATER
        init
#else
        set
#endif
        => this._result = value; }
#else
    private Stream? _result;
    // ReSharper disable once MemberCanBePrivate.Global
    public Stream? Result { get => _result; private init => _result = value; }
#endif
    private string? _errorMessage;
    // ReSharper disable once MemberCanBePrivate.Global
    public string? ErrorMessage { get => _errorMessage; private 
#if NET7_0_OR_GREATER
        init
#else
        set
#endif
        => _errorMessage = value; }

#if WINDOWS_UWP
    public DownloadResult(DownloadResultStatus status, MemoryStream result)
    {
        this.Status = status;
        this.Result = result.AsRandomAccessStream();
    }
    public DownloadResult(DownloadResultStatus status, Stream result)
    {
        this.Status = status;
        this.Result = result.AsRandomAccessStream();
    }
    public DownloadResult(DownloadResultStatus status, IRandomAccessStream result)
    {
        this.Status = status;
        this.Result = result;
    }
#else
    public DownloadResult(DownloadResultStatus status, Stream result)
    {
        Status = status;
        Result = result;
    }
#endif

    public DownloadResult(DownloadResultStatus status)
    {
        Status = status;
    }

    public DownloadResult(string errorMessage)
    {
        Status = DownloadResultStatus.Error;
        ErrorMessage = errorMessage;
    }

    public void Dispose()
    {
#if WINDOWS_UWP
        if (LeaveOpen) _result = null;
#endif
        if (_result != null)
        {
            _result.Dispose();
            _result = null;
        }
        _errorMessage = null;
    }
#if !WINDOWS_UWP
    public async ValueTask DisposeAsync()
    {
        await DisposeAsync(true);
    }
    private async ValueTask DisposeAsync(bool disposing)
    {
        if (!disposing) return;
        if (_result != null)
        {
            await _result.DisposeAsync();
            _result = null;
        }
        _errorMessage = null;
    }
#endif
}
