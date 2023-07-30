namespace KinoshitaProductions.Common.Helpers;

using System.Globalization;

public static class ConversionHelper
{
    public static string ConvertBytesCountToString(long bytesCount)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        int[] precisions = { 0, 0, 1, 2, 3 };
        var estSize = (double)bytesCount;
        int order = 0;
        while (estSize >= 1024.0 && order < sizes.Length - 1)
        {
            order++;
            estSize = estSize / 1024.0;
        }

        return string.Format(CultureInfo.InvariantCulture, $"{{0:F{precisions[order]}}} {{1}}", estSize,
            sizes[order]);
    }
}
