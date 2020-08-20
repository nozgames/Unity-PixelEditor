using UnityEngine;
using UnityEngine.UIElements;

namespace NoZ.PA
{
    internal class PADrawEvent : PAMouseEvent
    {
        /// <summary>
        /// Start position of the drawing in workspace coordinates
        /// </summary>
        public Vector2 start;


        public static PADrawEvent Create<T>(PAWorkspace workspace, MouseEventBase<T> evt, MouseButton button, Vector2 start) where T : MouseEventBase<T>, new() => new PADrawEvent 
        {
            button = (MouseButton)button,
            canvasPosition = evt.localMousePosition,
            imagePosition = workspace.CanvasToImage(evt.localMousePosition),
            start = start,
            shift = evt.shiftKey,
            alt = evt.altKey,
            ctrl = evt.ctrlKey
        };
    }
}
