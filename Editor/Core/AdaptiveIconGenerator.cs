using UnityEngine;

namespace AndroidIconGenerator
{
    /// <summary>
    /// Adaptive icon layers. Foreground and background are rendered as separate 108dp canvases
    /// (density scaled) and are never masked in the exported files; masks are only applied for previews.
    /// </summary>
    public static class AdaptiveIconGenerator
    {
        /// <summary>Foreground scale 1.0 fits the image inside the 66dp safe box.</summary>
        public const float SafeBoxFraction = IconSpecs.AdaptiveSafeDp / IconSpecs.AdaptiveCanvasDp;

        public static ImageBuffer RenderForeground(AndroidIconSettings s, IconSourceSet set, int size)
        {
            ImagePyramid src = set?.ForegroundOrSource;
            if (src == null) return null;
            Color[] canvas = IconImageProcessor.NewCanvas(size);
            bool fill = s.foregroundPlacement.fit == FitMode.Fill;
            IconImageProcessor.DrawImage(canvas, size, src, SafeBoxFraction, s.foregroundPlacement, fill);
            return IconImageProcessor.ToBuffer(canvas, size);
        }

        /// <summary>Always fully opaque, as Android expects for the background layer.</summary>
        public static ImageBuffer RenderBackground(AndroidIconSettings s, IconSourceSet set, int size)
        {
            Color[] canvas = IconImageProcessor.NewCanvas(size);
            switch (s.backgroundMode)
            {
                case BackgroundMode.Gradient:
                    IconImageProcessor.FillGradient(canvas, size, s.gradientColorA, s.gradientColorB, s.gradientAngle);
                    break;
                case BackgroundMode.Image:
                    IconImageProcessor.FillSolid(canvas, s.backgroundColor, true);
                    if (set != null && set.BackgroundImage != null)
                        IconImageProcessor.DrawImage(canvas, size, set.BackgroundImage, 1f, s.backgroundPlacement, false);
                    break;
                default:
                    IconImageProcessor.FillSolid(canvas, s.backgroundColor, true);
                    break;
            }
            return IconImageProcessor.ToBuffer(canvas, size);
        }

        /// <summary>
        /// Previews what a launcher shows: foreground over background, clipped by the mask shape and cropped
        /// to the central 72dp. Output size = canvas * 72/108.
        /// </summary>
        public static ImageBuffer ComposeMasked(ImageBuffer foreground, ImageBuffer background, MaskShape shape)
        {
            int size = background != null ? background.Width : foreground.Width;
            int crop = Mathf.RoundToInt(size * IconSpecs.AdaptiveVisibleDp / IconSpecs.AdaptiveCanvasDp);
            int offset = (size - crop) / 2;
            var output = new ImageBuffer(crop, crop);

            for (int y = 0; y < crop; y++)
            {
                float v = (y + 0.5f) / crop;
                for (int x = 0; x < crop; x++)
                {
                    float u = (x + 0.5f) / crop;
                    float cover = IconCropper.MaskCoverage(shape, u, v, 0.5f, crop);
                    if (cover <= 0f) { output.Pixels[y * crop + x] = new Color32(0, 0, 0, 0); continue; }

                    int si = (y + offset) * size + (x + offset);
                    Color32 bg = background != null ? background.Pixels[si] : new Color32(0, 0, 0, 0);
                    Color32 fg = foreground != null ? foreground.Pixels[si] : new Color32(0, 0, 0, 0);
                    float fa = fg.a / 255f;
                    float ba = bg.a / 255f;
                    float oa = fa + ba * (1f - fa);
                    float r = 0, g = 0, b = 0;
                    if (oa > 0f)
                    {
                        r = (fg.r * fa + bg.r * ba * (1f - fa)) / oa;
                        g = (fg.g * fa + bg.g * ba * (1f - fa)) / oa;
                        b = (fg.b * fa + bg.b * ba * (1f - fa)) / oa;
                    }
                    output.Pixels[y * crop + x] = new Color32(
                        (byte)Mathf.Clamp(Mathf.RoundToInt(r), 0, 255),
                        (byte)Mathf.Clamp(Mathf.RoundToInt(g), 0, 255),
                        (byte)Mathf.Clamp(Mathf.RoundToInt(b), 0, 255),
                        (byte)Mathf.Clamp(Mathf.RoundToInt(oa * cover * 255f), 0, 255));
                }
            }
            return output;
        }
    }
}
