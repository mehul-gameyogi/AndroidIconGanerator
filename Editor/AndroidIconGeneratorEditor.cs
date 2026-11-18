using UnityEditor;
using UnityEngine;

namespace AndroidIconGenerator
{
    /// <summary>Project window integration: right-click a texture to send it to the Android Icon Generator.</summary>
    public static class AndroidIconGeneratorEditor
    {
        private const string MenuPath = "Assets/Android Icon Generator/Use As Source Image";

        [MenuItem(MenuPath, true)]
        private static bool ValidateUseAsSource()
        {
            return Selection.activeObject is Texture2D;
        }

        [MenuItem(MenuPath, false, 2000)]
        private static void UseAsSource()
        {
            if (Selection.activeObject is Texture2D texture)
                AndroidIconGeneratorWindow.OpenWithSource(texture);
        }
    }
}
