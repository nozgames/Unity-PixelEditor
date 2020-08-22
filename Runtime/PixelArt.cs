using UnityEngine;

namespace NoZ.PA
{
    public class PixelArt : ScriptableObject
    {
        public int width;
        public int height;
        public Texture2D texture = null;

        // TODO: animations
        [SerializeField] private PixelArtAnimation[] _animations = null;

        /// <summary>
        /// Returns the index of the animation with the given name or -1 if the animation
        /// was not found in the animation list
        /// </summary>
        public int FindAnimation (string name)
        {
            for (int i = 0; i < _animations.Length; i++)
                if (0 == string.Compare(_animations[i]._name, name, true))
                    return i;

            return -1;
        }
    }
}
