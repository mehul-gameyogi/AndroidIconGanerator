using System.IO;
using UnityEditor;
using UnityEngine;

namespace AndroidIconGenerator
{
    /// <summary>The decoded source images (as mip pyramids) shared by previews, export and validation.</summary>
    public sealed class IconSourceSet
    {
        public ImagePyramid Source;
        public ImagePyramid Foreground;
        public ImagePyramid BackgroundImage;
        public string Key = string.Empty;

        public ImagePyramid ForegroundOrSource => Foreground ?? Source;        public bool HasForegroundOverride => Foreground != null;

        /// <summary>Cheap identity of the assigned images (paths + content hashes) to detect when a reload is needed.</summary>
        public static string ComputeKey(AndroidIconSettings s)
        {
            return KeyOf(s.sourceImage) + "|" + KeyOf(s.foregroundImage) + "|" + KeyOf(s.backgroundImage)
                + "|" + (s.foregroundTrim ? "trim" : "full");
        }

        private static string KeyOf(Texture2D t)
        {
            if (t == null) return "-";
            string path = AssetDatabase.GetAssetPath(t);
            if (string.IsNullOrEmpty(path)) return "mem" + t.GetInstanceID();
            return path + ":" + AssetDatabase.GetAssetDependencyHash(path);
        }

        public static IconSourceSet Load(AndroidIconSettings s, ValidationReport report = null)
        {
            var set = new IconSourceSet { Key = ComputeKey(s) };
            set.Source = LoadPyramid(s.sourceImage, "Source image", report);
            set.Foreground = LoadPyramid(s.foregroundImage, "Foreground image", report);
            set.BackgroundImage = LoadPyramid(s.backgroundImage, "Background image", report);

            // Trim applies to the adaptive foreground layer only (legacy/round keep the full image).
            if (s.foregroundTrim)
            {
                ImagePyramid layer = set.ForegroundOrSource;
                if (layer != null)
                {
                    ImageBuffer trimmed = layer.Level0.TrimTransparent();
                    if (trimmed != layer.Level0) set.Foreground = ImagePyramid.Build(trimmed);
                }
            }
            return set;
        }

        private static ImagePyramid LoadPyramid(Texture2D texture, string label, ValidationReport report)
        {
            if (texture == null) return null;
            ImageBuffer buffer = TextureUtility.LoadAsset(texture, out string error);
            if (buffer == null)
            {
                report?.Error(label, error ?? "Could not load image.",
                    "Re-import the file, or use a PNG/JPG without corruption.");
                return null;
            }
            return ImagePyramid.Build(buffer);
        }
    }
}
