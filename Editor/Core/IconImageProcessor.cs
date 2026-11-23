using UnityEngine;

namespace AndroidIconGenerator
{
    /// <summary>
    /// Renders legacy and round icons and provides the canvas primitives (fill, gradient, composite, convert)
    /// used by every generator. Canvases are premultiplied Color[] arrays; outputs are straight-alpha ImageBuffers.
    /// The source asset is never modified.
    /// </summary>
    public static class IconImageProcessor
    {
        public static Color[] NewCanvas(int size) { return new Color[size * size]; }

        public static void FillSolid(Color[] canvas, Color color, bool forceOpaque = false)
        {
            if (forceOpaque) color.a = 1f;
            var pm = new Color(color.r * color.a, color.g * color.a, color.b * color.a, color.a);
            for (int i = 0; i < canvas.Length; i++) canvas[i] = pm;
        }

        /// <summary>Linear gradient. Angle 0 = A on the left to B on the right; 90 = A bottom to B top; 270 = A top to B bottom.</summary>
        public static void FillGradient(Color[] canvas, int size, Color a, Color b, float angleDegrees)
        {
            float rad = angleDegrees * Mathf.Deg2Rad;
            float dx = Mathf.Cos(rad), dy = Mathf.Sin(rad);
            float norm = Mathf.Abs(dx) + Mathf.Abs(dy);
            a.a = 1f; b.a = 1f;
            for (int y = 0; y < size; y++)
            {
                float v = (y + 0.5f) / size - 0.5f;
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size - 0.5f;
                    float t = Mathf.Clamp01(0.5f + (u * dx + v * dy) / Mathf.Max(0.0001f, norm));
                    canvas[y * size + x] = Color.Lerp(a, b, t);
                }
            }
        }

        /// <summary>Draws an image layer onto the canvas honouring scale / position / crop.</summary>
        public static void DrawImage(Color[] canvas, int size, ImagePyramid src, float boxFraction, ImagePlacement placement, bool clipToBox)
        {
            if (src == null) return;
            var rect = IconCropper.ComputeImageRect(src.Width, src.Height, boxFraction, placement);
            float clip = clipToBox && boxFraction < 1f ? boxFraction * 0.5f : 0f;
            IconResizer.DrawImage(canvas, size, src, rect, clip);
        }

        public static Color32 ToColor32(Color premultiplied)
        {
            float a = Mathf.Clamp01(premultiplied.a);
            if (a <= 0.0001f) return new Color32(0, 0, 0, 0);
            float inv = 1f / a;
            return new Color32(
                (byte)Mathf.Clamp(Mathf.RoundToInt(premultiplied.r * inv * 255f), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(premultiplied.g * inv * 255f), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(premultiplied.b * inv * 255f), 0, 255),
                (byte)Mathf.Clamp(Mathf.RoundToInt(a * 255f), 0, 255));
        }

        public static ImageBuffer ToBuffer(Color[] canvas, int size)
        {
            var buffer = new ImageBuffer(size, size);
            for (int i = 0; i < canvas.Length; i++) buffer.Pixels[i] = ToColor32(canvas[i]);
            return buffer;
        }

        // ---- Legacy / Round ---------------------------------------------------------------------------------

        private const float LegacyForegroundBox = 0.8f;
        private const float RoundForegroundBox = 0.7f;

        /// <summary>Layer 1 (background image or solid color) with layer 2 (foreground) drawn on top.</summary>
        private static Color[] ComposeLayers(AndroidIconSettings s, IconSourceSet set, int size, float foregroundBox)
        {
            if (set == null || (set.ForegroundOrSource == null && set.BackgroundImage == null)) return null;
            Color[] canvas = NewCanvas(size);
            FillSolid(canvas, s.backgroundColor, true);
            if (set.BackgroundImage != null)
                DrawImage(canvas, size, set.BackgroundImage, 1f, s.backgroundPlacement, false);
            if (set.ForegroundOrSource != null)
                DrawImage(canvas, size, set.ForegroundOrSource, foregroundBox, s.foregroundPlacement, false);
            return canvas;
        }

        public static ImageBuffer RenderLegacy(AndroidIconSettings s, IconSourceSet set, int size)
        {
            Color[] canvas = ComposeLayers(s, set, size, LegacyForegroundBox);
            return canvas == null ? null : ToBuffer(canvas, size);
        }

        public static ImageBuffer RenderRound(AndroidIconSettings s, IconSourceSet set, int size)
        {
            Color[] canvas = ComposeLayers(s, set, size, RoundForegroundBox);
            if (canvas == null) return null;
            IconCropper.ApplyMask(canvas, size, MaskShape.Circle, 0.5f);
            return ToBuffer(canvas, size);
        }
    }
}
