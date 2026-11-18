using System;
using System.IO;
using UnityEngine;

namespace AndroidIconGenerator
{
    /// <summary>Output directory layout, naming and project-relative path handling.</summary>
    public static class IconFolderManager
    {
        public static string ProjectRoot => Path.GetDirectoryName(Application.dataPath).Replace('\\', '/');

        public static string NormalizeOutputDirectory(string dir)
        {
            if (string.IsNullOrWhiteSpace(dir)) return AndroidIconSettings.DefaultOutputDirectory;
            dir = dir.Trim().Replace('\\', '/').TrimEnd('/');
            string root = ProjectRoot + "/";
            if (Path.IsPathRooted(dir) && dir.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                dir = dir.Substring(root.Length);
            return dir;
        }

        public static bool IsInsideProjectAssets(string dir)
        {
            dir = NormalizeOutputDirectory(dir);
            return dir == "Assets" || dir.StartsWith("Assets/", StringComparison.Ordinal);
        }

        public static string ToAbsolute(string pathOrAssetPath)
        {
            string p = pathOrAssetPath.Replace('\\', '/');
            if (Path.IsPathRooted(p)) return p;
            return ProjectRoot + "/" + p;
        }

        /// <summary>Sub folder (relative to the output directory) holding a given icon.</summary>
        public static string SubFolder(IconType type, AndroidDensity density)
        {
            switch (type)
            {
                case IconType.Legacy: return "Legacy/" + IconSpecs.FolderName(density);
                case IconType.Round: return "Round/" + IconSpecs.FolderName(density);
                case IconType.AdaptiveForeground: return "Adaptive/Foreground";
                default: return "Adaptive/Background";
            }
        }

        public static string FileName(IconType type, AndroidDensity density)
        {
            int size = IconSpecs.GetSize(type, density);
            string d = IconSpecs.FolderName(density);
            switch (type)
            {
                case IconType.Legacy: return $"ic_launcher_legacy_{d}_{size}x{size}.png";
                case IconType.Round: return $"ic_launcher_round_{d}_{size}x{size}.png";
                case IconType.AdaptiveForeground: return $"ic_launcher_foreground_{d}_{size}x{size}.png";
                default: return $"ic_launcher_background_{d}_{size}x{size}.png";
            }
        }

        /// <summary>Project-relative (or absolute, when outside the project) path of a generated file.</summary>
        public static string GetFilePath(string outputDirectory, IconType type, AndroidDensity density)
        {
            return NormalizeOutputDirectory(outputDirectory) + "/" + SubFolder(type, density) + "/" + FileName(type, density);
        }

        public static void EnsureDirectoryFor(string filePath)
        {
            string dir = Path.GetDirectoryName(ToAbsolute(filePath));
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
        }
    }
}
