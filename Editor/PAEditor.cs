using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace NoZ.PA
{
    /// <summary>
    /// PixelArt editor window
    /// </summary>
    internal class PAEditor : EditorWindow
    {
        private PixelArt _previousTarget = null;

        public PAWorkspace Workspace { get; private set; }
        public Toolbar Toolbar { get; private set; }

        [MenuItem("Window/2D/PixelArt Editor")]
        public static void OpenWindow ()
        {
            GetWindow<PAEditor>();
        }

        [UnityEditor.Callbacks.OnOpenAsset(1)]
        public static bool OnOpenAsset(int instanceID, int line)
        {
            if (Selection.activeObject as PixelArt)
            {
                GetWindow<PAEditor>().OpenFile((PixelArt)Selection.activeObject);
                return true;
            }

            return false;
        }

        private void OnGUI()
        {
            Workspace?.OnGUI();
        }

        public void OnEnable()
        {
            Undo.undoRedoPerformed += OnUndoRedo;

            SetTitle("PixelArt Editor");

            // Add style sheet
            rootVisualElement.AddStyleSheetPath("PAEditor");
            rootVisualElement.name = "Editor";
            rootVisualElement.focusable = true;

            rootVisualElement.Add(CreateToolbar());

            Workspace = new PAWorkspace (this) { name = "Workspace" };
            rootVisualElement.Add(Workspace);

            // Load the Saved preferences
            Workspace.Canvas.ForegroundColor = ColorUtility.TryParseHtmlString(EditorPrefs.GetString("com.noz.pixelart.ForegroundColor"), out var foregroundColor) ?
                foregroundColor :
                Color.white;
            Workspace.Canvas.BackgroundColor = ColorUtility.TryParseHtmlString(EditorPrefs.GetString("com.noz.pixelart.BackgroundColor"), out var backgroundColor) ?
                backgroundColor :
                Color.white;

            if (_previousTarget != null)
            {
                Workspace.OpenFile(_previousTarget);
                _previousTarget = null;
            }                
        }

        private void OnDisable()
        {
            _previousTarget = Workspace.Target;

            Workspace.CloseFile();

            Undo.undoRedoPerformed -= OnUndoRedo;

            // Save the colors
            EditorPrefs.SetString("com.noz.pixelart.ForegroundColor", $"#{ColorUtility.ToHtmlStringRGBA(Workspace.Canvas.ForegroundColor)}");
            EditorPrefs.SetString("com.noz.pixelart.BackgroundColor", $"#{ColorUtility.ToHtmlStringRGBA(Workspace.Canvas.BackgroundColor)}");
        }

        private void OpenFile(PixelArt target)
        {
            Workspace.OpenFile(target);
        }

        private void OnFocus()
        {
            if (Workspace != null)
                Workspace.Canvas.Focus();
        }

        private void Update()
        {
            // Automatically close the current file if the asset is deleted
            if (Workspace.Target == null && Workspace.Canvas.File != null)
                Workspace.CloseFile();
           
            // Handle asset renaming
            if(Workspace.Target != null && Workspace.Canvas.File != null && Workspace.Target.name != Workspace.Canvas.File.name)
            {
                Workspace.Canvas.File.name = Workspace.Target.name;
                SetTitle(Workspace.Target.name);
            }
        }

        /// <summary>
        /// Set the window title with with the pixel art icon
        /// </summary>
        private void SetTitle(string title) =>
            titleContent = new GUIContent(
                title,
                PAUtils.LoadImage("PixelArtEditor.psd"));


        private void OnUndoRedo()
        {
            Workspace?.Canvas?.RefreshImage();
        }

        /// <summary>
        /// Create the toolbar at the top of the edit view
        /// </summary>
        private Toolbar CreateToolbar()
        {
            Toolbar = new Toolbar { name = "Toolbar" };
            Toolbar.pickingMode = PickingMode.Position;
            Toolbar.AddToClassList("toolbar");

            var modeMenu = new ToolbarMenu();
            modeMenu.text = "Pixel Editor";
            modeMenu.menu.AppendAction("Pixel Editor", (a) => { }, (a) => DropdownMenuAction.Status.Checked);
            modeMenu.menu.AppendAction("Bone Editor", (a) => { }, (a) => DropdownMenuAction.Status.Normal);
            Toolbar.Add(modeMenu);


            return Toolbar;
        }

    }
}
