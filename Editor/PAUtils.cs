using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace NoZ.PA
{
    internal static class PAUtils
    {
        /// <summary>
        /// Load a image texture from a filename or a built-in editor icon
        /// </summary>
        public static Texture2D LoadImage(string name) =>
            name.StartsWith("!") ?
                (Texture2D)EditorGUIUtility.IconContent(name.Substring(1)).image :
                AssetDatabase.LoadAssetAtPath<Texture2D>($"{PixelArtPackageInfo.assetPackagePath}/Editor Default Resources/images/{name}");

        /// <summary>
        /// Load a cursor texture from a filename or a built-in editor icon
        /// </summary>
        public static Texture2D LoadCursor(string name) =>
            AssetDatabase.LoadAssetAtPath<Texture2D>($"{PixelArtPackageInfo.assetPackagePath}/Editor Default Resources/cursors/{name}");

        /// <summary>
        /// Load a style seet from the package path
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static StyleSheet LoadStyleSheet(string name) =>
            AssetDatabase.LoadAssetAtPath<StyleSheet>(
                $"{PixelArtPackageInfo.assetPackagePath}/Editor Default Resources/uss/{name}.uss");

        /// <summary>
        /// Create a button that is made up of a single image with no border
        /// </summary>
        public static VisualElement CreateImageButton(string image, string tooltip, System.Action clicked)
        {
            var button = new Image();
            button.AddToClassList("imageButton");
            button.image = LoadImage(image);

            button.RegisterCallback<ClickEvent>((e) => clicked());
            button.tooltip = tooltip;
            return button;
        }

        /// <summary>
        /// Create a toggle that is made up of a single image with no border
        /// </summary>
        public static VisualElement CreateImageToggle (string image, string tooltip, System.Action<bool> changeCallback, string uncheckedImage=null, string checkedClass=null, bool initialValue = true)
        {
            var imageChecked = LoadImage(image);
            var imageUnchecked = uncheckedImage != null ? LoadImage(uncheckedImage) : null;

            var toggle = new Image();
            toggle.AddToClassList("toggleImage");
            toggle.tooltip = tooltip;
            toggle.image = initialValue ? imageChecked : imageUnchecked;

            checkedClass = checkedClass ?? "checked";

            if(initialValue)
                toggle.AddToClassList(checkedClass);

            toggle.RegisterCallback<ClickEvent>((e) => {
                e.StopImmediatePropagation();
                toggle.ToggleInClassList(checkedClass);

                var ischecked = toggle.ClassListContains(checkedClass);
                changeCallback?.Invoke(ischecked);

                if (imageUnchecked != null)
                {
                    toggle.image = ischecked ? imageChecked : imageUnchecked;
                    toggle.MarkDirtyRepaint();
                }
            });

            return toggle;
        }

        public static string GetSelectedPathOrFallback()
        {
            string path = "Assets";

            foreach (UnityEngine.Object obj in Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.Assets))
            {
                path = AssetDatabase.GetAssetPath(obj);
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    path = Path.GetDirectoryName(path);
                    break;
                }
            }
            return path;
        }
    }
}
