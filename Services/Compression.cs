namespace KinoshitaProductions.Common.Services;

public enum CompressionAlgorithm
{
    None,
    Deflate,
    GZip,
    GZipFast,
}
public static class Compression
{
    public static async Task<Stream> GetSeekableStreamFrom(Stream inputStream)
    {
        var newStream = new MemoryStream();
        switch (inputStream)
        {
            case System.IO.Compression.GZipStream gzipStream:
                await gzipStream.CopyToAsync(newStream);
                newStream.Seek(0, SeekOrigin.Begin);
                break;
            case System.IO.Compression.DeflateStream deflateStream:
                await deflateStream.CopyToAsync(newStream);
                newStream.Seek(0, SeekOrigin.Begin);
                break;
            default:
                return inputStream;
        }
        return newStream;
    }

    public static Stream GetDecompressionStreamFor(Stream sourceStream, CompressionAlgorithm algorithm)
    {
        switch (algorithm)
        {
            case CompressionAlgorithm.None:
                return sourceStream;
            case CompressionAlgorithm.Deflate:
                return new System.IO.Compression.DeflateStream(sourceStream, System.IO.Compression.CompressionMode.Decompress);
            case CompressionAlgorithm.GZip:
            case CompressionAlgorithm.GZipFast:
                return new System.IO.Compression.GZipStream(sourceStream, System.IO.Compression.CompressionMode.Decompress);
            default:
                throw new ArgumentException("ATTEMPTING TO USE AN UNKNOWN COMPRESSION ALGORITHM");
        }
    }

#if WINDOWS_UWP
    public static Stream GetDecompressionStreamFor(Windows.Storage.Streams.IInputStream sourceStream, CompressionAlgorithm algorithm)
    {
        return GetDecompressionStreamFor(sourceStream.AsStreamForRead(), algorithm);
    }
#endif

    public static Stream GetCompressionStreamFor(Stream sourceStream, CompressionAlgorithm algorithm)
    {
        return algorithm switch
        {
            CompressionAlgorithm.None => sourceStream,
            CompressionAlgorithm.Deflate => new System.IO.Compression.DeflateStream(sourceStream,
                System.IO.Compression.CompressionLevel.Optimal),
            CompressionAlgorithm.GZip => new System.IO.Compression.GZipStream(sourceStream,
                System.IO.Compression.CompressionLevel.Optimal),
            CompressionAlgorithm.GZipFast => new System.IO.Compression.GZipStream(sourceStream,
                System.IO.Compression.CompressionLevel.Fastest),
            _ => throw new ArgumentException("ATTEMPTING TO USE AN UNKNOWN COMPRESSION ALGORITHM")
        };
    }

#if WINDOWS_UWP
    public static Stream GetCompressionStreamFor(Windows.Storage.Streams.IInputStream sourceStream, CompressionAlgorithm algorithm)
    {
        return GetCompressionStreamFor(sourceStream.AsStreamForRead(), algorithm);
    }
#endif
}
