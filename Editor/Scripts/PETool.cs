using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace NoZ.PixelEditor
{
    internal class PETool : ImmediateModeElement
    {
        public PEWindow Window { get; private set; }

        public float DrawThreshold = 0.0f;

        public PETool(PEWindow window)
        {
            Window = window;

            visible = false;
            pickingMode = PickingMode.Ignore;

            // Stretch to fit the parent workspace
            style.position = new StyleEnum<Position>(Position.Absolute);
            style.left = 0;
            style.right = 0;
            style.bottom = 0;
            style.top = 0;
        }

        public bool IsDrawing => Window.IsDrawing;

        /// <summary>
        /// Load the icon for the tool
        /// </summary>
        /// <returns></returns>
        public Texture2D LoadIcon () =>
            AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/PixelEditor/Editor/Icon/{GetType().Name}.psd");

        /// <summary>
        /// Called when a mouse button is pressed with the tool active
        /// </summary>
        public virtual void OnMouseDown (MouseButton button, Vector2 workspacePosition)
        {
        }

        /// <summary>
        /// Called when a mouse button is released with the tool active
        /// </summary>
        public virtual void OnMouseUp (MouseButton button, Vector2 workspacePosition)
        {
        }

        /// <summary>
        /// Called when the mouse moves while the tool is active
        /// </summary>
        public virtual void OnMouseMove(Vector2 workspacePosition) { }

        /// <summary>
        /// Set the cursor to the appropriate cursor for the given position
        /// </summary>
        public virtual void SetCursor (Vector2Int canvasPosition)
        {
            Window.SetCursor(MouseCursor.Arrow);
        }

        public virtual void OnDrawStart (MouseButton button, Vector2Int canvasPosition)
        {
        }

        public virtual void OnDrawContinue (MouseButton button, Vector2Int canvasPosition)
        {

        }

        public virtual void OnDrawEnd (MouseButton button, Vector2Int canvasPosition)
        {
        }

        public virtual void OnDrawCancel(MouseButton button)
        {
        }

        sealed protected override void ImmediateRepaint()
        {
            GUI.BeginClip(contentRect);
            OnRepaint();
            GUI.EndClip();            
        }

        protected virtual void OnRepaint()
        {
        }
    }
}
