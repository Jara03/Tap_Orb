using System;
using System.Collections;
using DefaultNamespace;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelDataManager : MonoBehaviour
{
    public GameObject[] stars;
    private int starwon = 0;

    public GameObject FinishedLevelUI;
    public GameObject OptionsScreen;
    public GameObject PlayerBall;
    
    public GameObject NextLevelButton;
    public GameObject HomeButton;

    private GameObject player;
    
    private Vector3 playerStartPosition;
    private Quaternion playerStartRotation;
    private Rigidbody playerRigidbody;
    
    private Level level;
    
    public Collider endGameCollider;
    
    
    public float resetDelay = 0.5f; // délai avant de reset après disparition
    public float minResetCooldown = 1.0f; // délai minimum entre deux resets
    
    public float lastSeenTime = 0f;

    public InterstitialAds interstitialAds;
    void Start()
    {
        //trouver l'objet qui contient le script Level
        level = FindFirstObjectByType<Level>();
        VacuumAttractor vacuum = FindFirstObjectByType<VacuumAttractor>();
        
        vacuum.OnEndLevel += EndLevel;
        
        if (level == null)
        {
            Debug.LogWarning("Aucun objet 'Level' trouvé dans la scène.");
        }
        
        // Ajoute un collider et un script de détection à chaque étoile si nécessaire
        foreach (GameObject star in stars)
        {
            if (star != null)
            {
                // S'assure que chaque étoile a un collider configuré comme trigger
                Collider col = star.GetComponent<Collider>();
              
                col.isTrigger = true;

                // Ajoute un composant pour gérer la détection de collision
            }
        }

        // Récupère le joueur et mémorise sa position de départ pour pouvoir le replacer si nécessaire
        player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerStartPosition = player.transform.position;
            playerStartRotation = player.transform.rotation;
            playerRigidbody = player.GetComponent<Rigidbody>();
        }
        
        interstitialAds = gameObject.AddComponent<InterstitialAds>();

    }
    void FixedUpdate()
    {
        
        if (player != null)
        {
            bool isVisible = IsPlayerVisible();
            
            if (!isVisible)
            {
                    ResetPlayerPosition();
                    level.RestoreDestroyedItems();
            }

        }
        
    }
    
    private bool IsPlayerVisible()
    {
      
        //si le joueur est dans la zone de vision
        if (endGameCollider.bounds.Contains(player.transform.position))
        {
            return true;
        }
            
        return false;
        
    }

    private void ResetPlayerPosition()
    {
        //supprimer et refaire spawn le prefab de la boule 
        
        
        player.transform.SetPositionAndRotation(playerStartPosition, playerStartRotation);

        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
            playerRigidbody.Sleep();
            playerRigidbody.WakeUp();
            
            //cancel toutes les forces qui sont appliquée sur la balle
        }

        Debug.LogWarning("Player reset triggered.");
    }


    public void ToggleOptionsScreen()
    {
        if (OptionsScreen.activeSelf == false)
        {
            level.isPaused = !OptionsScreen.activeSelf;
        }
        else
        {
            StartCoroutine(UnlockInputAfterDelay());
        }
        Debug.Log("level paused : " + level.isPaused);
        OptionsScreen.SetActive(!OptionsScreen.activeSelf);
        Time.timeScale = OptionsScreen.activeSelf ? 0f : 1f;
        
        IEnumerator UnlockInputAfterDelay()
        {
            yield return null; // bloque 1 frame → suffisant la plupart du temps
            yield return new WaitForSeconds(1f); // sécurité mobile
            level.isPaused = OptionsScreen.activeSelf;
        }
        
    }

    public void EndLevel()
    {
        //afficher l'UI de fin de partie
        FinishedLevelUI.SetActive(true);
        if (LevelManager.isLastLevel())
        {
            NextLevelButton.SetActive(false);
            CenterHomeButton();
        }
        Destroy(PlayerBall);
        
        //afficher une pub 
        interstitialAds.LoadInterstitialAd();
        interstitialAds.ShowInterstitialAd();
        
        void CenterHomeButton()
        {
            HomeButton.transform.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(-350, -25, 0);
        }
    }

    public void LoadNextLevel()
    {
        Debug.Log("Next Level");
        //TODO afficher le prochain niveau
        LevelManager.goToNextLevel();
        // cacher L'UI de fin de partie 
        FinishedLevelUI.SetActive(false);
    }

    public void BackHome()
    {
        Debug.Log("Back Home");
        LevelManager.goBackHome();        
    }

}
