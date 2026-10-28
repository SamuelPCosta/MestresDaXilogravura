using System;
using Xilo.Core;

namespace Xilo.Painting
{
    // Pure coverage math over RGBA32 pixel buffers (4 bytes per pixel).
    public static class CoverageCalculator
    {
        public const byte PaintedThreshold = 128;
        public const byte ArtDarkThreshold = 128;

        // Builds the target mask from the art pixels: which pixels must be painted for the step.
        public static bool[] BuildTarget(ReadOnlySpan<byte> artRgba, CoverageTarget target)
        {
            int pixelCount = artRgba.Length / 4;
            var result = new bool[pixelCount];
            for (int i = 0; i < pixelCount; i++)
            {
                result[i] = target switch
                {
                    CoverageTarget.ArtDark => Luminance(artRgba, i) < ArtDarkThreshold,
                    CoverageTarget.ArtLight => Luminance(artRgba, i) >= ArtDarkThreshold,
                    _ => true
                };
            }
            return result;
        }

        // Target of the pixels NOT painted in another mask (e.g. the matrix areas left uncarved).
        public static bool[] BuildUnpaintedTarget(ReadOnlySpan<byte> maskRgba)
        {
            int pixelCount = maskRgba.Length / 4;
            var result = new bool[pixelCount];
            for (int i = 0; i < pixelCount; i++)
                result[i] = maskRgba[i * 4] < PaintedThreshold;
            return result;
        }

        // Printable area: pixels inked by the roller on the relief (painted in the ink mask and not carved).
        public static bool[] BuildInkedTarget(ReadOnlySpan<byte> inkRgba, ReadOnlySpan<byte> carveRgba)
        {
            int pixelCount = Math.Min(inkRgba.Length, carveRgba.Length) / 4;
            var result = new bool[pixelCount];
            for (int i = 0; i < pixelCount; i++)
                result[i] = inkRgba[i * 4] >= PaintedThreshold && carveRgba[i * 4] < PaintedThreshold;
            return result;
        }

        public static bool[] BuildFullTarget(int pixelCount)
        {
            var result = new bool[pixelCount];
            Array.Fill(result, true);
            return result;
        }

        // Fraction (0..1) of target pixels whose mask red channel is painted.
        public static float Compute(ReadOnlySpan<byte> maskRgba, bool[] target)
        {
            if (target == null || target.Length == 0)
                return 0f;

            int pixelCount = Math.Min(maskRgba.Length / 4, target.Length);
            int targetCount = 0;
            int paintedCount = 0;
            for (int i = 0; i < pixelCount; i++)
            {
                if (!target[i])
                    continue;
                targetCount++;
                if (maskRgba[i * 4] >= PaintedThreshold)
                    paintedCount++;
            }

            return targetCount == 0 ? 1f : (float)paintedCount / targetCount;
        }

        static int Luminance(ReadOnlySpan<byte> rgba, int pixel)
        {
            int offset = pixel * 4;
            return (rgba[offset] + rgba[offset + 1] + rgba[offset + 2]) / 3;
        }
    }
}
