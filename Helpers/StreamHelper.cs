namespace KinoshitaProductions.Common.Helpers;

using System.Text;

public static class StreamHelper
{
    public static async Task<byte[]> ReadFullyAsBytesAsync(Stream input)
    {
        if (input is MemoryStream sourceMs)
            return sourceMs.ToArray();
        using var ms = new MemoryStream();
        await input.CopyToAsync(ms);
        return ms.ToArray();
    }

    public static async Task<Stream> ReadFullyAsSeekableStreamAsync(Stream input)
    {
        if (input.CanSeek)
            return input;
#if NET7_0_OR_GREATER
        await 
#endif
        using var inputStream = input;
        var ms = new MemoryStream();
        await inputStream.CopyToAsync(ms).ConfigureAwait(false);
        ms.Seek(0, SeekOrigin.Begin);
        return ms;
    }

    public static async Task<string> ReadFullyAsStringAsync(Stream input, bool leaveOpen = false)
    {
        using var reader = new StreamReader(input, Encoding.UTF8, false, 16 * 1024, leaveOpen);
        var str = await reader.ReadToEndAsync().ConfigureAwait(false);
        return str;
    }
}
