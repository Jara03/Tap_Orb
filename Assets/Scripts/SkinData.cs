using System;
using UnityEngine;

[Serializable]
public class SkinData
{
    public string Name = "Default";
    public Color BallColor = Color.white;
    public float BallSize = 1f;
    public string BallMeshFileName = string.Empty;
    public Color BackgroundColor = Color.black;
    public string BackgroundSpriteName = string.Empty;
    public bool UseBackgroundImage = false;

    public SkinData Clone()
    {
        return new SkinData
        {
            Name = Name,
            BallColor = BallColor,
            BallSize = BallSize,
            BallMeshFileName = BallMeshFileName,
            BackgroundColor = BackgroundColor,
            BackgroundSpriteName = BackgroundSpriteName,
            UseBackgroundImage = UseBackgroundImage
        };
    }
}
