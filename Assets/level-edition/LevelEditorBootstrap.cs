using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelEditorBootstrap : MonoBehaviour
{
    private const string HomeSceneName = "Home";
    private static LevelEditorBootstrap instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        if (instance != null) return;
        GameObject bootstrapObject = new GameObject("LevelEditorBootstrap");
        instance = bootstrapObject.AddComponent<LevelEditorBootstrap>();
        DontDestroyOnLoad(bootstrapObject);
    }

    private void Awake()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        InjectEditLevelButton();
        if (LevelEditorSession.IsEditorActive)
        {
            EnsureEditorManager();
        }
    }

    private void OnGUI()
    {
        if (SceneManager.GetActiveScene().name != HomeSceneName) return;
        if (LevelEditorSession.IsEditorActive) return;

        const int buttonWidth = 200;
        const int buttonHeight = 40;
        Rect rect = new Rect(20, 20, buttonWidth, buttonHeight);
        if (GUI.Button(rect, "Level Editor"))
        {
            LevelEditorSession.StartNewLevel();
            SceneManager.LoadScene(LevelEditorSession.DefaultTemplateScenePath);
        }
    }

    private void InjectEditLevelButton()
    {
        LevelDataManager levelDataManager = FindFirstObjectByType<LevelDataManager>();
        if (levelDataManager == null || levelDataManager.OptionsScreen == null) return;

        Transform optionsTransform = levelDataManager.OptionsScreen.transform;
        if (optionsTransform.Find("EditLevelButton") != null) return;

        GameObject buttonObject = new GameObject("EditLevelButton");
        buttonObject.transform.SetParent(optionsTransform, false);

        RectTransform rectTransform = buttonObject.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(260, 60);
        rectTransform.anchoredPosition = new Vector2(0, -220);

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.2f, 0.6f, 0.9f, 0.9f);

        Button button = buttonObject.AddComponent<Button>();
        button.onClick.AddListener(() => StartEditorInCurrentScene(levelDataManager));

        GameObject textObject = new GameObject("Text");
        textObject.transform.SetParent(buttonObject.transform, false);
        Text text = textObject.AddComponent<Text>();
        text.text = "Edit level";
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
    }

    private void StartEditorInCurrentScene(LevelDataManager levelDataManager)
    {
        LevelEditorSession.StartEditingExisting();
        levelDataManager.OptionsScreen.SetActive(false);
        Time.timeScale = 1f;
        EnsureEditorManager();
    }

    private void EnsureEditorManager()
    {
        if (FindFirstObjectByType<LevelEditorManager>() != null) return;

        GameObject managerObject = new GameObject("LevelEditorManager");
        managerObject.AddComponent<LevelEditorManager>();
    }
}
