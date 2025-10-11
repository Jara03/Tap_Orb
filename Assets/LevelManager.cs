using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelManager : MonoBehaviour
{
   
    public static int worldSelected = 0;
    public static int levelSelected = 0;
    public static LevelManager Instance;

    // Start is called before the first frame update
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // reste entre les scènes
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public static void updateLevelDatas(int score)
    {
        ScoreManager.updateStarScore(worldSelected, levelSelected, score);
    }

    public static void goBackHome()
    {
        SceneManager.LoadScene("Home");
        
    }
    
   
}
