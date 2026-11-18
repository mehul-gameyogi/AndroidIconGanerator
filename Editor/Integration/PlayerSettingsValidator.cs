using System;
using UnityEditor;
using UnityEngine;

namespace AndroidIconGenerator
{
    /// <summary>Compares the current Android Player Settings icon slots with the generated files.</summary>
    public static class PlayerSettingsValidator
    {
        public static ValidationReport Validate(AndroidIconSettings s)
        {
            var report = new ValidationReport();
            string unsupported = AndroidPlayerSettingsManager.UnsupportedReason();
            if (unsupported != null)
            {
                report.Error("Player Settings", unsupported, "Install the Android module through Unity Hub.");
                return report;
            }

            foreach (IconCategory category in Enum.GetValues(typeof(IconCategory)))
                ValidateCategory(s, category, report);
            return report;
        }

        private static void ValidateCategory(AndroidIconSettings s, IconCategory category, ValidationReport report)
        {
            string cat = "Player Settings / " + category;
            if (!AndroidPlayerSettingsManager.TryReadIcons(category, out _, out PlatformIcon[] icons, out string error))
            {
                report.Warning(cat, error, "Assign this icon kind manually in Project Settings > Player > Icon.");
                return;
            }

            bool adaptive = category == IconCategory.Adaptive;
            int slots = icons.Length, filled = 0, generated = 0;

            foreach (PlatformIcon icon in icons)
            {
                AndroidDensity d = AndroidPlayerSettingsManager.DensityForSlot(category, icon.width);
                bool slotFilled;
                Texture2D[] expected;

                if (adaptive)
                {
                    expected = new[]
                    {
                        Load(s, IconType.AdaptiveBackground, d),
                        Load(s, IconType.AdaptiveForeground, d),
                    };
                    slotFilled = icon.layerCount >= 2 && icon.GetTexture(0) != null && icon.GetTexture(1) != null;
                    if (slotFilled && expected[0] != null && expected[1] != null &&
                        icon.GetTexture(0) == expected[0] && icon.GetTexture(1) == expected[1]) generated++;
                }
                else
                {
                    expected = new[] { Load(s, AndroidPlayerSettingsManager.PrimaryType(category), d) };
                    slotFilled = icon.GetTexture(0) != null;
                    if (slotFilled && expected[0] != null && icon.GetTexture(0) == expected[0]) generated++;
                }
                if (slotFilled) filled++;
            }

            if (filled == 0)
                report.Warning(cat, "No icons are assigned.", "Use 'Assign To Player Settings' after generating the icons.");
            else if (generated == slots)
                report.Success(cat, $"All {slots} slots use the generated icons.");
            else if (generated > 0)
                report.Warning(cat, $"{generated} of {slots} slots use the generated icons ({filled} slots are filled).",
                    "Assign again to update the remaining slots.");
            else
                report.Warning(cat, $"{filled} of {slots} slots are filled, but none use the generated icons.",
                    "Assign again if you want the generated icons to be used.");
        }

        private static Texture2D Load(AndroidIconSettings s, IconType type, AndroidDensity d)
        {
            string path = IconFolderManager.GetFilePath(s.outputDirectory, type, d);
            return IconFolderManager.IsInsideProjectAssets(path) ? AssetDatabase.LoadAssetAtPath<Texture2D>(path) : null;
        }
    }
}
