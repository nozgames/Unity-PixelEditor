using UnityEngine;

namespace NoZ.PA
{
    public class PixelArt : ScriptableObject
    {
        [SerializeField] private int _defaultAnimationIndex = 0;

        // TODO: animations
        [SerializeField] private PixelArtAnimation[] _animations = null;

        public int animationCount => _animations.Length;

        public int defaultAnimationIndex => _defaultAnimationIndex;

        public PixelArtAnimation defaultAnimation => GetAnimationAt(_defaultAnimationIndex);

        public PixelArtAnimation GetAnimationAt(int index) => _animations[index];

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

        /// <summary>
        /// Create a pixel art from the given animations
        /// </summary>
        public static PixelArt Create(PixelArtAnimation[] animations, int defaultAnimationIndex=0)
        {
            var pa = CreateInstance<PixelArt>();
            pa._animations = animations;
            pa._defaultAnimationIndex = Mathf.Clamp(defaultAnimationIndex, 0, animations.Length - 1);
            return pa;
        }
    }
}
