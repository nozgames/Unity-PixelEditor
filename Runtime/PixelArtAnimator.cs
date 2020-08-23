using System.Collections;
using UnityEngine;

namespace NoZ.PA
{
    public class PixelArtAnimator : MonoBehaviour
    {
        /// <summary>
        /// Pixel art being renderer
        /// </summary>
        [SerializeField] private PixelArt _pixelArt = null;
        
        /// <summary>
        /// Mapping of sprite renderers to layers within the pixel art
        /// </summary>
        public SpriteRenderer[] layers = null;

        public bool playOnAwake = true;

        /// <summary>
        /// Pixel art being renderer
        /// </summary>
        public PixelArt PixelArt {
            get => _pixelArt;
            set {
                if (value == _pixelArt)
                    return;

                _pixelArt = value;

                // TODO: animation
            }
        }

        private void Awake()
        {
            if (playOnAwake)
                Play(1);
        }

        public void Play (string name)
        {

        }

        /// <summary>
        /// Play the animation at the given animation index.
        /// </summary>
        public void Play(int index)
        {
            StartCoroutine(PlayCoroutine(_pixelArt.GetAnimationAt(index)));
        }

        private IEnumerator PlayCoroutine(PixelArtAnimation animation)
        {
            var frameIndex = 0;
            while(true)
            {
                layers[0].sprite = animation.GetFrameAt(frameIndex).sprite;
                frameIndex = (frameIndex + 1) % animation.frameCount;
                yield return new WaitForSeconds(1.0f / 10.0f);
            }
        }

        /// <summary>
        /// Returns the index matching the given animation name.  Use this method to 
        /// to cache the name to index conversion. If there is no animation matching the 
        /// given name then a value of -1 will be returned.
        public int GetAnimationIndex (string name)
        {
            if (_pixelArt == null)
                return -1;

            for (var animationIndex = 0; animationIndex < _pixelArt.animationCount; animationIndex++)
                if (0 == string.Compare(_pixelArt.GetAnimationAt(animationIndex)._name, name, false))
                    return animationIndex;

            return -1;
        }
    }
}
