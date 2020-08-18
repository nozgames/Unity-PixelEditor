using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace NoZ.PixelEditor
{
    internal static class PEUtils
    {
        /// <summary>
        /// Load a texture from a filename or a built-in editor icon
        /// </summary>
        public static Texture LoadTexture(string name) =>
            name.StartsWith("!") ?
                EditorGUIUtility.IconContent(name.Substring(1)).image :
                AssetDatabase.LoadAssetAtPath<Texture>(name);

        /// <summary>
        /// Create a button that is made up of a single image with no border
        /// </summary>
        public static VisualElement CreateImageButton(string image, string tooltip, System.Action clicked)
        {
            var button = new Image();
            button.AddToClassList("imageButton");
            button.image = LoadTexture(image);

            button.RegisterCallback<ClickEvent>((e) => clicked());
            button.tooltip = tooltip;
            return button;
        }

        /// <summary>
        /// Create a toggle that is made up of a single image with no border
        /// </summary>
        public static VisualElement CreateImageToggle (string image, string tooltip, System.Action<bool> changeCallback, string uncheckedImage=null, string checkedClass=null, bool initialValue = true)
        {
            var imageChecked = LoadTexture(image);
            var imageUnchecked = uncheckedImage != null ? LoadTexture(uncheckedImage) : null;

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
    }
}
