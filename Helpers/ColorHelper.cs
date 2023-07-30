// ReSharper disable MemberCanBePrivate.Global

namespace KinoshitaProductions.Common.Helpers;

using System.Globalization;
#if ANDROID
using Android.Graphics;
#elif WINDOWS_UWP
using Windows.UI;
#else
using System.Drawing;
#endif

public static class ColorHelper
{
#if ANDROID
    public static (int A, int R, int G, int B)
    GetGenericColor(Color color)
    {
        return (color.A, color.R, color.G, color.B);
    }
#endif
    public static Color MultiplyAlpha(Color source, double multiplier)
    {
#if ANDROID
        source.A = Math.Clamp((byte)(source.A * multiplier), (byte)0, (byte)255);
        return source;
#elif WINDOWS_UWP
        source.A = Math.Clamp((byte)(source.A * multiplier), (byte)0, (byte)255);
        return source;
#else
        return Color.FromArgb(Math.Clamp((byte)(source.A * multiplier), (byte)0, (byte)255), source.R, source.G, source.B);
#endif
    }

    public static Color SetAlpha(Color source, double alpha)
    {
#if ANDROID
        source.A = Math.Clamp((byte)(alpha * 255), (byte)0, (byte)255);
        return source;
#elif WINDOWS_UWP
        source.A = Math.Clamp((byte)(alpha * 255), (byte)0, (byte)255);
        return source;
#else
        return Color.FromArgb(Math.Clamp((byte)(alpha * 255), (byte)0, (byte)255), source.R, source.G, source.B);
#endif
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public static (int A, int R, int G, int B)
        BlendColors(
            (int A, int R, int G, int B) colorA,
            (int A, int R, int G, int B) colorB,
            double blendPercentage
        )
    {
        blendPercentage = Math.Clamp(blendPercentage, 0.0, 1.0);
        (int A, int R, int G, int B) blendColor =
        (
            (
                int
            )(colorB.A * blendPercentage +
              colorA.A * (1.0 - blendPercentage)),
            (
                int
            )(colorB.R * blendPercentage +
              colorA.R * (1.0 - blendPercentage)),
            (
                int
            )(colorB.G * blendPercentage +
              colorA.G * (1.0 - blendPercentage)),
            (
                int
            )(colorB.B * blendPercentage +
              colorA.B * (1.0 - blendPercentage))
        );

        blendColor.A = Math.Clamp(blendColor.A, 0, 255);
        blendColor.R = Math.Clamp(blendColor.R, 0, 255);
        blendColor.G = Math.Clamp(blendColor.G, 0, 255);
        blendColor.B = Math.Clamp(blendColor.B, 0, 255);
        return blendColor;
    }

    public static (int A, int R, int G, int B)
        BlendColors(
            (int A, int R, int G, int B) colorA,
            (int A, int R, int G, int B) colorB,
            double blendPercentage,
            int steps
        )
    {
        steps = Math.Max(1, steps);

        double stepSize = 1.0 / steps;
        double stepsCount = Math.Round(blendPercentage / stepSize);
        blendPercentage = stepSize * stepsCount;

        return BlendColors(colorA, colorB, blendPercentage);
    }

#if ANDROID
    public static Color
    BlendColors(Color colorA, Color colorB, double blendPercentage)
    {
        var blendColor =
            BlendColors((colorA.A, colorA.R, colorA.G, colorA.B),
            (colorB.A, colorB.R, colorB.G, colorB.B),
            blendPercentage);
        return Color
            .Argb(blendColor.A, blendColor.R, blendColor.G, blendColor.B);
    }

    public static Color
    BlendColors(
        Color colorA,
        Color colorB,
        double blendPercentage,
        int steps
    )
    {
        var blendColor =
            BlendColors((colorA.A, colorA.R, colorA.G, colorA.B),
            (colorB.A, colorB.R, colorB.G, colorB.B),
            blendPercentage,
            steps);
        return Color
            .Argb(blendColor.A, blendColor.R, blendColor.G, blendColor.B);
    }

    public static Color? PlatformColorOrNullFromHexString(string hex)
    {
        var color = ColorOrNullFromHexString(hex);
        if (!color.HasValue)
            return null;
        else
            return Color
                .Argb(color.Value.A,
                color.Value.R,
                color.Value.G,
                color.Value.B);
    }

    public static Color PlatformColorFromHexString(string hex)
    {
        var color = ColorFromHexString(hex);
        return Color
            .Argb(color.A,
            color.R,
            color.G,
            color.B);
    }

    public static Color
    GetPlatformColor((int A, int R, int G, int B) color)
    {
        return Color.Argb(color.A, color.R, color.G, color.B);
    }
#elif WINDOWS_UWP
    public static Color BlendColors(Color colorA, Color colorB, double blendPercentage)
    {
        var blendColor =
            BlendColors((colorA.A, colorA.R, colorA.G, colorA.B),
                (colorB.A, colorB.R, colorB.G, colorB.B),
                blendPercentage);
        return Color
            .FromArgb((byte)blendColor.A, (byte)blendColor.R, (byte)blendColor.G, (byte)blendColor.B);
    }

    public static (int A, int R, int G, int B) GetUniversalColor(Color color)
    {
        return (color.A, color.R, color.G, color.B);
    }

    public static Color GetPlatformColor((int A, int R, int G, int B) color)
    {
        return Color.FromArgb((byte)color.A, (byte)color.R, (byte)color.G, (byte)color.B);
    }

    public static Color PlatformColorFromHexString(string hex)
    {
        var color = ColorFromHexString(hex);
        return Color
            .FromArgb((byte)color.A,
                (byte)color.R,
                (byte)color.G,
                (byte)color.B);
    }
#endif

    public static (int A, int R, int G, int B)? ColorOrNullFromHexString(string hex)
    {
        if (String.IsNullOrWhiteSpace(hex)) return null;
        hex = hex.Replace("0x", "").TrimStart('#');

        if (hex.Length == 6)
            hex = "FF" + hex;

        if (
            Int32
            .TryParse(hex,
                NumberStyles.HexNumber,
                CultureInfo.InvariantCulture,
                out var argb)
        )
        {
            int a = argb >> 24 & 0xFF;
            int r = argb >> 16 & 0xFF;
            int g = argb >> 8 & 0xFF;
            int b = argb & 0xFF;

            return (a, r, g, b);
        }

        //If method hasn't returned a color yet, then there's a problem
        throw new ArgumentException("Invalid Hex value. Hex must be either an ARGB (8 digits) or RGB (6 digits)");
    }

    public static (int A, int R, int G, int B) ColorFromHexString(string hex)
    {
        string cleanHex = hex.Replace("0x", "").TrimStart('#');

        if (cleanHex.Length == 6)
            cleanHex = "FF" + cleanHex;

        if (
            Int32
            .TryParse(cleanHex,
                NumberStyles.HexNumber,
                CultureInfo.InvariantCulture,
                out var argb)
        )
        {
            int a = argb >> 24 & 0xFF;
            int r = argb >> 16 & 0xFF;
            int g = argb >> 8 & 0xFF;
            int b = argb & 0xFF;

            return (a, r, g, b);
        }

        //If method hasn't returned a color yet, then there's a problem
        throw new ArgumentException("Invalid Hex value. Hex must be either an ARGB (8 digits) or RGB (6 digits)");
    }
}
