using System;
using UnityEditor;
using UnityEngine;

namespace AndroidIconGenerator
{
    /// <summary>Persists <see cref="AndroidIconSettings"/> per project in EditorPrefs.</summary>
    public static class IconGeneratorPreferences
    {
        [Serializable]
        private class PersistedState
        {
            public string json;
            public string sourceGuid;
            public string foregroundGuid;
            public string backgroundGuid;
        }

        private static string Key => "AndroidIconGenerator.Settings." + Application.dataPath.GetHashCode().ToString("X");

        public static AndroidIconSettings LoadOrCreate()
        {
            var settings = AndroidIconSettings.CreateDefault();
            try
            {
                string raw = EditorPrefs.GetString(Key, string.Empty);
                if (string.IsNullOrEmpty(raw)) return settings;

                var state = JsonUtility.FromJson<PersistedState>(raw);
                if (state == null || string.IsNullOrEmpty(state.json)) return settings;

                JsonUtility.FromJsonOverwrite(state.json, settings);
                settings.sourceImage = LoadByGuid(state.sourceGuid);
                settings.foregroundImage = LoadByGuid(state.foregroundGuid);
                settings.backgroundImage = LoadByGuid(state.backgroundGuid);
                if (string.IsNullOrWhiteSpace(settings.outputDirectory))
                    settings.outputDirectory = AndroidIconSettings.DefaultOutputDirectory;
            }
            catch (Exception e)
            {
                IconLogger.Warning("Could not restore saved settings, using defaults. " + e.Message);
            }
            return settings;
        }

        public static void Save(AndroidIconSettings settings)
        {
            if (settings == null) return;
            try
            {
                var state = new PersistedState
                {
                    json = JsonUtility.ToJson(settings),
                    sourceGuid = ToGuid(settings.sourceImage),
                    foregroundGuid = ToGuid(settings.foregroundImage),
                    backgroundGuid = ToGuid(settings.backgroundImage),
                };
                EditorPrefs.SetString(Key, JsonUtility.ToJson(state));
            }
            catch (Exception e)
            {
                IconLogger.Warning("Could not save settings: " + e.Message);
            }
        }

        private static string ToGuid(UnityEngine.Object o)
        {
            if (o == null) return string.Empty;
            string path = AssetDatabase.GetAssetPath(o);
            return string.IsNullOrEmpty(path) ? string.Empty : AssetDatabase.AssetPathToGUID(path);
        }

        private static Texture2D LoadByGuid(string guid)
        {
            if (string.IsNullOrEmpty(guid)) return null;
            string path = AssetDatabase.GUIDToAssetPath(guid);
            return string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
