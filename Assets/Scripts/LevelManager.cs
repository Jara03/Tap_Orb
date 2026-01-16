using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelManager : MonoBehaviour
{
   
    public static int levelSelected = 0;
    public static int levelDifficulty = 1;
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
            if (AdsConsentBootstrap.Instance == null)
            {
                Debug.LogWarning("[AdsConsent] Aucun AdsConsentBootstrap dans la scène. Les pubs ne seront pas chargées.");
            }
            
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

    public static int GetDifficultyForLevel(int level)
    {
        if (lvlCount <= 1)
        {
            return 1;
        }

        int tierSize = Mathf.CeilToInt(lvlCount / 3f);
        int difficulty = Mathf.Clamp(((level - 1) / tierSize) + 1, 1, 3);
        return difficulty;
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
            levelDifficulty = GetDifficultyForLevel(levelSelected);
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
        levelDifficulty = GetDifficultyForLevel(levelSelected);
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

        List<int> easyLevels = new List<int>();
        List<int> mediumLevels = new List<int>();
        List<int> hardLevels = new List<int>();

        foreach (int level in sessionLevelOrder)
        {
            int difficulty = GetDifficultyForLevel(level);
            if (difficulty == 1)
            {
                easyLevels.Add(level);
            }
            else if (difficulty == 2)
            {
                mediumLevels.Add(level);
            }
            else
            {
                hardLevels.Add(level);
            }
        }

        sessionLevelOrder = new List<int>(easyLevels.Count + mediumLevels.Count + hardLevels.Count);
        sessionLevelOrder.AddRange(easyLevels);
        sessionLevelOrder.AddRange(mediumLevels);
        sessionLevelOrder.AddRange(hardLevels);

        sessionLevelIndex = 0;
        if (levelSelected <= 0 || !sessionLevelOrder.Contains(levelSelected))
        {
            levelSelected = sessionLevelOrder[0];
        }
        levelDifficulty = GetDifficultyForLevel(levelSelected);
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
