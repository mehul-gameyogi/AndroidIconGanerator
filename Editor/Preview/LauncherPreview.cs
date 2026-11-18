using UnityEditor;
using UnityEngine;

namespace AndroidIconGenerator
{
    /// <summary>A mock launcher home screen showing the legacy, round and adaptive icons side by side.</summary>
    public static class LauncherPreview
    {
        private static GUIStyle _label;

        public static void Draw(IconPreviewRenderer previews, MaskShape adaptiveMask)
        {
            if (_label == null)
            {
                _label = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.UpperCenter,
                    normal = { textColor = Color.white },
                    clipping = TextClipping.Clip,
                };
            }

            Rect panel = GUILayoutUtility.GetRect(10, 122, GUILayout.ExpandWidth(true));
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(panel, new Color(0.10f, 0.13f, 0.20f));
                EditorGUI.DrawRect(new Rect(panel.x, panel.y, panel.width, panel.height * 0.5f), new Color(0.14f, 0.18f, 0.28f, 0.6f));
            }

            const float icon = 64f;
            float slot = panel.width / 3f;
            DrawIcon(panel, 0, slot, icon, previews.Legacy, "Legacy");
            DrawIcon(panel, 1, slot, icon, previews.Round, "Round");
            DrawIcon(panel, 2, slot, icon, previews.GetCombined(adaptiveMask), "Adaptive");
        }

        private static void DrawIcon(Rect panel, int index, float slot, float size, Texture2D tex, string caption)
        {
            var r = new Rect(panel.x + slot * index + (slot - size) * 0.5f, panel.y + 18f, size, size);
            if (Event.current.type != EventType.Repaint) return;
            if (tex != null) GUI.DrawTexture(r, tex, ScaleMode.ScaleToFit, true);
            GUI.Label(new Rect(panel.x + slot * index, r.yMax + 4f, slot, 18f), caption, _label);
        }
    }
}
