using System.Collections.Generic;
using UnityEngine;

namespace AndroidIconGenerator
{
    public enum IconType { Legacy, Round, AdaptiveForeground, AdaptiveBackground }

    public enum AndroidDensity { Ldpi, Mdpi, Hdpi, Xhdpi, Xxhdpi, Xxxhdpi }

    /// <summary>Android icon specification constants and density tables.</summary>
    public static class IconSpecs
    {
        /// <summary>Adaptive icon layers are 108x108dp.</summary>
        public const float AdaptiveCanvasDp = 108f;
        /// <summary>The launcher mask reveals the centre 72x72dp.</summary>
        public const float AdaptiveVisibleDp = 72f;
        /// <summary>Content that must never be clipped: circle of 66dp diameter.</summary>
        public const float AdaptiveSafeDp = 66f;
        /// <summary>Legacy launcher icons are 48x48dp.</summary>
        public const float LauncherDp = 48f;
        /// <summary>Single best-fit export used for legacy and round Player Settings slots.</summary>
        public const int BestFitLauncherSize = 192;
        /// <summary>Single best-fit export used for adaptive foreground and background layers.</summary>
        public const int BestFitAdaptiveSize = 432;
        /// <summary>The best-fit files use Android's xxxhdpi dimensions.</summary>
        public const AndroidDensity BestFitDensity = AndroidDensity.Xxxhdpi;

        public static float Scale(AndroidDensity d)
        {
            switch (d)
            {
                case AndroidDensity.Ldpi: return 0.75f;
                case AndroidDensity.Mdpi: return 1f;
                case AndroidDensity.Hdpi: return 1.5f;
                case AndroidDensity.Xhdpi: return 2f;
                case AndroidDensity.Xxhdpi: return 3f;
                default: return 4f;
            }
        }

        public static string FolderName(AndroidDensity d) { return d.ToString().ToLowerInvariant(); }

        public static int LauncherSize(AndroidDensity d) { return Mathf.RoundToInt(LauncherDp * Scale(d)); }
        public static int AdaptiveSize(AndroidDensity d) { return Mathf.RoundToInt(AdaptiveCanvasDp * Scale(d)); }

        public static int GetSize(IconType type, AndroidDensity d)
        {
            return (type == IconType.AdaptiveForeground || type == IconType.AdaptiveBackground)
                ? AdaptiveSize(d) : LauncherSize(d);
        }

        public static int BestFitSize(IconType type)
        {
            return IsAdaptive(type) ? BestFitAdaptiveSize : BestFitLauncherSize;
        }

        public static bool IsAdaptive(IconType type)
        {
            return type == IconType.AdaptiveForeground || type == IconType.AdaptiveBackground;
        }

        public static IEnumerable<AndroidDensity> Densities(bool includeLdpi)
        {
            if (includeLdpi) yield return AndroidDensity.Ldpi;
            yield return AndroidDensity.Mdpi;
            yield return AndroidDensity.Hdpi;
            yield return AndroidDensity.Xhdpi;
            yield return AndroidDensity.Xxhdpi;
            yield return AndroidDensity.Xxxhdpi;
        }

        public static string DisplayName(IconType type)
        {
            switch (type)
            {
                case IconType.Legacy: return "Legacy";
                case IconType.Round: return "Round";
                case IconType.AdaptiveForeground: return "Adaptive Foreground";
                default: return "Adaptive Background";
            }
        }
    }
}
