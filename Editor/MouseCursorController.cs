
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace NoZ.PixelEditor
{
    class MouseCursorController : ImmediateModeElement
    {
        private MouseCursor _cursor = MouseCursor.Arrow;
        private Texture2D _custom = null;

        public MouseCursor Cursor {
            get => _cursor;
            set {
                if (value == _cursor)
                    return;

                _cursor = value;
                MarkDirtyRepaint();
            }
        }

        public void Reset()
        {
            _custom = null;
            _cursor = MouseCursor.Arrow;
            MarkDirtyRepaint();
        }

        public void SetCustomCursor (Texture2D texture, Vector2 hotspot)
        {
            if (_custom == texture && Cursor == MouseCursor.CustomCursor)
                return;

            UnityEngine.Cursor.SetCursor(texture, hotspot, CursorMode.Auto);
            _custom = texture;
            Cursor = MouseCursor.CustomCursor;
        }

        protected override void ImmediateRepaint()
        {
            EditorGUIUtility.AddCursorRect(contentRect, Cursor);
        }
    }
}
