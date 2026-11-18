using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AndroidIconGenerator
{
    /// <summary>Draws adaptive layer previews with the 108dp canvas, 72dp visible area and 66dp safe-zone guides.</summary>
    public static class AdaptiveMaskPreview
    {
        public static string Label(MaskShape shape)
        {
            return shape == MaskShape.RoundedSquare ? "Rounded square" : shape.ToString();
        }

        /// <summary>Draws a 108dp layer texture (with checkerboard) and, optionally, the safe-zone guides on top.</summary>
        public static void DrawLayer(Rect rect, Texture2D layer, bool showGuides)
        {
            IconEditorGUI.DrawTextureOnChecker(rect, layer);
            if (showGuides && Event.current.type == EventType.Repaint) DrawGuides(rect);
        }

        public static void DrawGuides(Rect rect)
        {
            float pxPerDp = rect.width / IconSpecs.AdaptiveCanvasDp;
            Vector2 center = rect.center;

            Handles.BeginGUI();
            Color old = Handles.color;

            // 72dp visible area (what the launcher can reveal)
            float half = IconSpecs.AdaptiveVisibleDp * 0.5f * pxPerDp;
            Handles.color = new Color(1f, 0.9f, 0.2f, 0.9f);
            Handles.DrawAAPolyLine(1.5f, new Vector3[]
            {
                new Vector3(center.x - half, center.y - half), new Vector3(center.x + half, center.y - half),
                new Vector3(center.x + half, center.y + half), new Vector3(center.x - half, center.y + half),
                new Vector3(center.x - half, center.y - half),
            });

            // 66dp safe-zone circle
            Handles.color = new Color(0.2f, 1f, 0.5f, 0.95f);
            float radius = IconSpecs.AdaptiveSafeDp * 0.5f * pxPerDp;
            const int segments = 64;
            var points = new List<Vector3>(segments + 1);
            for (int i = 0; i <= segments; i++)
            {
                float a = i / (float)segments * Mathf.PI * 2f;
                points.Add(new Vector3(center.x + Mathf.Cos(a) * radius, center.y + Mathf.Sin(a) * radius));
            }
            Handles.DrawAAPolyLine(1.5f, points.ToArray());

            Handles.color = old;
            Handles.EndGUI();
        }

        /// <summary>Row of combined foreground+background previews, one per mask shape.</summary>
        public static void DrawMaskRow(IconPreviewRenderer previews, float size)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            foreach (MaskShape shape in System.Enum.GetValues(typeof(MaskShape)))
            {
                EditorGUILayout.BeginVertical(GUILayout.Width(size));
                Rect r = GUILayoutUtility.GetRect(size, size, GUILayout.ExpandWidth(false));
                IconEditorGUI.DrawTextureOnChecker(r, previews.GetCombined(shape));
                GUILayout.Label(Label(shape), EditorStyles.centeredGreyMiniLabel, GUILayout.Width(size));
                EditorGUILayout.EndVertical();
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }
    }
}
