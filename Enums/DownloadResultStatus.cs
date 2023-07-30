namespace KinoshitaProductions.Common.Enums;

public enum DownloadResultStatus
{
    Success, // Download finished successfully
    Invalid, // should not retry, like error 403, 404, etc.
    Cancelled, // should not retry
    Error // should retry, like error 500
}
