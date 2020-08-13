using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace NoZ.PixelEditor
{
    /// <summary>
    /// Manages the mouse cursor for the workspace.  This element is overlayed
    /// on top of the workspace and uses immediate mode to change the cursors. Note that
    /// immediate mode was used to work around some bugs with swapping cursors at
    /// runtime within an element.
    /// </summary>
    class WorkspaceCursorManager : ImmediateModeElement
    {
        /// <summary>
        /// Current cursor
        /// </summary>
        private MouseCursor _cursor = MouseCursor.Arrow;

        /// <summary>
        /// Custom cursor texture last set
        /// </summary>
        private Texture2D _texture = null;

        /// <summary>
        /// Property to wrap the current cursor and handle changing of the cursor
        /// </summary>
        public MouseCursor Cursor {
            get => _cursor;
            set {
                if (value == _cursor)
                    return;

                _cursor = value;
                if (_cursor != MouseCursor.CustomCursor)
                    _texture = null;

                MarkDirtyRepaint();
            }
        }

        /// <summary>
        /// Reset the cursor back to default state
        /// </summary>
        public void Reset()
        {
            _cursor = MouseCursor.Arrow;
            MarkDirtyRepaint();
        }

        /// <summary>
        /// Set a cursor using a texure and hotspot
        /// </summary>
        public void SetCursor (Texture2D texture, Vector2 hotspot)
        {
            // Is the cursor already set?
            if (_texture == texture && Cursor == MouseCursor.CustomCursor)
                return;

            // Set the cursor directly in unity
            UnityEngine.Cursor.SetCursor(texture, hotspot, CursorMode.Auto);

            // Save the cursor texture 
            _texture = texture;
            Cursor = MouseCursor.CustomCursor;
        }

        protected override void ImmediateRepaint()
        {
            EditorGUIUtility.AddCursorRect(contentRect, Cursor);
        }
    }
}
