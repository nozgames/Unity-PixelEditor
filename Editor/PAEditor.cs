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
        private int _openFileControlId;

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
            var pixelArt = Selection.activeObject as PixelArt;
            if (null == pixelArt)
                return false;

            GetWindow<PAEditor>().OpenFile(pixelArt);

            return true;
        }

        private void OnGUI()
        {
            Workspace?.OnGUI();

            // Handle object selection
            if (Event.current.commandName == "ObjectSelectorUpdated")
            {
                if(EditorGUIUtility.GetObjectPickerControlID() == _openFileControlId)
                {
                    var target = EditorGUIUtility.GetObjectPickerObject() as PixelArt;
                    if (target != null)
                        OpenFile(target);
                    else
                        CloseFile();
                }
                
            }
            else if (Event.current.commandName == "ObjectSelectorClosed")
                _openFileControlId = -1;
        }

        public void OnEnable()
        {
            RefreshTitle();

            // Add style sheet
            rootVisualElement.AddStyleSheetPath("PAEditor");
            rootVisualElement.name = "Editor";
            rootVisualElement.focusable = true;
            //rootVisualElement.RegisterCallback<FocusInEvent>((e) => Workspace?.schedule.Execute(Workspace.Canvas.Focus));

            rootVisualElement.Add(CreateToolbar());

            if (_previousTarget != null)
            {
                OpenFile(_previousTarget);
                _previousTarget = null;
            }

            Undo.undoRedoPerformed += OnUndoRedo;
            EditorApplication.quitting += CloseFile;
        }

        private void OnDisable()
        {
            _previousTarget = Workspace?.Target;

            CloseFile();

            Undo.undoRedoPerformed -= OnUndoRedo;
            EditorApplication.quitting -= CloseFile;
        }

        private void OnFocus()
        {
            if (Workspace != null)
                Workspace.Canvas.Focus();
        }

        private void Update()
        {
            if (null == Workspace)
                return;

            // Automatically close the current file if the asset is deleted
            if (Workspace.Target == null && Workspace.Canvas.File != null)
            {
                CloseFile();
                return;
            }

            // Handle asset renaming
            if (Workspace.Target != null && Workspace.Canvas.File != null && Workspace.Target.name != Workspace.Canvas.File.name)
            {
                Workspace.Canvas.File.name = Workspace.Target.name;
                RefreshTitle();
            }
        }

        private void OpenFile(PixelArt target)
        {
            if (Workspace?.Target == target)
                return;

            if (Workspace != null)
                CloseFile();

            Workspace = new PAWorkspace(this) { name = "Workspace" };
            rootVisualElement.Add(Workspace);

            // Load the Saved preferences
            Workspace.Canvas.ForegroundColor = ColorUtility.TryParseHtmlString(EditorPrefs.GetString("com.noz.pixelart.ForegroundColor"), out var foregroundColor) ?
                foregroundColor :
                Color.white;
            Workspace.Canvas.BackgroundColor = ColorUtility.TryParseHtmlString(EditorPrefs.GetString("com.noz.pixelart.BackgroundColor"), out var backgroundColor) ?
                backgroundColor :
                Color.white;

            Workspace.OpenFile(target);

            RefreshTitle();
        }

        private void CloseFile()
        {
            if (Workspace == null)
                return;

            // Save the colors
            EditorPrefs.SetString("com.noz.pixelart.ForegroundColor", $"#{ColorUtility.ToHtmlStringRGBA(Workspace.Canvas.ForegroundColor)}");
            EditorPrefs.SetString("com.noz.pixelart.BackgroundColor", $"#{ColorUtility.ToHtmlStringRGBA(Workspace.Canvas.BackgroundColor)}");

            Workspace.CloseFile();
            rootVisualElement.Remove(Workspace);
            Workspace = null;
            RefreshTitle();

            rootVisualElement.Focus();
        }

        /// <summary>
        /// Set the window title with with the pixel art icon
        /// </summary>
        public void RefreshTitle () =>
            titleContent = new GUIContent(
                Workspace == null ? "PixelArt" : $"{Workspace.Target.name}{(Workspace.IsModified ? "*" : "")}",
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

            var fileMenu = new ToolbarMenu();
            fileMenu.text = "File";
            fileMenu.menu.AppendAction("New...", (a) => { }, (a) => DropdownMenuAction.Status.Normal);
            fileMenu.menu.AppendAction(
                "Open...", 
                (a) => {
                    _openFileControlId = GUIUtility.GetControlID(FocusType.Passive);
                    EditorGUIUtility.ShowObjectPicker<PixelArt>(Workspace?.Target, false, null, _openFileControlId);
                },
                (a) => DropdownMenuAction.Status.Normal);

            fileMenu.menu.AppendSeparator();
            fileMenu.menu.AppendAction("Save", (a) => Workspace?.SaveFile(), (a) => Workspace != null ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            fileMenu.menu.AppendAction("Save As...", (a) => { }, (a) => Workspace != null ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            fileMenu.menu.AppendAction("Close", (a) => CloseFile(), (a) => Workspace != null ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            Toolbar.Add(fileMenu);

            return Toolbar;
        }

    }
}
