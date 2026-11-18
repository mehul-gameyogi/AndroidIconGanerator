using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AndroidIconGenerator
{
    /// <summary>Writes PNG files and configures their Unity import settings.</summary>
    public static class IconFileWriter
    {
        /// <summary>Encodes and writes a PNG. Returns false and an error message on failure.</summary>
        public static bool WritePng(ImageBuffer buffer, string path, out string error)
        {
            error = null;
            try
            {
                IconFolderManager.EnsureDirectoryFor(path);
                byte[] png = TextureUtility.EncodePng(buffer);
                if (png == null || png.Length == 0) { error = "PNG encoding produced no data."; return false; }

                string full = IconFolderManager.ToAbsolute(path);
                string temp = full + ".tmp";
                File.WriteAllBytes(temp, png);
                if (File.Exists(full)) File.Delete(full);
                File.Move(temp, full);
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                error = "Access denied. Check that the folder is writable and the file is not read-only or open elsewhere.";
                return false;
            }
            catch (Exception e)
            {
                error = e.Message;
                return false;
            }
        }

        /// <summary>
        /// Applies icon-friendly import settings (no compression / mip maps, transparency preserved)
        /// to a batch of PNG assets using a single asset-editing block.
        /// </summary>
        public static void ConfigureImportSettings(System.Collections.Generic.IEnumerable<string> assetPaths)
        {
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (string path in assetPaths)
                {
                    var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (importer == null) continue;

                    bool dirty = false;
                    if (importer.textureType != TextureImporterType.Default) { importer.textureType = TextureImporterType.Default; dirty = true; }
                    if (importer.mipmapEnabled) { importer.mipmapEnabled = false; dirty = true; }
                    if (!importer.alphaIsTransparency) { importer.alphaIsTransparency = true; dirty = true; }
                    if (importer.npotScale != TextureImporterNPOTScale.None) { importer.npotScale = TextureImporterNPOTScale.None; dirty = true; }
                    if (importer.textureCompression != TextureImporterCompression.Uncompressed) { importer.textureCompression = TextureImporterCompression.Uncompressed; dirty = true; }
                    if (importer.maxTextureSize < 512) { importer.maxTextureSize = 512; dirty = true; }
                    if (importer.isReadable) { importer.isReadable = false; dirty = true; }
                    if (dirty) importer.SaveAndReimport();
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
        }
    }
}
