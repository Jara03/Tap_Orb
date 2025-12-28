using System.Collections;
using System.Collections.Generic;
using GoogleMobileAds.Api;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelManager : MonoBehaviour
{
   
    public static int levelSelected = 0;
    public static LevelManager Instance;
    public static int lvlCount = 10;
    private const string LevelCountKey = "LevelCount";

    // Start is called before the first frame update
    
    void Awake()
    {
        if (Instance == null)
        {
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            ObjectiveManager.AttachTo(gameObject);
            ObjectiveManager.Instance.RegisterSessionStart();
            if (PlayerPrefs.HasKey(LevelCountKey))
            {
                lvlCount = PlayerPrefs.GetInt(LevelCountKey, lvlCount);
            }

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
            
            //abonner la fct loadSkin a l'event de SkinManager OnChangedSkin
           // SkinManager.OnSkinChanged += LoadSkin;
        }
        else
        {
            Destroy(gameObject);
        }
        
    }

    public static void SetLevelCount(int count)
    {
        lvlCount = count;
        PlayerPrefs.SetInt(LevelCountKey, count);
        PlayerPrefs.Save();
    }

    public void LoadSkin(SkinData sk)
    {
        
    }

    public static void goBackHome()
    {
        SceneManager.LoadScene("Home");
        Time.timeScale = 1f;
        

    }

    public static bool isLastLevel()
    {
        return levelSelected == lvlCount;
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
        else
        {
            scenePath = "Home";
        }
        

        // Charger la scène de manière synchrone
        Debug.Log("Loading scene : " + scenePath);
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
