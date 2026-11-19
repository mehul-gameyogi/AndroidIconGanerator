using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace AndroidIconGenerator
{
    public enum IconCategory { Legacy, Round, Adaptive }

    /// <summary>
    /// Reads and writes the Android icon slots in Player Settings using the PlatformIcon API
    /// (PlayerSettings.GetPlatformIcons / SetPlatformIcons). The Android specific icon kinds live in the Android
    /// build support module, so they are resolved at runtime and the tool degrades gracefully when the module is missing.
    /// </summary>
    public static class AndroidPlayerSettingsManager
    {
        // Adaptive icon layer order in Unity's Android player settings.
        public const int AdaptiveBackgroundLayer = 0;
        public const int AdaptiveForegroundLayer = 1;

        private static NamedBuildTarget Target => NamedBuildTarget.Android;

        public static bool IsAndroidSupportInstalled()
        {
            try { return BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android); }
            catch { return false; }
        }

        public static string UnsupportedReason()
        {
            return IsAndroidSupportInstalled() ? null :
                "Android Build Support is not installed for this Unity version. Install it through Unity Hub > Installs > Add Modules.";
        }

        public static IconType PrimaryType(IconCategory category)
        {
            switch (category)
            {
                case IconCategory.Legacy: return IconType.Legacy;
                case IconCategory.Round: return IconType.Round;
                default: return IconType.AdaptiveForeground;
            }
        }

        public static bool TryGetKind(IconCategory category, out PlatformIconKind kind, out string error)
        {
            kind = null; error = null;
            string name = category.ToString();
            try
            {
                // 1) static members on UnityEditor.Android.AndroidPlatformIconKind (Android module).
                foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    Type t = asm.GetType("UnityEditor.Android.AndroidPlatformIconKind", false);
                    if (t == null) continue;
                    object value = t.GetProperty(name, BindingFlags.Public | BindingFlags.Static)?.GetValue(null)
                                   ?? t.GetField(name, BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                    if (value is PlatformIconKind k) { kind = k; return true; }
                }

                // 2) fall back to whatever kinds Unity reports for Android.
                PlatformIconKind[] supported = PlayerSettings.GetSupportedIconKinds(Target);
                if (supported != null)
                {
                    foreach (PlatformIconKind k in supported)
                    {
                        if (k != null && k.ToString().IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            kind = k; return true;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                error = "Unity's Android icon API is not available in this editor version: " + e.Message;
                return false;
            }
            error = $"This Unity version does not expose the Android '{name}' icon kind.";
            return false;
        }

        public static bool TryReadIcons(IconCategory category, out PlatformIconKind kind, out PlatformIcon[] icons, out string error)
        {
            icons = null;
            if (!TryGetKind(category, out kind, out error)) return false;
            try
            {
                icons = PlayerSettings.GetPlatformIcons(Target, kind);
            }
            catch (Exception e)
            {
                error = "Could not read Android icon slots: " + e.Message;
                return false;
            }
            if (icons == null || icons.Length == 0)
            {
                error = $"Unity reported no {category} icon slots for Android.";
                return false;
            }
            return true;
        }

        public static AndroidDensity DensityForSlot(IconCategory category, int width)
        {
            IconType type = PrimaryType(category);
            AndroidDensity best = AndroidDensity.Mdpi;
            int bestDiff = int.MaxValue;
            foreach (AndroidDensity d in IconSpecs.Densities(true))
            {
                int diff = Mathf.Abs(IconSpecs.GetSize(type, d) - width);
                if (diff < bestDiff) { bestDiff = diff; best = d; }
            }
            return best;
        }

        private static Texture2D LoadAsset(string assetPath)
        {
            if (!IconFolderManager.IsInsideProjectAssets(assetPath)) return null;
            return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        }

        /// <summary>
        /// Assigns the generated icon files to Android Player Settings. Each category is verified after assignment.
        /// The caller is responsible for asking the user for confirmation before calling this.
        /// </summary>
        public static ValidationReport Assign(AndroidIconSettings s, bool legacy, bool round, bool adaptive)
        {
            var report = new ValidationReport();
            const string cat = "Player Settings";

            string unsupported = UnsupportedReason();
            if (unsupported != null) { report.Error(cat, unsupported, "Install the Android module, then try again."); return report; }
            if (!IconFolderManager.IsInsideProjectAssets(s.outputDirectory))
            {
                report.Error(cat, "The output folder is outside Assets/, so the PNGs are not Unity assets.",
                    "Set the output directory to a folder inside Assets/ and generate the icons again.");
                return report;
            }

            if (legacy) AssignCategory(s, IconCategory.Legacy, report);
            if (round) AssignCategory(s, IconCategory.Round, report);
            if (adaptive) AssignCategory(s, IconCategory.Adaptive, report);

            if (!report.HasErrors) AssetDatabase.SaveAssets();
            return report;
        }

        private static void AssignCategory(AndroidIconSettings s, IconCategory category, ValidationReport report)
        {
            string cat = "Player Settings / " + category;
            if (!TryReadIcons(category, out PlatformIconKind kind, out PlatformIcon[] icons, out string error))
            {
                report.Error(cat, error, "Update Unity or assign the icons manually in Project Settings > Player > Icon.");
                return;
            }

            bool adaptive = category == IconCategory.Adaptive;
            var expected = new List<Texture2D[]>();     // per slot: textures per layer (null entries = untouched)
            int assigned = 0, missing = 0;

            try
            {
                foreach (PlatformIcon icon in icons)
                {
                    AndroidDensity d = DensityForSlot(category, icon.width);
                    Texture2D[] layers;

                    if (adaptive)
                    {
                        Texture2D bg = LoadAsset(IconFolderManager.GetFilePath(s.outputDirectory, IconType.AdaptiveBackground, d));
                        Texture2D fg = LoadAsset(IconFolderManager.GetFilePath(s.outputDirectory, IconType.AdaptiveForeground, d));
                        // layerCount is the number of layers currently filled; maxLayerCount is what the slot supports.
                        if (bg == null || fg == null || icon.maxLayerCount < 2) { missing++; expected.Add(null); continue; }
                        icon.SetTexture(bg, AdaptiveBackgroundLayer);
                        icon.SetTexture(fg, AdaptiveForegroundLayer);
                        layers = new[] { bg, fg };
                    }
                    else
                    {
                        Texture2D tex = LoadAsset(IconFolderManager.GetFilePath(s.outputDirectory, PrimaryType(category), d));
                        if (tex == null) { missing++; expected.Add(null); continue; }
                        icon.SetTexture(tex, 0);
                        layers = new[] { tex };
                    }
                    expected.Add(layers);
                    assigned++;
                }

                if (assigned == 0)
                {
                    report.Error(cat, "No generated icon files were found for this category.",
                        "Click 'Generate' first and make sure the output directory matches.");
                    return;
                }
                PlayerSettings.SetPlatformIcons(Target, kind, icons);
            }
            catch (Exception e)
            {
                report.Error(cat, "Assignment failed: " + e.Message, "Assign the icons manually in Project Settings > Player > Icon.");
                return;
            }

            // Verify by reading everything back.
            int verified = 0;
            PlatformIcon[] after = PlayerSettings.GetPlatformIcons(Target, kind);
            for (int i = 0; i < expected.Count && after != null && i < after.Length; i++)
            {
                Texture2D[] layers = expected[i];
                if (layers == null) continue;
                bool match = true;
                for (int l = 0; l < layers.Length; l++)
                    if (after[i].GetTexture(l) != layers[l]) { match = false; break; }
                if (match) verified++;
            }

            if (verified != assigned)
                report.Error(cat, $"Verification failed: only {verified} of {assigned} icon slots hold the generated textures.",
                    "Open Project Settings > Player > Android > Icon and assign them manually.");
            else if (missing > 0)
                report.Warning(cat, $"Assigned the best-fit icon to {assigned} slot(s); {missing} smaller slots were left unchanged.",
                    "The generator intentionally creates only the largest best-fit icon for each type.");
            else
                report.Success(cat, $"Assigned and verified {assigned} icon slots.");
        }
    }
}
