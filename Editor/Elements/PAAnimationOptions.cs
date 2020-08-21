using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace NoZ.PA
{
    internal class PAAnimationOptions : PopupWindowContent
    {
        private PAAnimation _animation;
        private PAWorkspace _workspace;

        public PAAnimationOptions(PAWorkspace worksapce, PAAnimation animation)
        {
            _workspace = worksapce;
            _animation = animation;
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(200, 150);
        }

        public override void OnGUI(Rect rect)
        {            
        }

        public override void OnOpen()
        {
            editorWindow.rootVisualElement.AddStyleSheetPath("PAAnimationOptions");
            editorWindow.rootVisualElement.name = "Window";

            editorWindow.rootVisualElement.Add(new Label("Name"));
            var animationName = new TextField();
            animationName.value = _animation.name;
            animationName.RegisterValueChangedCallback((e) =>
            {
                _animation.name = e.newValue;
                _workspace.RefreshAnimationList();
            });
            editorWindow.rootVisualElement.Add(animationName);

            editorWindow.rootVisualElement.Add(new Label("Frame Rate"));
            var animationFPS = new SliderInt(1, 30);
            animationFPS.value = _animation.fps;
            animationFPS.RegisterValueChangedCallback((e) => _animation.fps = e.newValue);
            editorWindow.rootVisualElement.Add(animationFPS);
        }
    }
}
