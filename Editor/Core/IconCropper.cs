using UnityEngine;

namespace AndroidIconGenerator
{
    /// <summary>Geometry helpers: image placement rectangles, crop clipping and launcher mask shapes.</summary>
    public static class IconCropper
    {
        /// <summary>Image rectangle in normalised canvas units (0..1, origin bottom-left).</summary>
        public struct ImageRect
        {
            public float cx, cy, w, h;
        }

        public const float RoundedSquareRadius = 0.22f;   // of the mask width
        public const float SquircleExponent = 4f;

        /// <summary>
        /// Fit: the whole image fits a square box of <paramref name="boxFraction"/> of the canvas.
        /// Fill: the image covers the box (cropping the overflow). Scale multiplies the result.
        /// </summary>
        public static ImageRect ComputeImageRect(int srcW, int srcH, float boxFraction, ImagePlacement p)
        {
            float box = boxFraction * Mathf.Max(0.0001f, p.scale);
            float reference = p.fit == FitMode.Fit ? Mathf.Max(srcW, srcH) : Mathf.Min(srcW, srcH);
            float k = box / Mathf.Max(1f, reference);
            return new ImageRect
            {
                cx = 0.5f + p.offsetX,
                cy = 0.5f + p.offsetY,
                w = srcW * k,
                h = srcH * k,
            };
        }

        /// <summary>Anti-aliased coverage (0..1) of a canvas point inside the image rectangle.</summary>
        public static float RectCoverage(float u, float v, ImageRect r, float pixelsPerUnit)
        {
            float dx = (0.5f - Mathf.Abs((u - r.cx) / r.w)) * r.w * pixelsPerUnit;
            float dy = (0.5f - Mathf.Abs((v - r.cy) / r.h)) * r.h * pixelsPerUnit;
            return Mathf.Clamp01(dx + 0.5f) * Mathf.Clamp01(dy + 0.5f);
        }

        /// <summary>Anti-aliased coverage inside a centred square clip box of the given half extent (canvas units).</summary>
        public static float ClipBoxCoverage(float u, float v, float halfExtent, float pixelsPerUnit)
        {
            float dx = (halfExtent - Mathf.Abs(u - 0.5f)) * pixelsPerUnit;
            float dy = (halfExtent - Mathf.Abs(v - 0.5f)) * pixelsPerUnit;
            return Mathf.Clamp01(dx + 0.5f) * Mathf.Clamp01(dy + 0.5f);
        }

        /// <summary>
        /// Mask coverage (0..1) for a point. The mask is centred on the canvas and spans
        /// [-halfExtent, +halfExtent] in canvas units.
        /// </summary>
        public static float MaskCoverage(MaskShape shape, float u, float v, float halfExtent, float pixelsPerUnit)
        {
            float px = u - 0.5f;
            float py = v - 0.5f;
            switch (shape)
            {
                case MaskShape.Circle:
                {
                    float dist = Mathf.Sqrt(px * px + py * py);
                    return Mathf.Clamp01((halfExtent - dist) * pixelsPerUnit + 0.5f);
                }
                case MaskShape.Squircle:
                {
                    float ax = Mathf.Abs(px) / halfExtent;
                    float ay = Mathf.Abs(py) / halfExtent;
                    float f = Mathf.Pow(ax, SquircleExponent) + Mathf.Pow(ay, SquircleExponent);
                    float radial = Mathf.Pow(f, 1f / SquircleExponent);       // 1 on the boundary
                    return Mathf.Clamp01((1f - radial) * halfExtent * pixelsPerUnit + 0.5f);
                }
                default:
                {
                    float r = shape == MaskShape.Square ? 0f : RoundedSquareRadius * halfExtent * 2f;
                    float qx = Mathf.Abs(px) - (halfExtent - r);
                    float qy = Mathf.Abs(py) - (halfExtent - r);
                    float outside = Mathf.Sqrt(Mathf.Max(qx, 0f) * Mathf.Max(qx, 0f) + Mathf.Max(qy, 0f) * Mathf.Max(qy, 0f));
                    float inside = Mathf.Min(Mathf.Max(qx, qy), 0f);
                    float d = outside + inside - r;                           // signed distance, <0 inside
                    return Mathf.Clamp01(0.5f - d * pixelsPerUnit);
                }
            }
        }

        /// <summary>Multiplies a premultiplied canvas by the mask (circular crop etc.).</summary>
        public static void ApplyMask(Color[] canvas, int size, MaskShape shape, float halfExtent)
        {
            float ppu = size;
            for (int y = 0; y < size; y++)
            {
                float v = (y + 0.5f) / size;
                for (int x = 0; x < size; x++)
                {
                    float cov = MaskCoverage(shape, (x + 0.5f) / size, v, halfExtent, ppu);
                    int i = y * size + x;
                    if (cov >= 1f) continue;
                    Color c = canvas[i];
                    canvas[i] = new Color(c.r * cov, c.g * cov, c.b * cov, c.a * cov);
                }
            }
        }
    }
}
