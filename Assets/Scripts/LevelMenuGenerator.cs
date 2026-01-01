using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelMenuGenerator : MonoBehaviour
{
    [SerializeField] private int levelsCount = 9;      // Nombre total de niveaux

    public void LoadLevel(int level)
    {
        LevelManager.levelSelected = level;
        // Construire le nom de la scène à charger, par ex : "Monde1/Level 1"
        string scenePath = $"Scenes/Levels/Level {LevelManager.levelSelected}";

        // Charger la scène de manière synchrone
        SceneManager.LoadScene(scenePath);

    }
}