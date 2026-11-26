using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AndroidIconGenerator
{
    /// <summary>
    /// Image Asset Studio style preview: every adaptive mask shape, the full bleed layers and the legacy / round
    /// icons as a wrapping grid of tiles. Tiles are always sized for xxxhdpi.
    /// </summary>
    public static class IconPreviewGrid
    {
        private const float Gap = 10f;
        private const float LabelHeight = 18f;

        private struct Tile
        {
            public string Label;
            public Texture2D Texture;
            public float Dp;          // what the tile represents in dp (72 masked, 108 full bleed, 48 legacy)
            public bool Adaptive;
            public bool Bleed;
        }

        public static void Draw(IconPreviewRenderer previews, AndroidIconSettings s, float availableWidth)
        {
            float scale = IconSpecs.Scale(IconSpecs.BestFitDensity);
            var tiles = new List<Tile>
            {
                new Tile { Label = "Circle", Texture = previews.GetCombined(MaskShape.Circle), Dp = IconSpecs.AdaptiveVisibleDp, Adaptive = true },
                new Tile { Label = "Squircle", Texture = previews.GetCombined(MaskShape.Squircle), Dp = IconSpecs.AdaptiveVisibleDp, Adaptive = true },
                new Tile { Label = "Rounded Square", Texture = previews.GetCombined(MaskShape.RoundedSquare), Dp = IconSpecs.AdaptiveVisibleDp, Adaptive = true },
                new Tile { Label = "Square", Texture = previews.GetCombined(MaskShape.Square), Dp = IconSpecs.AdaptiveVisibleDp, Adaptive = true },
                new Tile { Label = "Full Bleed Layers", Texture = previews.FullBleed, Dp = IconSpecs.AdaptiveCanvasDp, Adaptive = true, Bleed = true },
                new Tile { Label = "Legacy Icon", Texture = previews.Legacy, Dp = IconSpecs.LauncherDp },
                new Tile { Label = "Round Icon", Texture = previews.Round, Dp = IconSpecs.LauncherDp },
            };

            // Deterministic layout: compute positions first, then reserve one rect of the total height.
            float width = Mathf.Max(120f, availableWidth);
            var rects = new Rect[tiles.Count];
            float x = 0f, y = 0f, rowHeight = 0f;
            for (int i = 0; i < tiles.Count; i++)
            {
                float size = Mathf.Min(tiles[i].Dp * scale, width);
                if (x > 0f && x + size > width) { x = 0f; y += rowHeight + Gap; rowHeight = 0f; }
                rects[i] = new Rect(x, y, size, size);
                x += size + Gap;
                rowHeight = Mathf.Max(rowHeight, size + LabelHeight);
            }
            float totalHeight = y + rowHeight;

            Rect area = GUILayoutUtility.GetRect(width, totalHeight, GUILayout.ExpandWidth(true));
            for (int i = 0; i < tiles.Count; i++)
            {
                Rect r = rects[i];
                r.x += area.x; r.y += area.y;
                Tile t = tiles[i];

                IconEditorGUI.DrawTextureOnChecker(r, t.Texture);
                if (s.showGrid) AdaptiveMaskPreview.DrawGrid(r, t.Dp, 6f);
                if (s.showSafeZone && t.Adaptive && Event.current.type == EventType.Repaint)
                    AdaptiveMaskPreview.DrawGuides(r, t.Dp);

                GUI.Label(new Rect(r.x, r.yMax, r.width, LabelHeight), t.Label, EditorStyles.centeredGreyMiniLabel);
            }
        }
    }
}
