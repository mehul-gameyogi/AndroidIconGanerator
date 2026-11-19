using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AndroidIconGenerator
{
    /// <summary>
    /// Tools &gt; Android Icon Generator. This class only does UI and orchestration; all processing,
    /// export, validation and Player Settings work lives in dedicated classes.
    /// </summary>
    public sealed class AndroidIconGeneratorWindow : EditorWindow
    {
        private const float PreviewPaneWidth = 400f;
        private const string ImportFolder = "Assets/AndroidIconSource";

        [SerializeField] private AndroidIconSettings settings;

        private SerializedObject so;
        private IconSourceSet sourceSet;
        private IconPreviewRenderer previews;
        private Texture2D sourceThumb;
        private ValidationReport report = new ValidationReport();
        private ExportResult lastExport;
        private Vector2 leftScroll, rightScroll;

        private bool foldSource = true, foldPlayer = true;

        // ------------------------------------------------------------------------------------------------
        // Entry points
        // ------------------------------------------------------------------------------------------------

        [MenuItem("Tools/Android Icon Generator")]
        public static void Open()
        {
            var window = GetWindow<AndroidIconGeneratorWindow>("Android Icon Generator");
            window.minSize = new Vector2(960f, 640f);
            window.Show();
        }

        public static void OpenWithSource(Texture2D texture)
        {
            Open();
            var window = GetWindow<AndroidIconGeneratorWindow>();
            window.EnsureReady();
            window.SetSourceTexture(texture);
        }

        // ------------------------------------------------------------------------------------------------
        // Lifecycle
        // ------------------------------------------------------------------------------------------------

        private void OnEnable()
        {
            titleContent = new GUIContent("Android Icons", EditorGUIUtility.IconContent("BuildSettings.Android.Small").image);
            minSize = new Vector2(960f, 640f);
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
            if (settings != null) IconGeneratorPreferences.Save(settings);
            previews?.Dispose();
            previews = null;
            TextureUtility.SafeDestroy(sourceThumb);
            sourceThumb = null;
            sourceSet = null;
            so = null;
        }

        private void OnDestroy()
        {
            if (settings != null) DestroyImmediate(settings);
        }

        private void OnFocus() { if (settings != null && ReloadSources()) RefreshPreviews(); Repaint(); }
        private void OnProjectChange() { if (settings != null && ReloadSources()) { RefreshPreviews(); Repaint(); } }

        private void OnUndoRedo()
        {
            if (settings == null) return;
            so?.Update();
            ReloadSources();
            RefreshPreviews();
            IconGeneratorPreferences.Save(settings);
            Repaint();
        }

        private void EnsureReady()
        {
            if (settings == null)
            {
                settings = IconGeneratorPreferences.LoadOrCreate();
                settings.outputDirectory = AndroidIconSettings.DefaultOutputDirectory;
            }
            if (so == null || so.targetObject == null) so = new SerializedObject(settings);
            if (previews == null) previews = new IconPreviewRenderer();
            if (sourceSet == null) { ReloadSources(true); RefreshPreviews(); }
        }

        /// <summary>Reloads source pyramids when the assigned images (or their content) changed.</summary>
        private bool ReloadSources(bool force = false)
        {
            string key = IconSourceSet.ComputeKey(settings);
            if (!force && sourceSet != null && sourceSet.Key == key) return false;

            var loadReport = new ValidationReport();
            sourceSet = IconSourceSet.Load(settings, loadReport);
            if (loadReport.HasErrors) { report = loadReport; loadReport.LogToConsole(false); }

            TextureUtility.UpdateTexture(ref sourceThumb,
                sourceSet.Source != null ? IconResizer.ResizeToFit(sourceSet.Source, 128, 128) : null);
            return true;
        }

        private void RefreshPreviews()
        {
            previews.Refresh(settings, sourceSet);
        }

        private void OnSettingsChanged()
        {
            ReloadSources();
            RefreshPreviews();
            IconGeneratorPreferences.Save(settings);
            Repaint();
        }

        /// <summary>Runs an action after the current GUI pass so dialogs and long operations are safe.</summary>
        private void Defer(Action action)
        {
            EditorApplication.delayCall += () =>
            {
                if (this == null) return;
                try { action(); }
                catch (Exception e)
                {
                    report = new ValidationReport();
                    report.Error("Unexpected error", e.Message, "See the Console for the full stack trace.");
                    Debug.LogException(e);
                }
                Repaint();
            };
        }

        // ------------------------------------------------------------------------------------------------
        // GUI
        // ------------------------------------------------------------------------------------------------

        private void OnGUI()
        {
            EnsureReady();
            so.Update();

            DrawToolbar();

            EditorGUILayout.BeginHorizontal();
            leftScroll = EditorGUILayout.BeginScrollView(leftScroll);
            DrawSourceSection();
            DrawGenerationActions();
            DrawPlayerSettingsSection();
            DrawReportSection();
            EditorGUILayout.EndScrollView();

            if (so.ApplyModifiedProperties()) OnSettingsChanged();

            rightScroll = EditorGUILayout.BeginScrollView(rightScroll, GUILayout.Width(PreviewPaneWidth));
            DrawPreviewPane();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndHorizontal();

            if (so.ApplyModifiedProperties()) IconGeneratorPreferences.Save(settings);
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Unity Android Icon Generator", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        // ---- Section 1: Source image ---------------------------------------------------------------------

        private void DrawSourceSection()
        {
            IconEditorGUI.Section(ref foldSource, "1. Source Image");
            if (foldSource)
            {
                EditorGUILayout.PropertyField(so.FindProperty("sourceImage"), new GUIContent("Source Image", "PNG or JPG texture used for every icon."));

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Import / Replace Image...", GUILayout.Height(22))) Defer(ImportSourceImage);
                using (new EditorGUI.DisabledScope(settings.sourceImage == null))
                    if (GUILayout.Button("Clear", GUILayout.Width(70), GUILayout.Height(22))) Defer(ClearSource);
                EditorGUILayout.EndHorizontal();

                if (sourceSet != null && sourceSet.Source != null)
                {
                    string path = AssetDatabase.GetAssetPath(settings.sourceImage);
                    long bytes = File.Exists(path) ? new FileInfo(path).Length : 0;
                    EditorGUILayout.BeginHorizontal();
                    Rect thumb = GUILayoutUtility.GetRect(96, 96, GUILayout.ExpandWidth(false));
                    IconEditorGUI.DrawTextureOnChecker(thumb, sourceThumb);
                    EditorGUILayout.BeginVertical();
                    EditorGUILayout.LabelField("Dimensions", sourceSet.Source.Width + " x " + sourceSet.Source.Height + " px");
                    EditorGUILayout.LabelField("Format", TextureUtility.FormatName(path));
                    EditorGUILayout.LabelField("Transparency", sourceSet.Source.Level0.HasTransparency() ? "Yes" : "No (fully opaque)");
                    if (bytes > 0) EditorGUILayout.LabelField("File size", (bytes / 1024f).ToString("0.#") + " KB");
                    EditorGUILayout.LabelField("Path", path, EditorStyles.miniLabel);
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.EndHorizontal();
                }
                else if (settings.sourceImage == null)
                {
                    EditorGUILayout.HelpBox("Assign or import a PNG/JPG image (at least 432x432 recommended; transparent background works best).", MessageType.Info);
                }
            }
            IconEditorGUI.EndSection();
        }

        private void DrawGenerationActions()
        {
            EditorGUILayout.Space(6f);
            using (new EditorGUI.DisabledScope(settings.sourceImage == null))
            {
                if (GUILayout.Button("Generate Icons", GUILayout.Height(32))) Defer(Generate);
            }
            EditorGUILayout.LabelField("Adaptive foreground/background: 432 x 432    Legacy/round: 192 x 192", EditorStyles.miniLabel);

            if (lastExport != null)
            {
                MessageType type = lastExport.Failed > 0 ? MessageType.Error : lastExport.Cancelled ? MessageType.Warning : MessageType.Info;
                EditorGUILayout.HelpBox(
                    $"Last generation: {lastExport.Written} written, {lastExport.Skipped} skipped, {lastExport.Failed} failed.",
                    type);
            }
        }

        // ---- Section 2: Player settings ------------------------------------------------------------------

        private void DrawPlayerSettingsSection()
        {
            IconEditorGUI.Section(ref foldPlayer, "2. Unity Player Settings (Android)");
            if (foldPlayer)
            {
                string unsupported = AndroidPlayerSettingsManager.UnsupportedReason();
                if (unsupported != null) EditorGUILayout.HelpBox(unsupported, MessageType.Warning);

                EditorGUILayout.PropertyField(so.FindProperty("assignLegacy"), new GUIContent("Assign Legacy Icons"));
                EditorGUILayout.PropertyField(so.FindProperty("assignRound"), new GUIContent("Assign Round Icons"));
                EditorGUILayout.PropertyField(so.FindProperty("assignAdaptive"), new GUIContent("Assign Adaptive Icons"));
                using (new EditorGUI.DisabledScope(unsupported != null))
                {
                    if (GUILayout.Button("Assign To Player Settings", GUILayout.Height(24))) Defer(() => AssignToPlayerSettings(true));
                }
            }
            IconEditorGUI.EndSection();
        }

        private void DrawReportSection()
        {
            if (report == null || report.Messages.Count == 0) return;
            EditorGUILayout.LabelField(
                $"Validation: {report.Count(Severity.Error)} errors, {report.Count(Severity.Warning)} warnings, {report.Count(Severity.Success)} OK",
                EditorStyles.boldLabel);
            IconEditorGUI.DrawReport(report);
        }

        // ---- Section 3: Live preview -----------------------------------------------------------------------

        private void DrawPreviewPane()
        {
            GUILayout.Label("3. Live Preview", EditorStyles.boldLabel);
            if (!previews.HasSource)
                EditorGUILayout.HelpBox("Assign a source image to see the legacy, round and adaptive foreground previews. The adaptive background preview works without one.", MessageType.Info);

            EditorGUILayout.BeginHorizontal();
            DrawPreviewItem("Legacy square", previews.Legacy, 150, false);
            DrawPreviewItem("Round", previews.Round, 150, false);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            DrawPreviewItem("Adaptive foreground", previews.Foreground, 150, settings.showSafeZone);
            DrawPreviewItem("Adaptive background", previews.Background, 150, false);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(so.FindProperty("showSafeZone"), new GUIContent("Show safe zone guides"));
            if (settings.showSafeZone)
                EditorGUILayout.LabelField("Green: 66dp safe zone    Yellow: 72dp visible area", EditorStyles.miniLabel);

            EditorGUILayout.Space(4);
            GUILayout.Label("Combined adaptive icon (as shown by launchers)", EditorStyles.boldLabel);
            AdaptiveMaskPreview.DrawMaskRow(previews, 112f);

            EditorGUILayout.Space(4);
            GUILayout.Label("Launcher preview", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(so.FindProperty("launcherMask"), new GUIContent("Adaptive mask"));
            LauncherPreview.Draw(previews, settings.launcherMask);
        }

        private static void DrawPreviewItem(string label, Texture2D texture, float size, bool guides)
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(size + 6));
            Rect r = GUILayoutUtility.GetRect(size, size, GUILayout.ExpandWidth(false));
            if (label.StartsWith("Adaptive")) AdaptiveMaskPreview.DrawLayer(r, texture, guides);
            else IconEditorGUI.DrawTextureOnChecker(r, texture);
            GUILayout.Label(label, EditorStyles.centeredGreyMiniLabel, GUILayout.Width(size));
            EditorGUILayout.EndVertical();
        }

        // ------------------------------------------------------------------------------------------------
        // Actions
        // ------------------------------------------------------------------------------------------------

        private void ClearSource()
        {
            Undo.RecordObject(settings, "Clear Source Image");
            settings.sourceImage = null;
            so.Update();
            OnSettingsChanged();
        }

        private void SetSourceTexture(Texture2D texture)
        {
            Undo.RecordObject(settings, "Set Source Image");
            settings.sourceImage = texture;
            so.Update();
            OnSettingsChanged();
        }

        private void ImportSourceImage()
        {
            string file = EditorUtility.OpenFilePanelWithFilters("Select source image", string.Empty,
                new[] { "Images (PNG, JPG)", "png,jpg,jpeg", "All files", "*" });
            if (string.IsNullOrEmpty(file)) return;

            if (!TextureUtility.IsSupportedExtension(file))
            {
                EditorUtility.DisplayDialog("Unsupported format", "Please choose a PNG, JPG or JPEG file.", "OK");
                return;
            }

            string assetPath;
            if (IconFolderManager.IsInsideProjectAssets(file))
            {
                assetPath = IconFolderManager.NormalizeOutputDirectory(file);
            }
            else
            {
                Directory.CreateDirectory(IconFolderManager.ToAbsolute(ImportFolder));
                assetPath = AssetDatabase.GenerateUniqueAssetPath(ImportFolder + "/" + Path.GetFileName(file));
                File.Copy(file, IconFolderManager.ToAbsolute(assetPath));
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
            }

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (texture == null)
            {
                report = new ValidationReport();
                report.Error("Source image", "Unity could not import " + Path.GetFileName(file) + " as a texture.",
                    "Make sure the file is a valid PNG/JPG.");
                return;
            }
            SetSourceTexture(texture);
        }

        private void Generate()
        {
            // The streamlined workflow always generates every required icon type.
            settings.legacyEnabled = true;
            settings.roundEnabled = true;
            settings.adaptiveForegroundEnabled = true;
            settings.adaptiveBackgroundEnabled = true;
            ReloadSources(true);
            report = IconValidator.ValidateSettings(settings, sourceSet);
            if (report.HasErrors)
            {
                report.LogToConsole(false);
                IconLogger.Error("Generation stopped: fix the errors shown in the window first.");
                return;
            }

            var plan = AndroidIconExporter.BuildPlan(settings, AndroidIconExporter.AllTypes());
            if (plan.Count == 0)
            {
                report.Warning("Export", "No icon files were planned.", "Close and reopen the generator, then try again.");
                return;
            }

            var policy = ExistingFilePolicy.Overwrite;
            int existing = plan.Count(f => f.Exists);
            if (existing > 0)
            {
                int choice = EditorUtility.DisplayDialogComplex("Overwrite existing icons?",
                    $"{existing} of {plan.Count} files already exist in\n{settings.outputDirectory}\n\nOverwrite them, or keep the existing files and only create missing ones?",
                    "Overwrite", "Cancel", "Skip Existing");
                if (choice == 1) return;
                if (choice == 2) policy = ExistingFilePolicy.SkipExisting;
            }

            lastExport = AndroidIconExporter.Export(settings, sourceSet, plan, policy);
            report.AddRange(lastExport.Report);
            report.LogToConsole(false);
            IconLogger.Log(lastExport.Success ? Severity.Success : Severity.Error,
                $"Generation finished: {lastExport.Written} written, {lastExport.Skipped} skipped, {lastExport.Failed} failed -> {settings.outputDirectory}");
        }

        private void AssignToPlayerSettings(bool reportFirst)
        {
            if (reportFirst) { report = new ValidationReport(); }
            if (!EditorUtility.DisplayDialog("Assign icons to Android Player Settings",
                    "This changes the icon slots in Project Settings > Player > Android > Icon.\n\nContinue?", "Assign", "Cancel"))
                return;

            ValidationReport result = AndroidPlayerSettingsManager.Assign(settings, settings.assignLegacy, settings.assignRound, settings.assignAdaptive);
            report.AddRange(result);
            result.LogToConsole();
        }
    }
}
