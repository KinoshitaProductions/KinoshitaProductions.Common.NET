namespace KinoshitaProductions.Common.Helpers;

using System.Security.Cryptography;
using System.Text;

public static class ChecksumHelper
{
    public static string HexToBase64(string hexStr)
    {
        var byteArray = new byte[hexStr.Length / 2];
        for (var i = 0; i < byteArray.Length; i++)
        {
            byteArray[i] = Convert.ToByte(hexStr.Substring(i * 2, 2), 16);
        }
        return Convert.ToBase64String(byteArray);
    }
#if NET7_0_OR_GREATER
    public static string GetChecksumFor(string input, ChecksumAlgorithm checksumAlgorithm)
    {
        switch (checksumAlgorithm)
        {
            case ChecksumAlgorithm.Md5:
                var data = MD5.HashData(Encoding.UTF8.GetBytes(input));
                var sb = new StringBuilder();
                foreach (var b in data)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            case ChecksumAlgorithm.Sha256:
                data = SHA256.HashData(Encoding.UTF8.GetBytes(input));
                sb = new StringBuilder();
                foreach (var b in data)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            case ChecksumAlgorithm.Sha256X2:
                data = SHA256.HashData(SHA256.HashData(Encoding.UTF8.GetBytes(input)));
                sb = new StringBuilder();
                foreach (var b in data)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            case ChecksumAlgorithm.Unknown:
            default:
                return input;
        }
    }
#else
    public static string GetChecksumFor(string? input, ChecksumAlgorithm checksumAlgorithm)
    {
        input = input ?? string.Empty; // null should be hashed as an empty string
        switch (checksumAlgorithm)
        {
            case ChecksumAlgorithm.Md5:
                using (MD5 md5Hash = MD5.Create())
                {
                    var data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));
                    var sb = new StringBuilder();
                    foreach (var t in data)
                    {
                        sb.Append(t.ToString("x2"));
                    }
                    return sb.ToString();
                }
            case ChecksumAlgorithm.Sha256:
                using (SHA256 sha256Hash = SHA256.Create())
                {
                    var data = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(input));
                    var sb = new StringBuilder();
                    foreach (var t in data)
                    {
                        sb.Append(t.ToString("x2"));
                    }
                    return sb.ToString();
                }
            case ChecksumAlgorithm.Sha256X2:
                using (SHA256 sha256Hash = SHA256.Create())
                {
                    var data = sha256Hash.ComputeHash(sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(input)));
                    var sb = new StringBuilder();
                    foreach (var t in data)
                    {
                        sb.Append(t.ToString("x2"));
                    }
                    return sb.ToString();
                }
            case ChecksumAlgorithm.Unknown:
            default:
                return input;
        }
    }
#endif
}
