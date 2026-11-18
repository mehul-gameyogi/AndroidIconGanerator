using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AndroidIconGenerator
{
    /// <summary>Safe texture loading, conversion and PNG helpers.</summary>
    public static class TextureUtility
    {
        private static readonly string[] DirectLoadExtensions = { ".png", ".jpg", ".jpeg" };
        private static readonly byte[] PngSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        public static bool IsSupportedExtension(string path)
        {
            string ext = Path.GetExtension(path ?? string.Empty).ToLowerInvariant();
            return Array.IndexOf(DirectLoadExtensions, ext) >= 0;
        }

        public static string FormatName(string path)
        {
            switch (Path.GetExtension(path ?? string.Empty).ToLowerInvariant())
            {
                case ".png": return "PNG";
                case ".jpg":
                case ".jpeg": return "JPEG";
                case "": return "Unknown";
                default: return Path.GetExtension(path).TrimStart('.').ToUpperInvariant();
            }
        }

        /// <summary>
        /// Loads the original pixels of an imported texture asset. PNG/JPG files are decoded straight from disk so
        /// import settings (max size, compression, non-readable) never degrade the source.
        /// </summary>
        public static ImageBuffer LoadAsset(Texture2D asset, out string error)
        {
            error = null;
            if (asset == null) { error = "No texture assigned."; return null; }

            string assetPath = AssetDatabase.GetAssetPath(asset);
            if (!string.IsNullOrEmpty(assetPath) && IsSupportedExtension(assetPath))
            {
                string full = Path.GetFullPath(assetPath);
                if (File.Exists(full))
                    return LoadFromFile(full, out error);
            }
            return ReadFromTexture(asset, out error);
        }

        public static ImageBuffer LoadFromFile(string absolutePath, out string error)
        {
            error = null;
            Texture2D tmp = null;
            try
            {
                byte[] bytes = File.ReadAllBytes(absolutePath);
                tmp = new Texture2D(2, 2, TextureFormat.RGBA32, false, false);
                if (!ImageConversion.LoadImage(tmp, bytes, false))
                {
                    error = "The file could not be decoded as an image: " + absolutePath;
                    return null;
                }
                return new ImageBuffer(tmp.width, tmp.height, tmp.GetPixels32());
            }
            catch (Exception e)
            {
                error = "Failed to read image: " + e.Message;
                return null;
            }
            finally
            {
                SafeDestroy(tmp);
            }
        }

        /// <summary>Copies any texture (including unreadable / compressed ones) through a temporary RenderTexture.</summary>
        public static ImageBuffer ReadFromTexture(Texture2D source, out string error)
        {
            error = null;
            RenderTexture rt = null;
            Texture2D readable = null;
            RenderTexture previous = RenderTexture.active;
            try
            {
                rt = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                Graphics.Blit(source, rt);
                RenderTexture.active = rt;
                readable = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false, false);
                readable.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
                readable.Apply(false);
                return new ImageBuffer(source.width, source.height, readable.GetPixels32());
            }
            catch (Exception e)
            {
                error = "Failed to read texture: " + e.Message;
                return null;
            }
            finally
            {
                RenderTexture.active = previous;
                if (rt != null) RenderTexture.ReleaseTemporary(rt);
                SafeDestroy(readable);
            }
        }

        /// <summary>Creates or updates a display texture from a buffer (reuses the texture when the size matches).</summary>
        public static void UpdateTexture(ref Texture2D texture, ImageBuffer buffer, FilterMode filter = FilterMode.Bilinear)
        {
            if (buffer == null) { SafeDestroy(texture); texture = null; return; }
            if (texture == null || texture.width != buffer.Width || texture.height != buffer.Height)
            {
                SafeDestroy(texture);
                texture = new Texture2D(buffer.Width, buffer.Height, TextureFormat.RGBA32, false, false)
                {
                    hideFlags = HideFlags.HideAndDontSave,
                    wrapMode = TextureWrapMode.Clamp,
                };
            }
            texture.filterMode = filter;
            texture.SetPixels32(buffer.Pixels);
            texture.Apply(false);
        }

        public static byte[] EncodePng(ImageBuffer buffer)
        {
            Texture2D tmp = new Texture2D(buffer.Width, buffer.Height, TextureFormat.RGBA32, false, false);
            try
            {
                tmp.SetPixels32(buffer.Pixels);
                tmp.Apply(false);
                return ImageConversion.EncodeToPNG(tmp);
            }
            finally
            {
                SafeDestroy(tmp);
            }
        }

        /// <summary>Reads width/height from a PNG header. Returns false when the data is not a PNG.</summary>
        public static bool TryReadPngSize(byte[] bytes, out int width, out int height)
        {
            width = height = 0;
            if (bytes == null || bytes.Length < 24) return false;
            for (int i = 0; i < PngSignature.Length; i++)
                if (bytes[i] != PngSignature[i]) return false;
            width = (bytes[16] << 24) | (bytes[17] << 16) | (bytes[18] << 8) | bytes[19];
            height = (bytes[20] << 24) | (bytes[21] << 16) | (bytes[22] << 8) | bytes[23];
            return width > 0 && height > 0;
        }

        public static void SafeDestroy(UnityEngine.Object obj)
        {
            if (obj != null) UnityEngine.Object.DestroyImmediate(obj);
        }
    }
}
