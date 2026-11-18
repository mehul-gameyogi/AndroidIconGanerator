using System.Collections.Generic;
using UnityEngine;

namespace AndroidIconGenerator
{
    /// <summary>
    /// Box-filtered mip chain of a source image. Choosing the right level before bilinear sampling gives
    /// alias-free downscaling (e.g. 4096px logo -> 48px icon) without any GPU state.
    /// Colours are averaged alpha-weighted so transparent texels never darken edges.
    /// </summary>
    public sealed class ImagePyramid
    {
        private readonly ImageBuffer[] _levels;

        public int LevelCount => _levels.Length;
        public int Width => _levels[0].Width;
        public int Height => _levels[0].Height;
        public ImageBuffer Level0 => _levels[0];

        private ImagePyramid(ImageBuffer[] levels) { _levels = levels; }

        public static ImagePyramid Build(ImageBuffer source)
        {
            var levels = new List<ImageBuffer> { source };
            ImageBuffer current = source;
            while (current.Width > 4 || current.Height > 4)
            {
                current = Downsample(current);
                levels.Add(current);
            }
            return new ImagePyramid(levels.ToArray());
        }

        private static ImageBuffer Downsample(ImageBuffer s)
        {
            int w = Mathf.Max(1, s.Width / 2);
            int h = Mathf.Max(1, s.Height / 2);
            var dst = new ImageBuffer(w, h);
            for (int y = 0; y < h; y++)
            {
                int y0 = Mathf.Min(2 * y, s.Height - 1);
                int y1 = Mathf.Min(2 * y + 1, s.Height - 1);
                for (int x = 0; x < w; x++)
                {
                    int x0 = Mathf.Min(2 * x, s.Width - 1);
                    int x1 = Mathf.Min(2 * x + 1, s.Width - 1);
                    Color32 a = s.Pixels[y0 * s.Width + x0];
                    Color32 b = s.Pixels[y0 * s.Width + x1];
                    Color32 c = s.Pixels[y1 * s.Width + x0];
                    Color32 d = s.Pixels[y1 * s.Width + x1];
                    int sumA = a.a + b.a + c.a + d.a;
                    if (sumA == 0) { dst.Pixels[y * w + x] = new Color32(0, 0, 0, 0); continue; }
                    int r = a.r * a.a + b.r * b.a + c.r * c.a + d.r * d.a;
                    int g = a.g * a.a + b.g * b.a + c.g * c.a + d.g * d.a;
                    int bl = a.b * a.a + b.b * b.a + c.b * c.a + d.b * d.a;
                    dst.Pixels[y * w + x] = new Color32(
                        (byte)((r + sumA / 2) / sumA),
                        (byte)((g + sumA / 2) / sumA),
                        (byte)((bl + sumA / 2) / sumA),
                        (byte)((sumA + 2) / 4));
                }
            }
            return dst;
        }

        /// <summary>Bilinear sample (clamp-to-edge) returning premultiplied RGBA. u and v are 0..1 across the level.</summary>
        public Color SamplePremultiplied(int level, float u, float v)
        {
            ImageBuffer l = _levels[level];
            float x = u * l.Width - 0.5f;
            float y = v * l.Height - 0.5f;
            int x0 = Mathf.FloorToInt(x);
            int y0 = Mathf.FloorToInt(y);
            float fx = x - x0;
            float fy = y - y0;

            Color c00 = Fetch(l, x0, y0);
            Color c10 = Fetch(l, x0 + 1, y0);
            Color c01 = Fetch(l, x0, y0 + 1);
            Color c11 = Fetch(l, x0 + 1, y0 + 1);

            float w00 = (1 - fx) * (1 - fy), w10 = fx * (1 - fy), w01 = (1 - fx) * fy, w11 = fx * fy;
            return new Color(
                c00.r * w00 + c10.r * w10 + c01.r * w01 + c11.r * w11,
                c00.g * w00 + c10.g * w10 + c01.g * w01 + c11.g * w11,
                c00.b * w00 + c10.b * w10 + c01.b * w01 + c11.b * w11,
                c00.a * w00 + c10.a * w10 + c01.a * w01 + c11.a * w11);
        }

        private static Color Fetch(ImageBuffer l, int x, int y)
        {
            x = Mathf.Clamp(x, 0, l.Width - 1);
            y = Mathf.Clamp(y, 0, l.Height - 1);
            Color32 p = l.Pixels[y * l.Width + x];
            float a = p.a * (1f / 255f);
            return new Color(p.r * a * (1f / 255f), p.g * a * (1f / 255f), p.b * a * (1f / 255f), a);
        }
    }
}
