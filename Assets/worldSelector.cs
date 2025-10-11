using System;
using UnityEngine;

public class WorldSelector : MonoBehaviour
{
    
    public UIHomeManager uiHomeManager;
    // Start is called once before the first execution of Update after the MonoBehaviour is created



    public void WorldButtonClicked(int worldNumber)
    {
        //désactive la vue des mondes et affiche le LevelSelector
        Debug.Log("World " + worldNumber + " selected");
        uiHomeManager.toggleLevelSelection(worldNumber);

    }
}
