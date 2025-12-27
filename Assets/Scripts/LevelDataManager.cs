using System;
using System.Collections;
using DefaultNamespace;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TapOrb.Backgrounds;

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
    private int gameOverCounter = 0;
    private float lastCallTime = -999f;

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
        
        //charge le skin de jeu 
        LoadSkin(SkinManager.CurrentSkin);

    }

    public void LoadSkin(SkinData sk)
    {
        //Modif de la balle
        PlayerBall.gameObject.GetComponent<MeshRenderer>().material.SetColor("_EmissionColor",sk.BallColor*1f);
        PlayerBall.transform.GetChild(0).GetComponent<SpriteRenderer>().color = new Color(sk.BallColor.r, sk.BallColor.g, sk.BallColor.b, 0.03f);
        PlayerBall.transform.GetChild(1).GetComponent<SpriteRenderer>().color = new Color(sk.BallColor.r, sk.BallColor.g, sk.BallColor.b, 0.15f);


        PlayerBall.transform.localScale *= 1+sk.BallSize;
        
        //Modif du bg
        GameObject bg = GameObject.FindGameObjectWithTag("Background");
        var animatedBg = bg.GetComponent<AnimatedBackgroundController>();
        if (animatedBg == null)
        {
            animatedBg = bg.AddComponent<AnimatedBackgroundController>();
        }
        if (!sk.UseBackgroundImage)
        {
            bg.GetComponent<Image>().color = new Color(sk.BackgroundColor.r, sk.BackgroundColor.g, sk.BackgroundColor.b, 1f);
            animatedBg.StopAnimation();

        }
        else
        {
            BackgroundAsset asset = SkinManager.LoadBackgroundAsset(sk.BackgroundSpriteName);
            if (asset != null && asset.IsAnimated)
            {
                animatedBg.ApplyGif(asset.GifFrames);
            }
            else
            {
                animatedBg.ApplySprite(asset?.StaticSprite);
            }
            bg.GetComponent<Image>().color = new Color(1f, 1f, 1f, 1f);
                //Resources.Load<Sprite>("Backgrounds/" + sk.BackgroundSpriteName);
        }
        
        
        
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
        
        
        //afficher une pub 
        gameOverCounter++;
        float now = Time.time; // temps depuis le lancement du jeu

        float deltaTimeAd = now - lastCallTime;

        if (gameOverCounter >= 3 && deltaTimeAd > 15f)
        {
            interstitialAds.ShowInterstitialAd();
            gameOverCounter = 0;
            lastCallTime = now;
        }
        
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
