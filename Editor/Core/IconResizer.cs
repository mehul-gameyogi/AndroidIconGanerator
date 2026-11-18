using UnityEngine;

namespace AndroidIconGenerator
{
    /// <summary>High quality image scaling built on <see cref="ImagePyramid"/>.</summary>
    public static class IconResizer
    {
        /// <summary>Picks the mip level whose texel density best matches the output pixel density.</summary>
        public static int ChooseLevel(ImagePyramid pyramid, float texelsPerPixel)
        {
            if (texelsPerPixel <= 1.4f) return 0;
            int level = Mathf.RoundToInt(Mathf.Log(texelsPerPixel, 2f));
            return Mathf.Clamp(level, 0, pyramid.LevelCount - 1);
        }

        /// <summary>
        /// Composites the image over a premultiplied canvas. <paramref name="clipHalfExtent"/> &gt; 0 additionally
        /// clips to a centred square box (used by the Fill crop mode).
        /// </summary>
        public static void DrawImage(Color[] canvas, int size, ImagePyramid src, IconCropper.ImageRect rect, float clipHalfExtent)
        {
            float ppu = size;
            float texelsPerPixel = src.Width / Mathf.Max(0.0001f, rect.w * size);
            int level = ChooseLevel(src, texelsPerPixel);

            int xMin = Mathf.Max(0, Mathf.FloorToInt((rect.cx - rect.w * 0.5f) * size) - 1);
            int xMax = Mathf.Min(size - 1, Mathf.CeilToInt((rect.cx + rect.w * 0.5f) * size) + 1);
            int yMin = Mathf.Max(0, Mathf.FloorToInt((rect.cy - rect.h * 0.5f) * size) - 1);
            int yMax = Mathf.Min(size - 1, Mathf.CeilToInt((rect.cy + rect.h * 0.5f) * size) + 1);

            for (int y = yMin; y <= yMax; y++)
            {
                float v = (y + 0.5f) / size;
                for (int x = xMin; x <= xMax; x++)
                {
                    float u = (x + 0.5f) / size;
                    float coverage = IconCropper.RectCoverage(u, v, rect, ppu);
                    if (clipHalfExtent > 0f) coverage *= IconCropper.ClipBoxCoverage(u, v, clipHalfExtent, ppu);
                    if (coverage <= 0f) continue;

                    float sx = (u - rect.cx) / rect.w + 0.5f;
                    float sy = (v - rect.cy) / rect.h + 0.5f;
                    Color s = src.SamplePremultiplied(level, sx, sy);
                    if (s.a <= 0f) continue;
                    s.r *= coverage; s.g *= coverage; s.b *= coverage; s.a *= coverage;

                    int i = y * size + x;
                    Color d = canvas[i];
                    float inv = 1f - s.a;
                    canvas[i] = new Color(s.r + d.r * inv, s.g + d.g * inv, s.b + d.b * inv, s.a + d.a * inv);
                }
            }
        }

        /// <summary>Scales an image to fit inside maxWidth x maxHeight keeping its aspect ratio.</summary>
        public static ImageBuffer ResizeToFit(ImagePyramid src, int maxWidth, int maxHeight)
        {
            float scale = Mathf.Min(maxWidth / (float)src.Width, maxHeight / (float)src.Height);
            int w = Mathf.Max(1, Mathf.RoundToInt(src.Width * scale));
            int h = Mathf.Max(1, Mathf.RoundToInt(src.Height * scale));
            int level = ChooseLevel(src, 1f / Mathf.Max(0.0001f, scale));
            var dst = new ImageBuffer(w, h);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Color c = src.SamplePremultiplied(level, (x + 0.5f) / w, (y + 0.5f) / h);
                    dst.Pixels[y * w + x] = IconImageProcessor.ToColor32(c);
                }
            }
            return dst;
        }
    }
}
