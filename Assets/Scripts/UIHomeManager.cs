using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public class UIHomeManager : MonoBehaviour
{
    //répartie la responsabilité pour la gestion des UI avec LevelManager
    
    public GameObject levelSelector;
    public GameObject startButton;
    
    public LevelSelectorDisplay levelSelectorDisplay;
    
    public static bool HasController { get; private set; }
    public static Action OnSplashScreenFinished;

    void Start()
    {
        
        if (HasController) return;
        HasController = true;

        OnSplashScreenFinished += () => { Debug.Log("Splash finished! " + DateTime.Now.ToLongTimeString()); };
        StartCoroutine("SplashCoroutine");
        
        levelSelector.SetActive(false);
        
        displayPreviousPage();
        
    }
    
    public void toggleLevelSelection(int worldNumber)
    {
        LevelManager.worldSelected = worldNumber;
        levelSelector.SetActive(true);
        levelSelectorDisplay.updateStarsCountDiplays();
    }
    public void LoadLevel(int level)
    {
        LevelManager.levelSelected = level;
        // Construire le nom de la scène à charger, par ex : "Monde1/Level 1"
        string scenePath = $"Scenes/Levels/Level {LevelManager.levelSelected}";

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
        //activer le level selector
        levelSelector.SetActive(true);
        //levelSelector.SetActive(false);
        startButton.SetActive(false);
        
    }
    
    IEnumerator SplashCoroutine()
    {
        if (SplashScreen.isFinished)
            yield return new WaitForEndOfFrame();
        OnSplashScreenFinished();
    }

}
