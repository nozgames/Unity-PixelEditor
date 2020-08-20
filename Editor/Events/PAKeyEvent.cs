using UnityEngine;
using UnityEngine.UIElements;

namespace NoZ.PA
{
    internal class PAKeyEvent
    {
        public KeyCode keyCode;
        public bool shift;
        public bool alt;
        public bool ctrl;

        public static PAKeyEvent Create(KeyDownEvent evt) =>
            new PAKeyEvent
            {
                keyCode = evt.keyCode,
                ctrl = evt.ctrlKey,
                shift = evt.shiftKey,
                alt = evt.altKey
            };
    }
}
