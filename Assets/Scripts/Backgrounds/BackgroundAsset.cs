using System.Collections.Generic;
using UnityEngine;

namespace TapOrb.Backgrounds
{
    public class BackgroundAsset
    {
        public Sprite StaticSprite { get; set; }
        public List<GifFrame> GifFrames { get; set; }
        public bool IsAnimated => GifFrames != null && GifFrames.Count > 0;
    }
}
