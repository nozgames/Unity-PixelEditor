
using UnityEngine;
using UnityEngine.UIElements;

namespace NoZ.PA
{
    internal class PAMouseEvent
    {
        public MouseButton button;
        public Vector2Int imagePosition;
        public Vector2 canvasPosition;
        public bool shift;
        public bool alt;
        public bool ctrl;

        public static PAMouseEvent Create<T>(PACanvas canvas, MouseEventBase<T> evt) where T : MouseEventBase<T>, new() => new PAMouseEvent
        {
            button = (MouseButton)evt.button,
            canvasPosition = evt.localMousePosition,
            imagePosition = canvas.CanvasToImage(evt.localMousePosition),
            shift = evt.shiftKey,
            alt = evt.altKey,
            ctrl = evt.ctrlKey
        };
    }
}
