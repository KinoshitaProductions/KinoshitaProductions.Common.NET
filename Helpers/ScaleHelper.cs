namespace KinoshitaProductions.Common.Helpers;

public static class ScaleHelper
{
    public static double GetLinearScaledValueToTarget((double minValue, double currentValue, double maxValue) original, (double minValue, double maxValue) target)
    {
        var originalRange = original.maxValue - original.minValue;
        var targetRange = target.maxValue - target.minValue;

        var relativeValue = (original.currentValue - original.minValue) / Math.Max(1, originalRange);
        return Math.Clamp(relativeValue * targetRange + target.minValue, target.minValue, target.maxValue);
    }
    public static double GetUpscaleFactorToFitDimensions((double Width, double Height) original, (double Width, double Height) target)
    {
        double xRatio = target.Width / original.Width;
        double yRatio = target.Height / original.Height;

        double scaleToRatio = Math.Min(xRatio, yRatio);

        if (scaleToRatio < 1.0)
            scaleToRatio = 1.0;

        return scaleToRatio;
    }

    public static double GetUpscaleFactorToFillDimensions((double Width, double Height) original, (double Width, double Height) target)
    {
        double xRatio = target.Width / original.Width;
        double yRatio = target.Height / original.Height;

        double scaleToRatio = Math.Max(xRatio, yRatio);

        if (scaleToRatio < 1.0)
            scaleToRatio = 1.0;

        return scaleToRatio;
    }

    public static (double Width, double Height) ScaleToFitDimensions((double Width, double Height) original, (double Width, double Height) target)
    {
        double xRatio = target.Width / original.Width;
        double yRatio = target.Height / original.Height;

        double scaleToRatio = Math.Min(xRatio, yRatio);

        original.Width *= scaleToRatio;
        original.Height *= scaleToRatio;

        return original;
    }

    public static (double Width, double Height) DownscaleToFitDimensions((double Width, double Height) original, (double Width, double Height) target)
    {
        double xRatio = target.Width / original.Width;
        double yRatio = target.Height / original.Height;

        double scaleToRatio = Math.Min(xRatio, yRatio);

        if (scaleToRatio < 1.0)
        {
            original.Width *= scaleToRatio;
            original.Height *= scaleToRatio;
        }

        return original;
    }

    public static (double Width, double Height) ScaleToFitWidth((double Width, double Height) original, double targetWidth)
    {
        double xRatio = targetWidth / original.Width;

        double scaleToRatio = xRatio;

        original.Width = targetWidth;
        original.Height *= scaleToRatio;

        return original;
    }

    public static (double Width, double Height) DownscaleToFitWidth((double Width, double Height) original, double targetWidth)
    {
        double xRatio = targetWidth / original.Width;

        double scaleToRatio = xRatio;

        if (scaleToRatio < 1.0)
        {
            original.Width = targetWidth;
            original.Height *= scaleToRatio;
        }

        return original;
    }

    public static (double Width, double Height) ScaleToFitHeight((double Width, double Height) original, double targetHeight)
    {
        double yRatio = targetHeight / original.Height;

        double scaleToRatio = yRatio;

        original.Width *= scaleToRatio;
        original.Height = targetHeight;

        return original;
    }

    public static (double Width, double Height) ScaleToFillDimensions((double Width, double Height) original, (double Width, double Height) target)
    {
        double xRatio = target.Width / original.Width;
        double yRatio = target.Height / original.Height;

        double scaleToRatio = Math.Max(xRatio, yRatio);

        original.Width *= scaleToRatio;
        original.Height *= scaleToRatio;

        return original;
    }
}