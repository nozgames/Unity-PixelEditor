using UnityEngine;
using UnityEngine.UIElements;

namespace NoZ.PixelEditor
{
    class PEKeyEvent
    {
        public KeyCode keyCode;
        public bool shift;
        public bool alt;
        public bool ctrl;

        public static PEKeyEvent Create(KeyDownEvent evt) =>
            new PEKeyEvent
            {
                keyCode = evt.keyCode,
                ctrl = evt.ctrlKey,
                shift = evt.shiftKey,
                alt = evt.altKey
            };
    }
}
