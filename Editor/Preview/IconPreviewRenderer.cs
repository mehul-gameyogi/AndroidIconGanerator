using System;
using UnityEngine;

namespace AndroidIconGenerator
{
    /// <summary>
    /// Renders in-memory preview textures with the very same pipeline as the exporter, so previews are exact
    /// but never touch the disk. Textures are reused between refreshes.
    /// </summary>
    public sealed class IconPreviewRenderer : IDisposable
    {
        public const int LauncherPreviewSize = 192;      // xxxhdpi launcher icon
        public const int AdaptiveCanvasSize = 216;       // 2px per dp; must be a multiple of 3

        public Texture2D Legacy;
        public Texture2D Round;
        public Texture2D Foreground;
        public Texture2D Background;
        public Texture2D FullBleed;
        public readonly Texture2D[] Combined = new Texture2D[Enum.GetValues(typeof(MaskShape)).Length];

        public bool HasSource { get; private set; }

        public Texture2D GetCombined(MaskShape shape) { return Combined[(int)shape]; }

        public void Refresh(AndroidIconSettings s, IconSourceSet set)
        {
            HasSource = set != null && set.Source != null;

            TextureUtility.UpdateTexture(ref Legacy, IconImageProcessor.RenderLegacy(s, set, LauncherPreviewSize));
            TextureUtility.UpdateTexture(ref Round, IconImageProcessor.RenderRound(s, set, LauncherPreviewSize));

            ImageBuffer fg = AdaptiveIconGenerator.RenderForeground(s, set, AdaptiveCanvasSize);
            ImageBuffer bg = AdaptiveIconGenerator.RenderBackground(s, set, AdaptiveCanvasSize);
            TextureUtility.UpdateTexture(ref Foreground, fg);
            TextureUtility.UpdateTexture(ref Background, bg);
            TextureUtility.UpdateTexture(ref FullBleed, AdaptiveIconGenerator.ComposeFullBleed(fg, bg));

            foreach (MaskShape shape in Enum.GetValues(typeof(MaskShape)))
            {
                int i = (int)shape;
                TextureUtility.UpdateTexture(ref Combined[i], AdaptiveIconGenerator.ComposeMasked(fg, bg, shape));
            }
        }

        public void Dispose()
        {
            TextureUtility.SafeDestroy(Legacy);
            TextureUtility.SafeDestroy(Round);
            TextureUtility.SafeDestroy(Foreground);
            TextureUtility.SafeDestroy(Background);
            TextureUtility.SafeDestroy(FullBleed);
            FullBleed = null;
            for (int i = 0; i < Combined.Length; i++)
            {
                TextureUtility.SafeDestroy(Combined[i]);
                Combined[i] = null;
            }
            Legacy = Round = Foreground = Background = null;
        }
    }
}
