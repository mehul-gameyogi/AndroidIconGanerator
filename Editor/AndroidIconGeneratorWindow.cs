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

        private bool foldSource = true, foldConfig = true, foldLegacy = true, foldRound = true,
                     foldForeground = true, foldBackground = true, foldExport = true, foldPlayer = true;

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
            if (settings == null) settings = IconGeneratorPreferences.LoadOrCreate();
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
            DrawConfigurationSection();
            DrawExportSection();
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
            if (GUILayout.Button("Validate", EditorStyles.toolbarButton, GUILayout.Width(70))) Defer(RunValidation);
            if (GUILayout.Button("Reset Configuration", EditorStyles.toolbarButton, GUILayout.Width(130))) Defer(ResetConfiguration);
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
                    EditorGUILayout.HelpBox("Assign or import a PNG/JPG image (1024x1024 recommended, transparent background works best).", MessageType.Info);
                }
            }
            IconEditorGUI.EndSection();
        }

        // ---- Section 2 + 3: Icon configuration & image controls --------------------------------------------

        private void DrawConfigurationSection()
        {
            IconEditorGUI.Section(ref foldConfig, "2. Icon Configuration  (3. Image Controls are inside each panel)");
            if (foldConfig)
            {
                DrawLegacyPanel();
                DrawRoundPanel();
                DrawForegroundPanel();
                DrawBackgroundPanel();
            }
            IconEditorGUI.EndSection();
        }

        private void DrawLegacyPanel()
        {
            IconEditorGUI.Section(ref foldLegacy, "Legacy Icon  (48dp, mdpi to xxxhdpi)");
            if (foldLegacy)
            {
                EditorGUILayout.PropertyField(so.FindProperty("legacyEnabled"), new GUIContent("Generate Legacy Icon"));
                using (new EditorGUI.DisabledScope(!settings.legacyEnabled))
                {
                    DrawPlacement(so.FindProperty("legacyPlacement"), 1f, FitMode.Fit);
                    EditorGUILayout.PropertyField(so.FindProperty("legacyUseBackground"), new GUIContent("Background Color"));
                    if (settings.legacyUseBackground)
                        EditorGUILayout.PropertyField(so.FindProperty("legacyBackgroundColor"), new GUIContent("Color"));
                }
            }
            IconEditorGUI.EndSection();
        }

        private void DrawRoundPanel()
        {
            IconEditorGUI.Section(ref foldRound, "Round Icon  (circular crop)");
            if (foldRound)
            {
                EditorGUILayout.PropertyField(so.FindProperty("roundEnabled"), new GUIContent("Generate Round Icon"));
                using (new EditorGUI.DisabledScope(!settings.roundEnabled))
                {
                    DrawPlacement(so.FindProperty("roundPlacement"), 0.8f, FitMode.Fit);
                    EditorGUILayout.PropertyField(so.FindProperty("roundUseBackground"), new GUIContent("Background Color"));
                    if (settings.roundUseBackground)
                        EditorGUILayout.PropertyField(so.FindProperty("roundBackgroundColor"), new GUIContent("Color"));
                }
            }
            IconEditorGUI.EndSection();
        }

        private void DrawForegroundPanel()
        {
            IconEditorGUI.Section(ref foldForeground, "Adaptive Foreground  (108dp layer)");
            if (foldForeground)
            {
                EditorGUILayout.PropertyField(so.FindProperty("adaptiveForegroundEnabled"), new GUIContent("Generate Adaptive Foreground"));
                using (new EditorGUI.DisabledScope(!settings.adaptiveForegroundEnabled))
                {
                    EditorGUILayout.PropertyField(so.FindProperty("foregroundImage"),
                        new GUIContent("Foreground Image", "Optional. The source image is used when this is empty."));
                    DrawPlacement(so.FindProperty("foregroundPlacement"), 1f, FitMode.Fit);
                    EditorGUILayout.HelpBox("Scale 1.0 fits the artwork into the 66dp safe zone. The exported layer is never cropped to a mask.", MessageType.None);
                }
            }
            IconEditorGUI.EndSection();
        }

        private void DrawBackgroundPanel()
        {
            IconEditorGUI.Section(ref foldBackground, "Adaptive Background  (108dp layer)");
            if (foldBackground)
            {
                EditorGUILayout.PropertyField(so.FindProperty("adaptiveBackgroundEnabled"), new GUIContent("Generate Adaptive Background"));
                using (new EditorGUI.DisabledScope(!settings.adaptiveBackgroundEnabled))
                {
                    EditorGUILayout.PropertyField(so.FindProperty("backgroundMode"), new GUIContent("Mode"));
                    switch (settings.backgroundMode)
                    {
                        case BackgroundMode.Solid:
                            EditorGUILayout.PropertyField(so.FindProperty("backgroundColor"), new GUIContent("Color"));
                            break;
                        case BackgroundMode.Gradient:
                            EditorGUILayout.PropertyField(so.FindProperty("gradientColorA"), new GUIContent("Start Color"));
                            EditorGUILayout.PropertyField(so.FindProperty("gradientColorB"), new GUIContent("End Color"));
                            EditorGUILayout.Slider(so.FindProperty("gradientAngle"), 0f, 360f, new GUIContent("Angle"));
                            break;
                        default:
                            EditorGUILayout.PropertyField(so.FindProperty("backgroundImage"), new GUIContent("Background Image"));
                            EditorGUILayout.PropertyField(so.FindProperty("backgroundColor"), new GUIContent("Fallback Color"));
                            DrawPlacement(so.FindProperty("backgroundPlacement"), 1f, FitMode.Fill);
                            break;
                    }
                }
            }
            IconEditorGUI.EndSection();
        }

        private static void DrawPlacement(SerializedProperty placement, float defaultScale, FitMode defaultFit)
        {
            SerializedProperty scale = placement.FindPropertyRelative("scale");
            SerializedProperty offsetX = placement.FindPropertyRelative("offsetX");
            SerializedProperty offsetY = placement.FindPropertyRelative("offsetY");
            SerializedProperty fit = placement.FindPropertyRelative("fit");

            EditorGUILayout.LabelField("Image Controls", EditorStyles.miniBoldLabel);
            EditorGUILayout.Slider(scale, 0.1f, 3f, new GUIContent("Scale"));
            EditorGUILayout.Slider(offsetX, -0.5f, 0.5f, new GUIContent("Horizontal Position"));
            EditorGUILayout.Slider(offsetY, -0.5f, 0.5f, new GUIContent("Vertical Position"));
            EditorGUILayout.PropertyField(fit, new GUIContent("Crop Mode", "Fit keeps the whole image visible. Fill crops the image so it covers the area."));
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Reset Image Controls", GUILayout.Width(150)))
            {
                scale.floatValue = defaultScale; offsetX.floatValue = 0f; offsetY.floatValue = 0f;
                fit.enumValueIndex = (int)defaultFit;
            }
            EditorGUILayout.EndHorizontal();
        }

        // ---- Section 5: Export ---------------------------------------------------------------------------

        private void DrawExportSection()
        {
            IconEditorGUI.Section(ref foldExport, "5. Export Settings");
            if (foldExport)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(so.FindProperty("outputDirectory"), new GUIContent("Output Directory"));
                if (GUILayout.Button("Browse...", GUILayout.Width(70))) Defer(BrowseOutputDirectory);
                if (GUILayout.Button("Reveal", GUILayout.Width(60))) Defer(RevealOutputDirectory);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.PropertyField(so.FindProperty("includeLdpi"), new GUIContent("Include LDPI", "Also generates the 36px / 81px ldpi sizes that Unity exposes in Player Settings."));

                string selected = string.Join(", ", AndroidIconExporter.SelectedTypes(settings).Select(IconSpecs.DisplayName).ToArray());
                EditorGUILayout.LabelField("Selected types", string.IsNullOrEmpty(selected) ? "(none)" : selected, EditorStyles.miniLabel);

                using (new EditorGUI.DisabledScope(settings.sourceImage == null && !(settings.adaptiveBackgroundEnabled && !settings.adaptiveForegroundEnabled)))
                {
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("Generate All Icons", GUILayout.Height(28))) Defer(() => Generate(true));
                    if (GUILayout.Button("Generate Selected Icons", GUILayout.Height(28))) Defer(() => Generate(false));
                    EditorGUILayout.EndHorizontal();
                }

                if (lastExport != null)
                {
                    MessageType type = lastExport.Failed > 0 ? MessageType.Error : lastExport.Cancelled ? MessageType.Warning : MessageType.Info;
                    EditorGUILayout.HelpBox(
                        $"Last generation: {lastExport.Written} written, {lastExport.Skipped} skipped, {lastExport.Failed} failed.",
                        type);
                }
            }
            IconEditorGUI.EndSection();
        }

        // ---- Section 6: Player settings ------------------------------------------------------------------

        private void DrawPlayerSettingsSection()
        {
            IconEditorGUI.Section(ref foldPlayer, "6. Unity Player Settings (Android)");
            if (foldPlayer)
            {
                string unsupported = AndroidPlayerSettingsManager.UnsupportedReason();
                if (unsupported != null) EditorGUILayout.HelpBox(unsupported, MessageType.Warning);

                EditorGUILayout.PropertyField(so.FindProperty("assignLegacy"), new GUIContent("Assign Legacy Icons"));
                EditorGUILayout.PropertyField(so.FindProperty("assignRound"), new GUIContent("Assign Round Icons"));
                EditorGUILayout.PropertyField(so.FindProperty("assignAdaptive"), new GUIContent("Assign Adaptive Icons"));
                EditorGUILayout.PropertyField(so.FindProperty("assignAfterGenerate"),
                    new GUIContent("Automatically assign after generating", "You are still asked to confirm before Player Settings are changed."));

                EditorGUILayout.BeginHorizontal();
                using (new EditorGUI.DisabledScope(unsupported != null))
                {
                    if (GUILayout.Button("Assign To Player Settings", GUILayout.Height(24))) Defer(() => AssignToPlayerSettings(true));
                    if (GUILayout.Button("Validate Current Assignments", GUILayout.Height(24))) Defer(ValidateAssignments);
                }
                EditorGUILayout.EndHorizontal();
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

        // ---- Section 4: Live preview -----------------------------------------------------------------------

        private void DrawPreviewPane()
        {
            GUILayout.Label("4. Live Preview", EditorStyles.boldLabel);
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

        private void RunValidation()
        {
            ReloadSources(true);
            report = IconValidator.ValidateSettings(settings, sourceSet);
            report.LogToConsole(false);
            IconLogger.Log(report.HasErrors ? Severity.Error : report.HasWarnings ? Severity.Warning : Severity.Success,
                $"Validation finished: {report.Count(Severity.Error)} errors, {report.Count(Severity.Warning)} warnings.");
        }

        private void ResetConfiguration()
        {
            if (!EditorUtility.DisplayDialog("Reset configuration",
                    "Reset all icon settings to their defaults? Assigned images and the output folder are kept.", "Reset", "Cancel"))
                return;
            Undo.RecordObject(settings, "Reset Android Icon Settings");
            settings.ResetConfiguration();
            so.Update();
            OnSettingsChanged();
        }

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

        private void BrowseOutputDirectory()
        {
            string start = Directory.Exists(IconFolderManager.ToAbsolute(settings.outputDirectory))
                ? IconFolderManager.ToAbsolute(settings.outputDirectory) : Application.dataPath;
            string chosen = EditorUtility.OpenFolderPanel("Choose output directory", start, string.Empty);
            if (string.IsNullOrEmpty(chosen)) return;

            Undo.RecordObject(settings, "Change Output Directory");
            settings.outputDirectory = IconFolderManager.NormalizeOutputDirectory(chosen);
            so.Update();
            IconGeneratorPreferences.Save(settings);
        }

        private void RevealOutputDirectory()
        {
            string abs = IconFolderManager.ToAbsolute(IconFolderManager.NormalizeOutputDirectory(settings.outputDirectory));
            if (Directory.Exists(abs)) EditorUtility.RevealInFinder(abs);
            else EditorUtility.DisplayDialog("Folder not found", "The output directory does not exist yet. Generate the icons first.", "OK");
        }

        private void Generate(bool all)
        {
            ReloadSources(true);
            report = IconValidator.ValidateSettings(settings, sourceSet);
            if (report.HasErrors)
            {
                report.LogToConsole(false);
                IconLogger.Error("Generation stopped: fix the errors shown in the window first.");
                return;
            }

            var types = all ? AndroidIconExporter.AllTypes() : AndroidIconExporter.SelectedTypes(settings);
            var plan = AndroidIconExporter.BuildPlan(settings, types);
            if (plan.Count == 0)
            {
                report.Warning("Export", "No icon types are selected.", "Enable an icon type or use 'Generate All Icons'.");
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

            if (lastExport.Success && settings.assignAfterGenerate) AssignToPlayerSettings(false);
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

        private void ValidateAssignments()
        {
            report = PlayerSettingsValidator.Validate(settings);
            report.LogToConsole();
        }
    }
}
