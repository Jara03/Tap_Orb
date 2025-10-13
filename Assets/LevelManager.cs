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
    
    public static void goToNextLevel()
    {
        // Construire le nom de la scène à charger, par ex : "Monde1/Level 1"
        //uniquement si la scene existe
        int nextLevel = levelSelected + 1;
        string scenePath = $"Scenes/Monde {worldSelected}/Level {nextLevel}";

        if (SceneExists(scenePath))
        {
            levelSelected++;
        }
        

        // Charger la scène de manière synchrone
        SceneManager.LoadScene(scenePath);

    }
    private static bool SceneExists(string sceneName)
    {
        // Vérifie si la scène existe dans le Build Settings
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            if (name == sceneName || path.Contains(sceneName))
                return true;
        }
        return false;
    }

    
   
}
