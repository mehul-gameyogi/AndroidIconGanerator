using System.IO;
using System.Linq;
using UnityEditor;

namespace AndroidIconGenerator
{
    /// <summary>Validates settings before generation and verifies the exported PNG files afterwards.</summary>
    public static class IconValidator
    {
        public const int MinimumSourceSize = 48;
        public const int RecommendedSourceSize = 432;

        public static ValidationReport ValidateSettings(AndroidIconSettings s, IconSourceSet set)
        {
            var report = new ValidationReport();

            // --- source -----------------------------------------------------------------------------------
            if (s.sourceImage == null && s.foregroundImage == null)
            {
                report.Error("Source image", "No icon or foreground image is assigned.",
                    "Import or pick an Icon image and/or a Foreground image.");
            }
            else if (s.sourceImage != null)
            {
                string path = AssetDatabase.GetAssetPath(s.sourceImage);
                if (!TextureUtility.IsSupportedExtension(path))
                    report.Warning("Source image", $"'{Path.GetExtension(path)}' is not PNG/JPG. It will be read through a render texture.",
                        "Convert the image to PNG for best quality.");

                if (set == null || set.Source == null)
                {
                    report.Error("Source image", "The source image could not be loaded.", "Re-import the image and try again.");
                }
                else
                {
                    int w = set.Source.Width, h = set.Source.Height;
                    if (w < MinimumSourceSize || h < MinimumSourceSize)
                        report.Error("Source image", $"Image is only {w}x{h}px. At least {MinimumSourceSize}x{MinimumSourceSize}px is required.",
                            "Use a larger source image, ideally 1024x1024 or more.");
                    else if (w < RecommendedSourceSize || h < RecommendedSourceSize)
                        report.Warning("Source image", $"Image is {w}x{h}px. xxxhdpi adaptive icons are {RecommendedSourceSize}px, so they will look soft.",
                            $"Use a source of at least {RecommendedSourceSize}x{RecommendedSourceSize}px.");
                    else
                        report.Success("Source image", $"{w}x{h}px {TextureUtility.FormatName(path)} loaded.");

                    if (w > 0 && h > 0 && System.Math.Abs(w - h) > 1)
                        report.Warning("Source image", "The image is not square. It is fitted with its aspect ratio preserved.",
                            "Use a square image, or set the crop mode to Fill to crop it.");
                }
            }

            // --- adaptive -----------------------------------------------------------------------------------
            ImagePyramid fg = set?.ForegroundOrSource;
            if (s.adaptiveForegroundEnabled)
            {
                if (fg == null)
                {
                    report.Error("Adaptive foreground", "No foreground image available.", "Assign a source image or a dedicated foreground image.");
                }
                else
                {
                    if (!fg.Level0.HasTransparency())
                        report.Warning("Adaptive foreground", "The foreground image is fully opaque and will completely cover the adaptive background.",
                            "Use a PNG with a transparent background, or accept a flat look.");
                    int size = IconSpecs.AdaptiveSize(AndroidDensity.Xhdpi);
                    ImageBuffer render = AdaptiveIconGenerator.RenderForeground(s, set, size);
                    IconSafeZoneValidator.Validate(render, report);
                }
            }

            if (s.adaptiveBackgroundEnabled && s.backgroundMode == BackgroundMode.Image)
            {
                if (s.backgroundImage == null)
                    report.Error("Adaptive background", "Background mode is Image but no background image is assigned.",
                        "Assign a background image or switch the mode to Solid / Gradient.");
                else if (set == null || set.BackgroundImage == null)
                    report.Error("Adaptive background", "The background image could not be loaded.", "Re-import the background image.");
            }
            if (s.adaptiveBackgroundEnabled != s.adaptiveForegroundEnabled)
                report.Warning("Adaptive", "Only one adaptive layer is enabled. Android needs both foreground and background.",
                    "Enable both adaptive layers, or assign the missing layer manually.");

            // --- output ---------------------------------------------------------------------------------------
            ValidateOutputDirectory(s, report);
            return report;
        }

        private static void ValidateOutputDirectory(AndroidIconSettings s, ValidationReport report)
        {
            string dir = IconFolderManager.NormalizeOutputDirectory(s.outputDirectory);
            if (string.IsNullOrWhiteSpace(s.outputDirectory))
            {
                report.Error("Output", "Output directory is empty.", "Choose an output folder, e.g. Assets/GeneratedAndroidIcons.");
                return;
            }
            if (dir.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
            {
                report.Error("Output", "Output directory contains invalid characters.", "Pick the folder with the Browse button.");
                return;
            }
            string abs = IconFolderManager.ToAbsolute(dir);
            if (Directory.Exists(abs)) report.Success("Output", "Output directory exists: " + dir);
            else
            {
                string parent = Path.GetDirectoryName(abs);
                if (!string.IsNullOrEmpty(parent) && Directory.Exists(parent))
                    report.Success("Output", "Output directory will be created: " + dir);
                else
                    report.Warning("Output", "Output directory does not exist yet; missing folders will be created: " + dir);
            }
            if (!IconFolderManager.IsInsideProjectAssets(dir))
                report.Warning("Output", "The output folder is outside Assets/. Icons can be exported but not assigned to Player Settings.",
                    "Use a folder inside Assets/ to enable automatic assignment.");
        }

        /// <summary>Verifies each exported file exists, is a PNG and has the expected resolution.</summary>
        public static ValidationReport ValidateExported(AndroidIconSettings s, ExportResult result)
        {
            var report = new ValidationReport();
            int ok = 0;
            foreach (GeneratedIconFile file in result.Files)
            {
                if (file.Status == ExportStatus.Failed)
                {
                    report.Error("Export", $"{Path.GetFileName(file.Path)} failed: {file.Error}",
                        "Make sure the output folder is writable and the file is not locked by another program.");
                    continue;
                }
                if (file.Status != ExportStatus.Written && file.Status != ExportStatus.Skipped) continue;

                string abs = IconFolderManager.ToAbsolute(file.Path);
                if (!File.Exists(abs))
                {
                    report.Error("Export", "Missing file: " + file.Path, "Run Generate again.");
                    continue;
                }
                byte[] bytes;
                try { bytes = File.ReadAllBytes(abs); }
                catch (System.Exception e) { report.Error("Export", $"Cannot read {file.Path}: {e.Message}"); continue; }

                if (!TextureUtility.TryReadPngSize(bytes, out int w, out int h))
                    report.Error("Export", $"{Path.GetFileName(file.Path)} is not a valid PNG.", "Delete the file and generate again.");
                else if (w != file.Size || h != file.Size)
                    report.Error("Export", $"{Path.GetFileName(file.Path)} is {w}x{h}px but {file.Size}x{file.Size}px was expected.",
                        "Overwrite the file by generating again.");
                else ok++;
            }

            // required resolutions
            foreach (IconType type in AndroidIconExporter.AllTypes())
            {
                int expected = result.Files.Count(f => f.Type == type);
                if (expected == 0) continue;
                int valid = result.Files.Count(f => f.Type == type && (f.Status == ExportStatus.Written || f.Status == ExportStatus.Skipped) && File.Exists(IconFolderManager.ToAbsolute(f.Path)));
                if (valid == expected) report.Success("Resolutions", $"{IconSpecs.DisplayName(type)}: all {expected} required resolutions present.");
                else report.Warning("Resolutions", $"{IconSpecs.DisplayName(type)}: {valid} of {expected} resolutions are present.", "Generate again to create the missing files.");
            }

            if (ok > 0 && !report.HasErrors) report.Success("Export", $"{ok} PNG files verified.");
            return report;
        }
    }
}
