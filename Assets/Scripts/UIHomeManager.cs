using UnityEngine;
using UnityEngine.SceneManagement;

public class UIHomeManager : MonoBehaviour
{
    //répartie la responsabilité pour la gestion des UI avec LevelManager
    
    public GameObject levelSelector;
    public GameObject worldSelector;
    public GameObject startButton;
    
    public LevelSelectorDisplay levelSelectorDisplay;

    void Start()
    {
        worldSelector.SetActive(false);
        levelSelector.SetActive(false);
        
        displayPreviousPage();
        
    }
    
    public void toggleLevelSelection(int worldNumber)
    {
        LevelManager.worldSelected = worldNumber;
        worldSelector.SetActive(false);
        levelSelector.SetActive(true);
        levelSelectorDisplay.updateStarsCountDiplays();
    }
    public void LoadLevel(int level)
    {
        LevelManager.levelSelected = level;
        // Construire le nom de la scène à charger, par ex : "Monde1/Level 1"
        string scenePath = $"Scenes/Monde {LevelManager.worldSelected}/Level {LevelManager.levelSelected}";

        // Charger la scène de manière synchrone
        SceneManager.LoadScene(scenePath);

    }
    
    public void displayPreviousPage()
    {
        if (LevelManager.worldSelected != 0)
        {
            startButton.SetActive(false);
            toggleLevelSelection(LevelManager.worldSelected);
          
            //TODO on pourra ajouter des animations de win ou unlock du mode random
            
        }
    }
    
       
    public void StartGameButtonClicked()
    {
        //activer le world selector
        worldSelector.SetActive(true);
        //levelSelector.SetActive(false);
        startButton.SetActive(false);
        
    }

}
