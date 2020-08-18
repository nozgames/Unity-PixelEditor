using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace NoZ.PixelEditor
{
    /// <summary>
    /// Custom toggle element that is just an image swap.
    /// </summary>
    internal class PEImageToggle : VisualElement
    {
        private Image _image = null;
        private Texture _checkedImage = null;
        private Texture _uncheckedImage = null;
        private bool _value = true;        

        public PEImageToggle()
        {
            pickingMode = PickingMode.Position;
            AddToClassList("noz-imageToggle");
            _image = new Image();
            Add(_image);
            RegisterCallback<ClickEvent>(OnClicked);
            AddToClassList("noz-imageToggle-checked");
        }

        private void OnClicked(ClickEvent evt)
        {
            value = !value;
            evt.StopImmediatePropagation();
        }

        public bool value {
            get => _value;
            set {
                if (_value == value)
                    return;
                _value = value;
                _image.image = (_value ? _checkedImage : _uncheckedImage) ?? _checkedImage;
                _image.MarkDirtyRepaint();

                if (_value)
                    AddToClassList("noz-imageToggle-checked");
                else
                    RemoveFromClassList("noz-imageToggle-checked");

                onValueChanged?.Invoke(_value);
            }
        }

        public Action<bool> onValueChanged { get; set; }

        public Texture checkedImage {
            get => _checkedImage;
            set {
                _checkedImage = value;
                if (this.value)
                    _image.image = _checkedImage;
            }
        }

        public Texture uncheckedImage {
            get => _uncheckedImage;
            set {
                _uncheckedImage = value;
                if (!this.value)
                    _image.image = _uncheckedImage ?? _checkedImage;
            }
        }
    }
}
