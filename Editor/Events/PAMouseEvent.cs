
using UnityEngine;
using UnityEngine.UIElements;

namespace NoZ.PA
{
    internal class PAMouseEvent
    {
        public MouseButton button;
        public Vector2Int canvasPosition;
        public Vector2 workspacePosition;
        public bool shift;
        public bool alt;
        public bool ctrl;

        public static PAMouseEvent Create<T>(PAWorkspace workspace, MouseEventBase<T> evt) where T : MouseEventBase<T>, new() => new PAMouseEvent
        {
            button = (MouseButton)evt.button,
            workspacePosition = evt.localMousePosition,
            canvasPosition = workspace.WorkspaceToCanvas(evt.localMousePosition),
            shift = evt.shiftKey,
            alt = evt.altKey,
            ctrl = evt.ctrlKey
        };
    }
}
