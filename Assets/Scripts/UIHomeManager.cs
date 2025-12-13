using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIHomeManager : MonoBehaviour
{
    //répartie la responsabilité pour la gestion des UI avec LevelManager
    
    public GameObject levelSelector;
    public GameObject startButton;
    
    public static bool HasController { get; private set; }
    public static Action OnSplashScreenFinished;

    [Header("Skin Editor")]
    [SerializeField] private Canvas homeCanvas;

    void Start()
    {
        
        if (HasController) return;
        HasController = true;

        OnSplashScreenFinished += () => { Debug.Log("Splash finished! " + DateTime.Now.ToLongTimeString()); };
        StartCoroutine("SplashCoroutine");

        levelSelector.SetActive(false);

        if (homeCanvas == null)
        {
            homeCanvas = GetComponentInChildren<Canvas>(true);
        }

    }
    
    public void toggleLevelSelection(int worldNumber)
    {
        levelSelector.SetActive(true);
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
