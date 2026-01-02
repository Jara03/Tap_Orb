using System;
using UnityEngine;

[Serializable]
public class SkinData
{
    public string Name = "Default";
    public Color BallColor = Color.white;
    public float BallSize = 1f;
    public Color BackgroundColor = Color.black;
    public string BackgroundSpriteName = string.Empty;
    public bool UseBackgroundImage = false;
    public string BackgroundVideoName = string.Empty;
    public bool UseBackgroundVideo = false;
    public string BallMeshName = string.Empty;
    public bool UseColorBackground = false;

    public SkinData Clone()
    {
        return new SkinData
        {
            Name = Name,
            BallColor = BallColor,
            BallSize = BallSize,
            BackgroundColor = BackgroundColor,
            BackgroundSpriteName = BackgroundSpriteName,
            UseBackgroundImage = UseBackgroundImage,
            BackgroundVideoName = BackgroundVideoName,
            UseBackgroundVideo = UseBackgroundVideo,
            UseColorBackground = UseColorBackground,
            BallMeshName = BallMeshName
        };
    }
}
