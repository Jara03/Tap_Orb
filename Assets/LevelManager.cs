using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelManager : MonoBehaviour
{
    public GameObject levelSelector;
    public GameObject worldSelector;
    public GameObject startButton;

    private int worldSelected = 0;
    
    // Start is called before the first frame update
    void Start()
    {
        worldSelector.SetActive(false);
        levelSelector.SetActive(false);
        
        
    }
    
    public void toggleLevelSelection(int worldNumber)
    {
        worldSelected = worldNumber;
        worldSelector.SetActive(false);
        levelSelector.SetActive(true);
    }

    
   public void LoadLevel(int level)
    {
        //charger une scene : le monde séléctionné et le niveau choisi 
        
        SceneManager.LoadScene("Monde"+worldSelected);
        
        //puis indiquer les bonnes infos a la scene chargée
    }
   
   public void StartGameButtonClicked()
    {
        //activer le world selector
        worldSelector.SetActive(true);
        //levelSelector.SetActive(false);
        startButton.SetActive(false);
        
    }
}
