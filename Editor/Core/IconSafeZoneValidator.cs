using UnityEngine;

namespace AndroidIconGenerator
{
    /// <summary>Measures how much of the adaptive foreground lies outside the visible area and the 66dp safe zone.</summary>
    public static class IconSafeZoneValidator
    {
        public struct Result
        {
            public bool HasContent;
            public float OutsideSafeFraction;       // of visible content pixels
            public float OutsideVisibleFraction;    // content that every mask clips
        }

        private const byte AlphaThreshold = 26;     // ~10% alpha counts as content

        public static Result Analyze(ImageBuffer foreground)
        {
            var result = new Result();
            if (foreground == null) return result;

            int size = foreground.Width;
            float ppdp = size / IconSpecs.AdaptiveCanvasDp;
            float center = size * 0.5f;
            float safeR = IconSpecs.AdaptiveSafeDp * 0.5f * ppdp;
            float visibleHalf = IconSpecs.AdaptiveVisibleDp * 0.5f * ppdp;

            long content = 0, outsideSafe = 0, outsideVisible = 0;
            for (int y = 0; y < size; y++)
            {
                float dy = y + 0.5f - center;
                for (int x = 0; x < size; x++)
                {
                    if (foreground.Pixels[y * size + x].a < AlphaThreshold) continue;
                    content++;
                    float dx = x + 0.5f - center;
                    if (dx * dx + dy * dy > safeR * safeR) outsideSafe++;
                    if (Mathf.Abs(dx) > visibleHalf || Mathf.Abs(dy) > visibleHalf) outsideVisible++;
                }
            }

            result.HasContent = content > 0;
            if (content > 0)
            {
                result.OutsideSafeFraction = outsideSafe / (float)content;
                result.OutsideVisibleFraction = outsideVisible / (float)content;
            }
            return result;
        }

        public static void Validate(ImageBuffer foreground, ValidationReport report)
        {
            const string cat = "Safe zone";
            Result r = Analyze(foreground);
            if (!r.HasContent)
            {
                report.Error(cat, "The adaptive foreground is empty (no visible pixels).",
                    "Check the foreground image, or raise the scale / move it back onto the canvas.");
            }
            else if (r.OutsideVisibleFraction > 0.005f)
            {
                report.Warning(cat, $"{r.OutsideVisibleFraction:P0} of the foreground lies outside the visible 72dp area and will be cropped by every launcher.",
                    "Lower the foreground scale or reduce the horizontal/vertical offset.");
            }
            else if (r.OutsideSafeFraction > 0.02f)
            {
                report.Warning(cat, $"{r.OutsideSafeFraction:P0} of the foreground is outside the 66dp safe zone and may be clipped by circular masks.",
                    "Lower the foreground scale until the artwork sits inside the safe-zone circle.");
            }
            else
            {
                report.Success(cat, "Foreground is inside the 66dp safe zone.");
            }
        }
    }
}
