using System.Collections.Generic;
using UnityEngine;

public class LevelSelectorDisplay : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public List<GameObject> starsCounts;
    
    void Start()
    {
        //updateStarsCountDiplays();
        
        //trouver la liste des gameObjects en "L*" dans LevelSelector
        
        foreach (Transform child in gameObject.transform)
        {
            if (child.name.StartsWith("L") && char.IsDigit(child.name[child.name.Length - 1]))
            {
                //ajouter a starsCounts le GameObject "starscount"    
                starsCounts.Add( child.gameObject.transform.Find("starscount").gameObject);
            }
        }
        
        
    }

    public void updateStarsCountDiplays()
    {
        int index = 0;

        foreach (var stars in starsCounts)
        {
            
            Debug.Log("World " + LevelManager.worldSelected + " Level " + index + "score : " + ScoreManager.starscoreLevel[index]);

            for (int i = 0; i < stars.transform.childCount; i++)
            {
                if (i < ScoreManager.starscoreLevel[index])
                {
                    stars.transform.GetChild(i).gameObject.SetActive(true);
                }
            }

            index++;

        }
    }

}
