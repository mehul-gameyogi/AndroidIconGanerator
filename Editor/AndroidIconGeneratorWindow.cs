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
        private const float LeftPaneWidth = 430f;
        private static readonly string[] TabNames = { "Foreground Layer", "Background Layer", "Options" };
        private static readonly string[] BackgroundModeNames = { "Color", "Gradient", "Image" };
        private static readonly string[] YesNo = { "Yes", "No" };
        private const string ImportFolder = "Assets/AndroidIconSource";

        [SerializeField] private AndroidIconSettings settings;

        private SerializedObject so;
        private IconSourceSet sourceSet;
        private IconPreviewRenderer previews;
        private Texture2D sourceThumb;
        private ValidationReport report = new ValidationReport();
        private ExportResult lastExport;
        private Vector2 leftScroll, rightScroll;

        private bool foldPlayer = true;

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
                UpdateOutputDirectory();
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
            UpdateOutputDirectory();
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

            // Left: configuration (Asset Studio style tabs) + output actions
            EditorGUILayout.BeginVertical(GUILayout.Width(LeftPaneWidth));
            leftScroll = EditorGUILayout.BeginScrollView(leftScroll);
            DrawHeaderFields();
            DrawTabs();
            DrawGenerationActions();
            DrawPlayerSettingsSection();
            DrawReportSection();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            // Right: preview
            EditorGUILayout.BeginVertical();
            DrawPreviewHeader();
            rightScroll = EditorGUILayout.BeginScrollView(rightScroll);
            DrawPreviewPane();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            if (so.ApplyModifiedProperties()) OnSettingsChanged();

            if (so.ApplyModifiedProperties()) IconGeneratorPreferences.Save(settings);
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Unity Android Icon Generator", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        // ---- Image Asset Studio style configuration -------------------------------------------------------

        private void DrawHeaderFields()
        {
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.Popup("Icon type", 0, new[] { "Launcher Icons (Adaptive and Legacy)" });
                EditorGUILayout.TextField("Name", "ic_launcher");
            }
            EditorGUILayout.Space(4f);
        }

        private void DrawTabs()
        {
            SerializedProperty tab = so.FindProperty("activeTab");
            int selected = GUILayout.Toolbar(tab.enumValueIndex, TabNames, GUILayout.Height(24f));
            if (selected != tab.enumValueIndex) tab.enumValueIndex = selected;

            EditorGUILayout.Space(6f);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            switch (settings.activeTab)
            {
                case LayerTab.Foreground: DrawForegroundTab(); break;
                case LayerTab.Background: DrawBackgroundTab(); break;
                default: DrawOptionsTab(); break;
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawForegroundTab()
        {
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.TextField("Layer name", "ic_launcher_foreground");

            GUILayout.Label("Source Asset", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.Popup("Asset type", 0, new[] { "Image" });

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(so.FindProperty("sourceImage"), new GUIContent("Path", "PNG or JPG texture used for every icon."));
            if (GUILayout.Button(EditorGUIUtility.IconContent("Folder Icon"), GUILayout.Width(28f), GUILayout.Height(18f)))
                Defer(ImportSourceImage);
            using (new EditorGUI.DisabledScope(settings.sourceImage == null))
                if (GUILayout.Button("Clear", GUILayout.Width(48f))) Defer(ClearSource);
            EditorGUILayout.EndHorizontal();

            if (sourceSet != null && sourceSet.Source != null)
            {
                string path = AssetDatabase.GetAssetPath(settings.sourceImage);
                EditorGUILayout.LabelField(
                    sourceSet.Source.Width + " x " + sourceSet.Source.Height + " px, " + TextureUtility.FormatName(path)
                    + (sourceSet.Source.Level0.HasTransparency() ? ", transparent" : ", opaque"),
                    EditorStyles.miniLabel);
            }
            else if (settings.sourceImage == null)
            {
                EditorGUILayout.HelpBox("Assign or import a PNG/JPG image (at least 432x432 recommended; transparent background works best).", MessageType.Info);
            }

            EditorGUILayout.Space(4f);
            GUILayout.Label("Scaling", EditorStyles.boldLabel);
            DrawYesNo("Trim", so.FindProperty("foregroundTrim"));
            DrawResizeSlider("Resize", so.FindProperty("foregroundPlacement.scale"));
        }

        private void DrawBackgroundTab()
        {
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.TextField("Layer name", "ic_launcher_background");

            GUILayout.Label("Source Asset", EditorStyles.boldLabel);
            SerializedProperty mode = so.FindProperty("backgroundMode");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Asset type");
            int selected = GUILayout.Toolbar(mode.enumValueIndex, BackgroundModeNames);
            if (selected != mode.enumValueIndex) mode.enumValueIndex = selected;
            EditorGUILayout.EndHorizontal();

            switch (settings.backgroundMode)
            {
                case BackgroundMode.Gradient:
                    EditorGUILayout.PropertyField(so.FindProperty("gradientColorA"), new GUIContent("Start color"));
                    EditorGUILayout.PropertyField(so.FindProperty("gradientColorB"), new GUIContent("End color"));
                    SerializedProperty angle = so.FindProperty("gradientAngle");
                    angle.floatValue = EditorGUILayout.Slider("Angle", angle.floatValue, 0f, 360f);
                    break;
                case BackgroundMode.Image:
                    EditorGUILayout.PropertyField(so.FindProperty("backgroundImage"), new GUIContent("Path"));
                    EditorGUILayout.PropertyField(so.FindProperty("backgroundColor"), new GUIContent("Fallback color"));
                    EditorGUILayout.Space(4f);
                    GUILayout.Label("Scaling", EditorStyles.boldLabel);
                    DrawResizeSlider("Resize", so.FindProperty("backgroundPlacement.scale"));
                    break;
                default:
                    EditorGUILayout.PropertyField(so.FindProperty("backgroundColor"), new GUIContent("Color"));
                    break;
            }
        }

        private void DrawOptionsTab()
        {
            GUILayout.Label("Legacy Icon (API 25 and below)", EditorStyles.boldLabel);
            DrawResizeSlider("Resize", so.FindProperty("legacyPlacement.scale"));
            EditorGUILayout.PropertyField(so.FindProperty("legacyUseBackground"), new GUIContent("Fill background"));
            if (settings.legacyUseBackground)
                EditorGUILayout.PropertyField(so.FindProperty("legacyBackgroundColor"), new GUIContent("Background color"));

            EditorGUILayout.Space(6f);
            GUILayout.Label("Round Icon", EditorStyles.boldLabel);
            DrawResizeSlider("Resize", so.FindProperty("roundPlacement.scale"));
            EditorGUILayout.PropertyField(so.FindProperty("roundUseBackground"), new GUIContent("Fill background"));
            if (settings.roundUseBackground)
                EditorGUILayout.PropertyField(so.FindProperty("roundBackgroundColor"), new GUIContent("Background color"));
        }

        /// <summary>Resize slider shown as a percentage (100% = default fit), stored as a 0..4 scale factor.</summary>
        private static void DrawResizeSlider(string label, SerializedProperty scale)
        {
            EditorGUI.BeginChangeCheck();
            int percent = EditorGUILayout.IntSlider(label, Mathf.RoundToInt(scale.floatValue * 100f), 1, 400);
            if (EditorGUI.EndChangeCheck()) scale.floatValue = percent / 100f;
        }

        private static void DrawYesNo(string label, SerializedProperty flag)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(label);
            int selected = GUILayout.Toolbar(flag.boolValue ? 0 : 1, YesNo, GUILayout.Width(100f));
            flag.boolValue = selected == 0;
            EditorGUILayout.EndHorizontal();
        }

        private void DrawGenerationActions()
        {
            EditorGUILayout.Space(6f);
            SerializedProperty createFolder = so.FindProperty("createOutputFolder");
            EditorGUILayout.PropertyField(createFolder,
                new GUIContent("Create Save Folder", "When enabled, uses the selected save folder. When disabled, saves directly in Assets."));
            SerializedProperty saveFolder = so.FindProperty("saveFolderDirectory");
            string selectedFolder = string.IsNullOrWhiteSpace(saveFolder.stringValue)
                ? AndroidIconSettings.DefaultOutputDirectory
                : IconFolderManager.NormalizeOutputDirectory(saveFolder.stringValue);
            string outputPath = createFolder.boolValue ? selectedFolder : "Assets";
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Save Location", outputPath, EditorStyles.miniLabel);
            if (GUILayout.Button("Browse...", GUILayout.Width(75f))) Defer(BrowseSaveLocation);
            EditorGUILayout.EndHorizontal();

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
            IconEditorGUI.Section(ref foldPlayer, "Unity Player Settings (Android)");
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

        private void DrawPreviewHeader()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Preview", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            SerializedProperty density = so.FindProperty("previewDensity");
            density.enumValueIndex = EditorGUILayout.Popup(density.enumValueIndex, density.enumDisplayNames, GUILayout.Width(80f));
            GUILayout.Space(8f);
            DrawHeaderToggle("showSafeZone", "Show safe zone");
            GUILayout.Space(8f);
            DrawHeaderToggle("showGrid", "Show grid");
            EditorGUILayout.EndHorizontal();
        }

        private void DrawHeaderToggle(string property, string label)
        {
            SerializedProperty p = so.FindProperty(property);
            p.boolValue = EditorGUILayout.ToggleLeft(label, p.boolValue, GUILayout.Width(EditorStyles.label.CalcSize(new GUIContent(label)).x + 22f));
        }

        private void DrawPreviewPane()
        {
            if (!previews.HasSource)
                EditorGUILayout.HelpBox("Assign a source image to see the foreground in the previews. The background layer works without one.", MessageType.Info);

            IconPreviewGrid.Draw(previews, settings, position.width - LeftPaneWidth - 40f);

            if (settings.showSafeZone)
                EditorGUILayout.LabelField("Green circle: 66dp safe zone    Yellow square: 72dp visible area", EditorStyles.miniLabel);

            EditorGUILayout.Space(8);
            GUILayout.Label("Launcher preview", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(so.FindProperty("launcherMask"), new GUIContent("Adaptive mask"));
            LauncherPreview.Draw(previews, settings.launcherMask);
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
            UpdateOutputDirectory();
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
            UpdateOutputDirectory();
            if (reportFirst) { report = new ValidationReport(); }
            if (!EditorUtility.DisplayDialog("Assign icons to Android Player Settings",
                    "This changes the icon slots in Project Settings > Player > Android > Icon.\n\nContinue?", "Assign", "Cancel"))
                return;

            ValidationReport result = AndroidPlayerSettingsManager.Assign(settings, settings.assignLegacy, settings.assignRound, settings.assignAdaptive);
            report.AddRange(result);
            result.LogToConsole();
        }

        private void BrowseSaveLocation()
        {
            string current = string.IsNullOrWhiteSpace(settings.saveFolderDirectory)
                ? AndroidIconSettings.DefaultOutputDirectory
                : settings.saveFolderDirectory;
            string absolute = IconFolderManager.ToAbsolute(IconFolderManager.NormalizeOutputDirectory(current));
            string start = Directory.Exists(absolute) ? absolute : Application.dataPath;
            string chosen = EditorUtility.OpenFolderPanel("Choose icon save location", start, string.Empty);
            if (string.IsNullOrEmpty(chosen)) return;

            Undo.RecordObject(settings, "Change Icon Save Location");
            settings.saveFolderDirectory = IconFolderManager.NormalizeOutputDirectory(chosen);
            settings.createOutputFolder = true;
            UpdateOutputDirectory();
            so.Update();
            IconGeneratorPreferences.Save(settings);
            Repaint();
        }

        private void UpdateOutputDirectory()
        {
            if (string.IsNullOrWhiteSpace(settings.saveFolderDirectory))
                settings.saveFolderDirectory = AndroidIconSettings.DefaultOutputDirectory;
            settings.outputDirectory = settings.createOutputFolder
                ? IconFolderManager.NormalizeOutputDirectory(settings.saveFolderDirectory)
                : "Assets";
        }
    }
}
