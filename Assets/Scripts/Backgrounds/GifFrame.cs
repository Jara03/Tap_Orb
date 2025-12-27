using UnityEngine;

namespace TapOrb.Backgrounds
{
    public class GifFrame
    {
        public Texture2D Texture { get; }
        public float Delay { get; }

        public GifFrame(Texture2D texture, float delay)
        {
            Texture = texture;
            Delay = delay;
        }
    }
}
