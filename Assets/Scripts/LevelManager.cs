using System.Collections;
using System.Collections.Generic;
using GoogleMobileAds.Api;
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
            MobileAds.Initialize((InitializationStatus initstatus) =>
            {
                if (initstatus == null)
                {
                    Debug.LogError("Google Mobile Ads initialization failed.");
                    return;
                }

                Debug.Log("Google Mobile Ads initialization complete.");

                // Google Mobile Ads events are raised off the Unity Main thread. If you need to
                // access UnityEngine objects after initialization,
                // use MobileAdsEventExecutor.ExecuteInUpdate(). For more information, see:
                // https://developers.google.com/admob/unity/global-settings#raise_ad_events_on_the_unity_main_thread
            });
        }
        else
        {
            Destroy(gameObject);
        }
        
    }

    public static void updateLevelDatas(int score)
    {
        ScoreManager.updateStarScore(levelSelected, score);
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
        string scenePath = $"Scenes/Levels/Level {nextLevel}";

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
