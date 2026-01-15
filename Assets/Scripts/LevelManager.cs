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
    private static List<int> sessionLevelOrder;
    private static int sessionLevelIndex;

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

            InitializeLevelOrder();

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

#if UNITY_ANDROID || UNITY_IOS
                if (Debug.isDebugBuild)
                {
                    MobileAds.OpenAdInspector((AdInspectorError error) =>
                    {
                        if (error != null)
                        {
                            Debug.LogWarning("Ad Inspector n'a pas pu s'ouvrir (mode dev) : " + error);
                            return;
                        }

                        Debug.Log("Ad Inspector ouvert (mode dev).");
                    });
                }
#endif

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
        InitializeLevelOrder();
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
        EnsureLevelOrder();
        if (sessionLevelOrder == null || sessionLevelOrder.Count == 0)
        {
            return levelSelected == lvlCount;
        }

        return sessionLevelIndex >= sessionLevelOrder.Count - 1;
    }
    
    public static void goToNextLevel()
    {
        EnsureLevelOrder();
        // Construire le nom de la scène à charger, par ex : "Monde1/Level 1"
        //uniquement si la scene existe
        string scenePath = "Home";

        if (sessionLevelOrder != null && sessionLevelOrder.Count > 0 && sessionLevelIndex + 1 < sessionLevelOrder.Count)
        {
            sessionLevelIndex++;
            levelSelected = sessionLevelOrder[sessionLevelIndex];
            scenePath = $"Scenes/Levels/Level {levelSelected}";
        }

        if (SceneExists(scenePath))
        {
            if (scenePath == "Home")
            {
                SceneManager.LoadScene(scenePath);
                return;
            }
        }
        else
        {
            scenePath = "Home";
        }
        

        // Charger la scène de manière synchrone
        Debug.Log("Loading scene : " + scenePath);
        SceneManager.LoadScene(scenePath);

    }

    public static void SetSelectedLevel(int level)
    {
        EnsureLevelOrder();
        levelSelected = level;
        if (sessionLevelOrder != null && sessionLevelOrder.Count > 0)
        {
            int index = sessionLevelOrder.IndexOf(level);
            sessionLevelIndex = index >= 0 ? index : 0;
        }
    }

    private static void EnsureLevelOrder()
    {
        if (sessionLevelOrder == null || sessionLevelOrder.Count == 0)
        {
            InitializeLevelOrder();
        }
    }

    private static void InitializeLevelOrder()
    {
        sessionLevelOrder = new List<int>();
        for (int i = 1; i <= lvlCount; i++)
        {
            string scenePath = $"Scenes/Levels/Level {i}";
            if (SceneExists(scenePath))
            {
                sessionLevelOrder.Add(i);
            }
        }

        if (sessionLevelOrder.Count == 0)
        {
            return;
        }

        for (int i = sessionLevelOrder.Count - 1; i > 0; i--)
        {
            int swapIndex = Random.Range(0, i + 1);
            int temp = sessionLevelOrder[i];
            sessionLevelOrder[i] = sessionLevelOrder[swapIndex];
            sessionLevelOrder[swapIndex] = temp;
        }

        sessionLevelIndex = 0;
        if (levelSelected <= 0 || !sessionLevelOrder.Contains(levelSelected))
        {
            levelSelected = sessionLevelOrder[0];
        }
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
