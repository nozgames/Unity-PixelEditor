using UnityEngine;
using UnityEngine.UIElements;

namespace NoZ.PixelEditor
{
    internal class PEDrawEvent : PEMouseEvent
    {
        /// <summary>
        /// Start position of the drawing in workspace coordinates
        /// </summary>
        public Vector2 start;


        public static PEDrawEvent Create<T>(PEWindow window, MouseEventBase<T> evt, MouseButton button, Vector2 start) where T : MouseEventBase<T>, new() => new PEDrawEvent 
        {
            button = (MouseButton)button,
            workspacePosition = evt.localMousePosition,
            canvasPosition = window.WorkspaceToCanvas(evt.localMousePosition),
            start = start,
            shift = evt.shiftKey,
            alt = evt.altKey,
            ctrl = evt.ctrlKey
        };
    }
}
