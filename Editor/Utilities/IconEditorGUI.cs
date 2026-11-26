using UnityEditor;
using UnityEngine;

namespace AndroidIconGenerator
{
    /// <summary>Shared IMGUI helpers: checkerboard, section frames, validation message rows.</summary>
    public static class IconEditorGUI
    {
        private const int CheckerCell = 8;
        private static Texture2D _checker;
        private static GUIStyle _wrapLabel;
        private static GUIStyle _solutionLabel;
        private static GUIStyle _sectionTitle;

        static IconEditorGUI()
        {
            AssemblyReloadEvents.beforeAssemblyReload += () => TextureUtility.SafeDestroy(_checker);
        }

        private static Texture2D Checker
        {
            get
            {
                if (_checker != null) return _checker;
                var t = new Texture2D(CheckerCell * 2, CheckerCell * 2, TextureFormat.RGBA32, false)
                {
                    hideFlags = HideFlags.HideAndDontSave,
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Repeat,
                };
                Color light = EditorGUIUtility.isProSkin ? new Color(0.36f, 0.36f, 0.36f) : new Color(0.85f, 0.85f, 0.85f);
                Color dark = EditorGUIUtility.isProSkin ? new Color(0.27f, 0.27f, 0.27f) : new Color(0.72f, 0.72f, 0.72f);
                for (int y = 0; y < t.height; y++)
                    for (int x = 0; x < t.width; x++)
                        t.SetPixel(x, y, ((x / CheckerCell) + (y / CheckerCell)) % 2 == 0 ? light : dark);
                t.Apply(false);
                _checker = t;
                return _checker;
            }
        }

        public static void DrawChecker(Rect rect)
        {
            if (Event.current.type != EventType.Repaint) return;
            float cell = CheckerCell * 2f;
            GUI.DrawTextureWithTexCoords(rect, Checker, new Rect(0, 0, rect.width / cell, rect.height / cell));
        }

        /// <summary>Draws a texture scaled to fit a square rect on top of a checkerboard.</summary>
        public static void DrawTextureOnChecker(Rect rect, Texture2D tex)
        {
            DrawChecker(rect);
            if (tex != null && Event.current.type == EventType.Repaint)
                GUI.DrawTexture(rect, tex, ScaleMode.ScaleToFit, true);
        }

        public static void Section(ref bool foldout, string title)
        {
            if (_sectionTitle == null)
                _sectionTitle = new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Bold };
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            foldout = EditorGUILayout.Foldout(foldout, title, true, _sectionTitle);
        }

        public static void EndSection()
        {
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }

        public static void DrawMessage(ValidationMessage m)
        {
            if (_wrapLabel == null)
            {
                _wrapLabel = new GUIStyle(EditorStyles.label) { wordWrap = true, richText = true };
                _solutionLabel = new GUIStyle(EditorStyles.miniLabel) { wordWrap = true, fontStyle = FontStyle.Italic };
            }
            string iconName = m.Severity == Severity.Error ? "console.erroricon.sml"
                : m.Severity == Severity.Warning ? "console.warnicon.sml" : "TestPassed";
            GUIContent icon = EditorGUIUtility.IconContent(iconName);

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUILayout.Label(icon, GUILayout.Width(20), GUILayout.Height(20));
            EditorGUILayout.BeginVertical();
            GUILayout.Label("<b>" + m.Category + "</b>  " + m.Message, _wrapLabel);
            if (!string.IsNullOrEmpty(m.Solution)) GUILayout.Label("Fix: " + m.Solution, _solutionLabel);
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }

        public static void DrawReport(ValidationReport report)
        {
            if (report == null) return;
            foreach (ValidationMessage m in report.Messages)
                if (m.Severity != Severity.Warning) DrawMessage(m);
        }
    }
}
