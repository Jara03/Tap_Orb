using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelMenuGenerator : MonoBehaviour
{
    [SerializeField] private Transform contentParent;   // Le Content du ScrollView
    [SerializeField] private GameObject levelButtonPrefab;
    [SerializeField] private int levelsCount = 9;      // Nombre total de niveaux

    private void Start()
    {
        GenerateLevelList();
    }

    private void GenerateLevelList()
    {
        for (int i = 1; i <= levelsCount; i++)
        {
            var obj = Instantiate(levelButtonPrefab, contentParent);
            var buttonUI = obj.GetComponent<LevelButton>();

            buttonUI.label.text = $"L{i}";

            int index = i; // capture local
            buttonUI.button.onClick.AddListener(() => LoadLevel(index));
        }
    }

    public void LoadLevel(int level)
    {
        LevelManager.levelSelected = level;
        // Construire le nom de la scène à charger, par ex : "Monde1/Level 1"
        string scenePath = $"Scenes/Levels/Level {LevelManager.levelSelected}";

        // Charger la scène de manière synchrone
        SceneManager.LoadScene(scenePath);

    }
}