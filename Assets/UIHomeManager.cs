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
        //charger une scene : le monde séléctionné et le niveau choisi 
        
        SceneManager.LoadScene("Monde"+LevelManager.worldSelected);
        
        //puis indiquer les bonnes infos a la scene chargée

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
