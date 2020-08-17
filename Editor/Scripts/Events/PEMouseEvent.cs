
using UnityEngine;
using UnityEngine.UIElements;

namespace NoZ.PixelEditor
{
    internal class PEMouseEvent
    {
        public MouseButton button;
        public Vector2Int canvasPosition;
        public Vector2 workspacePosition;
        public bool shift;
        public bool alt;
        public bool ctrl;

        public static PEMouseEvent Create<T>(PEWindow window, MouseEventBase<T> evt) where T : MouseEventBase<T>, new() => new PEMouseEvent
        {
            button = (MouseButton)evt.button,
            workspacePosition = evt.localMousePosition,
            canvasPosition = window.WorkspaceToCanvas(evt.localMousePosition),
            shift = evt.shiftKey,
            alt = evt.altKey,
            ctrl = evt.ctrlKey
        };
    }
}
