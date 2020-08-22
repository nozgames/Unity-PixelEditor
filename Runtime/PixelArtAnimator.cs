using UnityEngine;

namespace NoZ.PA
{
    public class PixelArtAnimator : MonoBehaviour
    {
        [SerializeField] private PixelArt _pixelArt = null;

        public PixelArt PixelArt {
            get => _pixelArt;
            set {
                if (value == _pixelArt)
                    return;

                _pixelArt = value;

                // TODO: animation
            }
        }

        public void Play (string name)
        {

        }

        public void Play(int index)
        {

        }

        /// <summary>
        /// Returns the index matching the given animation name.  Use this method to 
        /// to cache the name to index conversion. 
        public int GetAnimationIndex (string name)
        {
            return 0;
        }
    }
}
