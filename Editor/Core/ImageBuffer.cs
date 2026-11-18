using UnityEngine;

namespace AndroidIconGenerator
{
    /// <summary>
    /// CPU-side RGBA image (straight alpha, origin bottom-left like Texture2D).
    /// All icon processing runs on these buffers so the imported source asset is never touched.
    /// </summary>
    public sealed class ImageBuffer
    {
        public readonly int Width;
        public readonly int Height;
        public readonly Color32[] Pixels;

        public ImageBuffer(int width, int height)
        {
            Width = Mathf.Max(1, width);
            Height = Mathf.Max(1, height);
            Pixels = new Color32[Width * Height];
        }

        public ImageBuffer(int width, int height, Color32[] pixels)
        {
            Width = width; Height = height; Pixels = pixels;
        }

        public bool HasTransparency(byte threshold = 250)
        {
            for (int i = 0; i < Pixels.Length; i++)
                if (Pixels[i].a < threshold) return true;
            return false;
        }
    }
}
