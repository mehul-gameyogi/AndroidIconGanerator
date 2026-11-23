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

        /// <summary>Returns a copy cropped to the non-transparent bounds, or this buffer if there is nothing to trim.</summary>
        public ImageBuffer TrimTransparent(byte threshold = 8)
        {
            int minX = Width, minY = Height, maxX = -1, maxY = -1;
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                {
                    if (Pixels[y * Width + x].a <= threshold) continue;
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            if (maxX < 0) return this;                                            // fully transparent
            if (minX == 0 && minY == 0 && maxX == Width - 1 && maxY == Height - 1) return this;

            int w = maxX - minX + 1, h = maxY - minY + 1;
            var trimmed = new ImageBuffer(w, h);
            for (int y = 0; y < h; y++)
                System.Array.Copy(Pixels, (minY + y) * Width + minX, trimmed.Pixels, y * w, w);
            return trimmed;
        }

        public bool HasTransparency(byte threshold = 250)
        {
            for (int i = 0; i < Pixels.Length; i++)
                if (Pixels[i].a < threshold) return true;
            return false;
        }
    }
}
