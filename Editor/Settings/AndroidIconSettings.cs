using System;
using UnityEngine;

namespace AndroidIconGenerator
{
    public enum FitMode { Fit, Fill }
    public enum BackgroundMode { Solid, Gradient, Image }
    public enum MaskShape { Circle, Squircle, RoundedSquare, Square }
    public enum LayerTab { Foreground, Background, Options }

    /// <summary>Scale / position / crop of an image inside its layer.</summary>
    [Serializable]
    public class ImagePlacement
    {
        public float scale = 1f;
        public float offsetX;   // fraction of canvas, +right
        public float offsetY;   // fraction of canvas, +up
        public FitMode fit = FitMode.Fit;

        public ImagePlacement() { }
        public ImagePlacement(float scale) { this.scale = scale; }

        public void Reset(float defaultScale)
        {
            scale = defaultScale; offsetX = 0f; offsetY = 0f; fit = FitMode.Fit;
        }
    }

    /// <summary>
    /// All user-facing configuration. Lives only in memory (HideAndDontSave) and is persisted
    /// per project through <see cref="IconGeneratorPreferences"/>. Being a ScriptableObject lets
    /// the window use SerializedObject for automatic Undo/Redo.
    /// </summary>
    public class AndroidIconSettings : ScriptableObject
    {
        public const string DefaultOutputDirectory = "Assets/GeneratedAndroidIcons";

        // Images (persisted by GUID)
        public Texture2D sourceImage;
        public Texture2D foregroundImage;          // optional, falls back to source image
        public Texture2D backgroundImage;          // used when backgroundMode == Image

        // Enable switches
        public bool legacyEnabled = true;
        public bool roundEnabled = true;
        public bool adaptiveForegroundEnabled = true;
        public bool adaptiveBackgroundEnabled = true;

        // Legacy
        public ImagePlacement legacyPlacement = new ImagePlacement(1f);
        public bool legacyUseBackground;
        public Color legacyBackgroundColor = Color.white;

        // Round
        public ImagePlacement roundPlacement = new ImagePlacement(0.8f);
        public bool roundUseBackground = true;
        public Color roundBackgroundColor = Color.white;

        // Adaptive foreground (scale 1 == fits the 66dp safe zone)
        public ImagePlacement foregroundPlacement = new ImagePlacement(1f);
        public bool foregroundTrim;                // crop transparent borders before scaling

        // Adaptive background
        public BackgroundMode backgroundMode = BackgroundMode.Solid;
        public Color backgroundColor = new Color(0.17f, 0.24f, 0.31f, 1f);
        public Color gradientColorA = new Color(0.20f, 0.45f, 0.95f, 1f);
        public Color gradientColorB = new Color(0.55f, 0.20f, 0.85f, 1f);
        public float gradientAngle = 270f;
        public ImagePlacement backgroundPlacement = new ImagePlacement(1f) { fit = FitMode.Fill };

        // Export
        public bool createOutputFolder = true;
        public string saveFolderDirectory = DefaultOutputDirectory;
        public string outputDirectory = DefaultOutputDirectory;
        public bool includeLdpi = true;

        // Player settings
        public bool assignLegacy = true;
        public bool assignRound = true;
        public bool assignAdaptive = true;
        public bool assignAfterGenerate;

        // Preview-only UI state
        public bool showSafeZone = true;
        public bool showGrid;
        public AndroidDensity previewDensity = AndroidDensity.Xhdpi;
        public LayerTab activeTab = LayerTab.Foreground;
        public MaskShape launcherMask = MaskShape.Circle;

        public static AndroidIconSettings CreateDefault()
        {
            var s = CreateInstance<AndroidIconSettings>();
            s.hideFlags = HideFlags.HideAndDontSave;
            s.name = "AndroidIconSettings";
            return s;
        }

        /// <summary>Resets look and feel settings. Assigned images and the output folder are kept.</summary>
        public void ResetConfiguration()
        {
            var defaults = CreateDefault();
            var src = sourceImage; var fg = foregroundImage; var bg = backgroundImage; var dir = outputDirectory;
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(defaults), this);
            sourceImage = src; foregroundImage = fg; backgroundImage = bg; outputDirectory = dir;
            DestroyImmediate(defaults);
        }

        public bool IsTypeEnabled(IconType type)
        {
            switch (type)
            {
                case IconType.Legacy: return legacyEnabled;
                case IconType.Round: return roundEnabled;
                case IconType.AdaptiveForeground: return adaptiveForegroundEnabled;
                default: return adaptiveBackgroundEnabled;
            }
        }
    }
}
