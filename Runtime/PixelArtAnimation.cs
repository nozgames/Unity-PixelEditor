using System;
using UnityEngine;

namespace NoZ.PA
{
    [Serializable]
    public class PixelArtAnimation
    {
        [SerializeField] public string _name;

        [SerializeField] private PixelArtAnimationFrame[] _frames = null;

        [SerializeField] private WrapMode _wrapMode = WrapMode.Clamp;

        public int frameCount => _frames?.Length ?? 0;

        public PixelArtAnimationFrame GetFrameAt(int index) => _frames[index];

        public PixelArtAnimation()
        {
        }

        public PixelArtAnimation(string name, PixelArtAnimationFrame[] frames)
        {
            _name = name;
            _frames = frames;
        }
    }
}
