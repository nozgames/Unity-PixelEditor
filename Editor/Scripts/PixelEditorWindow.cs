using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace NoZ.PixelEditor
{
    public class PixelEditorWindow : EditorWindow
    {
        private const float ZoomMin = 1.0f;
        private const float ZoomMax = 50.0f;
        private const float ZoomIncrementUp = 1.1f;
        private const float ZoomIncrementDown = 1.0f / ZoomIncrementUp;

        private enum ToolType
        {
            Selection,
            Pencil,
            Eraser,
            EyeDropper
        }

        private Vector2 _lastMousePosition;
        private PixelArt _pixelArt = null;
        private ColorField _colorTool = null;
        private Layer _layer = null;
        private VisualElement _workspace = null;
        private WorkspaceCursorManager _workspaceCursor = null;
        private Slider _zoomSlider = null;
        private bool _drawing = false;
        private Color _drawColor = Color.white;
        private Texture2D _cursorDraw = null;
        private Texture2D _cursorEyeDropper = null;
        private bool _alt = false;
        private ToolType _tool = ToolType.Pencil;

        public bool IsDrawing {
            get => _drawing;
            set {
                _drawing = value;

                if (!_drawing && MouseCaptureController.HasMouseCapture(_workspace))
                    MouseCaptureController.ReleaseMouse();
            }
        }

        private ToolType Tool {
            get => _tool;
            set {
                _tool = value;
                UpdateCursor(_lastMousePosition, false);
            }
        }

        [UnityEditor.Callbacks.OnOpenAsset(1)]
        public static bool OnOpenAsset(int instanceID, int line)
        {
            if (Selection.activeObject as PixelArt)
            {
                // Shows an instance of custom window, RBSettings
                // Loads an icon from an image stored at the specified path
                Texture icon = AssetDatabase.LoadAssetAtPath<Texture>("Assets/PixelEditor/Editor/Icons/PixelArtEditor.psd");
                GUIContent titleContent = new GUIContent("Pixel Editor", icon);
                //window.titleContent = titleContent;

                // TODO: if window was already open save the document and open the new one
                var window = EditorWindow.GetWindow<PixelEditorWindow>();
                window.titleContent = titleContent;
                window.Load((PixelArt)Selection.activeObject);

                //var editor = EditorWindow.CreateWindow<PixelEditor>();
                //OpenYourScriptableObjectEditorWindow();
                return true;
            }

            return false; // let unity open the file
        }

        public void OnEnable()
        {
            // Each editor window contains a root VisualElement object
            VisualElement root = rootVisualElement;

            _cursorDraw = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/PixelEditor/Editor/Cursors/Pencil.psd");
            _cursorEyeDropper = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/PixelEditor/Editor/Cursors/EyeDropper.psd");

            // Import UXML
            //var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/PE/Editor/PixelEditor.uxml");
            //visualTree.CloneTree(root);

            // A stylesheet can be added to a VisualElement.
            // The style will be applied to the VisualElement and all of its children.
            root.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/PixelEditor/Editor/Scripts/PixelEditor.uss"));

            var toolbar = new Toolbar();
            toolbar.AddToClassList("toolbar");

            var modeMenu = new ToolbarMenu();
            modeMenu.text = "Pixel Editor";
            modeMenu.menu.AppendAction("Pixel Editor", (a) => { }, (a) => DropdownMenuAction.Status.Checked);
            modeMenu.menu.AppendAction("Bone Editor", (a) => { }, (a) => DropdownMenuAction.Status.Normal);
            toolbar.Add(modeMenu);

            _zoomSlider = new Slider();
            _zoomSlider.lowValue = ZoomMin;
            _zoomSlider.highValue = ZoomMax;
            _zoomSlider.AddToClassList("zoom");
            _zoomSlider.RegisterValueChangedCallback(OnZoomValueChanged);
            toolbar.Add(_zoomSlider);


            _colorTool = new ColorField();
            _colorTool.showEyeDropper = false;
            _colorTool.value = Color.white;
            toolbar.Add(_colorTool);

            var toolbarSpacer = new VisualElement();
            toolbarSpacer.style.flexGrow = 1.0f;
            toolbar.Add(toolbarSpacer);

            var saveButton = new ToolbarButton();
            saveButton.text = "Save";
            saveButton.clickable.clicked += Save;
            toolbar.Add(saveButton);
            root.Add(toolbar);

            _workspace = new VisualElement();
            _workspace.focusable = true;
            _workspace.AddToClassList("workspace");
            _workspace.RegisterCallback<MouseDownEvent>(OnWorkspaceMouseDown);
            _workspace.RegisterCallback<MouseMoveEvent>(OnWorkspaceMouseMove);
            _workspace.RegisterCallback<MouseUpEvent>(OnWorkspaceMouseUp);
            _workspace.RegisterCallback<MouseCaptureOutEvent>(OnWorkspaceMouseCaptureOut);
            _workspace.RegisterCallback<WheelEvent>(OnWorkspaceWheel);
            _workspace.RegisterCallback<MouseEnterEvent>(OnWorkspaceMouseEnter);
            root.RegisterCallback<KeyDownEvent>(OnWorkspaceKeyDown);
            _workspace.pickingMode = PickingMode.Position;
            _workspace.Focus();
            root.Add(_workspace);

            _layer = new Layer(16, 16);
            _workspace.Add(_layer);

            // Create an element to manage the workspace cursor
            _workspaceCursor = new WorkspaceCursorManager();
            _workspaceCursor.AddToClassList("workspaceCursor");
            _workspace.Add(_workspaceCursor);

#if false
        var test = EditorPrefs.GetString("Scene/Background").Split(';');
        var bkcolor = new Color(float.Parse(test[1]), float.Parse(test[2]), float.Parse(test[3]), 1.0f);
        _workspace.style.backgroundColor = bkcolor;
        _workspace.pickingMode = PickingMode.Position;
        var selectionTool = root.Query<Button>("selectionTool").First();
#endif

            Undo.undoRedoPerformed += OnUndoRedo;
        }

        private void OnWorkspaceKeyDown(KeyDownEvent evt)
        {
            switch (evt.keyCode)
            {
                case KeyCode.I:
                    Tool = ToolType.EyeDropper;
                    evt.StopImmediatePropagation();
                    break;

                case KeyCode.B:
                    Tool = ToolType.Pencil;
                    evt.StopImmediatePropagation();
                    break;
            }

            UpdateCursor(_lastMousePosition);
        }

        private void OnWorkspaceMouseEnter(MouseEnterEvent evt)
        {
            UpdateCursor(evt.localMousePosition, true);
        }

        public void Load(PixelArt pa)
        {
            _pixelArt = pa;
            if (null == _pixelArt)
                return;

            _layer._texture = pa.texture;
            _layer.MarkDirtyRepaint();
            _workspace.MarkDirtyRepaint();
        }

        private void Save()
        {
            if (null == _pixelArt)
                return;

            using (var writer = new BinaryWriter(File.Create(AssetDatabase.GetAssetPath(_pixelArt))))
            {
                writer.Write(_layer._texture.width);
                writer.Write(_layer._texture.height);
                writer.Write(_layer._texture.GetRawTextureData());
            }
            AssetDatabase.Refresh();
        }

        private void OnWorkspaceMouseCaptureOut(MouseCaptureOutEvent evt)
        {
            IsDrawing = false;
        }

        private void OnWorkspaceMouseUp(MouseUpEvent evt)
        {
            IsDrawing = false;
        }

        private void UpdateCursor(Vector2 localPosition, bool reset = false)
        {
            if (reset)
                _workspaceCursor.Reset();

            if (_tool == ToolType.EyeDropper)
                _workspaceCursor.SetCursor(_cursorEyeDropper, new Vector2(0, 31));
            else if (IsDrawing || _layer.IsMouseOver(localPosition))
                _workspaceCursor.SetCursor(_cursorDraw, new Vector2(0, 31));
            else
                _workspaceCursor.Cursor = MouseCursor.Arrow;
        }

        private void OnWorkspaceMouseMove(MouseMoveEvent evt)
        {
            _lastMousePosition = evt.localMousePosition;
            _alt = evt.altKey;

            UpdateCursor(evt.localMousePosition);

            if (IsDrawing)
                _layer.Draw(evt.localMousePosition, _drawColor);
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
        }

        private void OnUndoRedo()
        {
            _layer.MarkDirtyRepaint();
        }

        private void OnZoomValueChanged(ChangeEvent<float> evt)
        {
            _layer.Zoom = evt.newValue;
            UpdateCursor(_lastMousePosition);
        }

        private void OnWorkspaceWheel(WheelEvent evt)
        {
            var zoom = _layer.Zoom;
            if (evt.delta.y < 0)
                zoom *= ZoomIncrementUp;
            else
                zoom *= ZoomIncrementDown;

            _zoomSlider.value = Mathf.Clamp(zoom, ZoomMin, ZoomMax);
        }

        private void OnWorkspaceMouseDown(MouseDownEvent evt)
        {
            // Eye dropper tool
            if (Tool == ToolType.EyeDropper)
            {
                _colorTool.value = _layer.GetColor(evt.localMousePosition, _colorTool.value);
                return;
            }

            Undo.RegisterCompleteObjectUndo(_layer._texture, "Paint");

            IsDrawing = true;

            UpdateCursor(evt.localMousePosition);

            if (evt.button == 0)
                _drawColor = _colorTool.value;
            else
                _drawColor = Color.clear;

            _layer.Draw(evt.localMousePosition, _drawColor);

            MouseCaptureController.CaptureMouse(_workspace);
        }
    }
}
