using System;
using UnityEngine;

namespace NoZ.PA
{
    [Serializable]
    public class PixelArtAnimation
    {
        [Serializable]
        private struct Frame
        {
            public Sprite sprite;
        }

        [SerializeField] public string _name;

        [SerializeField] private Frame[] _frames = null;

        [SerializeField] private WrapMode _wrapMode = WrapMode.Clamp;
    }
}
