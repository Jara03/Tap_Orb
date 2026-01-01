using System;
using System.Collections;
using DefaultNamespace;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public class LevelDataManager : MonoBehaviour
{
    public GameObject[] stars;
    private int starwon = 0;

    public GameObject FinishedLevelUI;
    public GameObject OptionsScreen;
    public GameObject PlayerBall;

    private Mesh defaultBallMesh;
    private Vector3 defaultBallScale = Vector3.one;
    private MeshCollider playerBallCollider;

    public GameObject NextLevelButton;
    public GameObject HomeButton;

    private GameObject player;

    private Vector3 playerStartPosition;
    private Quaternion playerStartRotation;
    private Rigidbody playerRigidbody;
    private VideoPlayer backgroundVideoPlayer;
    private RawImage backgroundRawImage;
    
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
        //VacuumAttractor vacuum = FindFirstObjectByType<VacuumAttractor>();
        
        VacuumAttractor[] vacuums = FindObjectsOfType<VacuumAttractor>();

        foreach (VacuumAttractor vacuum in vacuums)
        {
            vacuum.OnEndLevel += EndLevel;

        }
        
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

        if (PlayerBall != null)
        {
            playerBallCollider = PlayerBall.GetComponent<MeshCollider>();
            var filter = PlayerBall.GetComponent<MeshFilter>();
            if (filter != null)
            {
                defaultBallMesh = filter.sharedMesh;
            }

            defaultBallScale = PlayerBall.transform.localScale;
        }
        
        interstitialAds = gameObject.AddComponent<InterstitialAds>();
        
        //charge le skin de jeu 
        LoadSkin(SkinManager.CurrentSkin);

    }
    
    [SerializeField] private RenderTexture bgRT;

    private Coroutine bgCoroutine;
    
    [SerializeField] private bool forceEditorTestVideo = false;
    [SerializeField] private string editorTestVideoFileName = "test_bg.mp4";


    private void EnsureBgRT(int w = 1280, int h = 1920)
    {
        if (bgRT != null) return;

        bgRT = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32);
        bgRT.name = "RT_BG_Runtime";
        bgRT.Create();
    }
    
    private bool EnsureBackgroundRefs()
    {
        GameObject bg = GameObject.FindGameObjectWithTag("Background");
        if (bg == null)
        {
            Debug.LogError("[BG] No GameObject with tag 'Background' found.");
            return false;
        }

        if (backgroundRawImage == null)
            backgroundRawImage = bg.GetComponent<RawImage>();

        if (backgroundVideoPlayer == null)
            backgroundVideoPlayer = bg.GetComponent<VideoPlayer>();

        // Important : pour la vidéo, il faut RawImage + VideoPlayer
        if (backgroundRawImage == null)
            backgroundRawImage = bg.AddComponent<RawImage>();

        if (backgroundVideoPlayer == null)
            backgroundVideoPlayer = bg.AddComponent<VideoPlayer>();

        // Évite d’avoir Image et RawImage qui se battent : on désactive Image si présent
        var img = bg.GetComponent<Image>();
        if (img != null) img.enabled = false;

        return true;
    }

    private void SetupAndPlayVideo(string rawPath)
    {
        EnsureBgRT();

        if (bgRT == null)
        {
            Debug.LogError("[BG VIDEO] bgRT (RenderTexture) n'est pas assignée dans l'inspector !");
            return;
        }

        if (backgroundVideoPlayer == null || backgroundRawImage == null)
            return;

        StopBackgroundVideo();

        backgroundVideoPlayer.playOnAwake = false;
        backgroundVideoPlayer.isLooping = true;
        backgroundVideoPlayer.waitForFirstFrame = true;

        backgroundVideoPlayer.source = VideoSource.Url;
        backgroundVideoPlayer.url = ToFileUrl(rawPath);

        backgroundVideoPlayer.renderMode = VideoRenderMode.RenderTexture;
        backgroundVideoPlayer.targetTexture = bgRT;

        backgroundRawImage.texture = bgRT;
        backgroundRawImage.color = Color.white;
        backgroundRawImage.enabled = true;

        backgroundVideoPlayer.errorReceived -= OnVideoError;
        backgroundVideoPlayer.prepareCompleted -= OnVideoPrepared;
        backgroundVideoPlayer.errorReceived += OnVideoError;
        backgroundVideoPlayer.prepareCompleted += OnVideoPrepared;
        
        Debug.Log("[BG VIDEO] videoPath=" + rawPath);
        Debug.Log("[BG VIDEO] url=" + backgroundVideoPlayer.url);
        
        backgroundVideoPlayer.audioOutputMode = VideoAudioOutputMode.None;

        // Sécurité : si jamais des pistes sont détectées, on les mute
        ushort trackCount = backgroundVideoPlayer.audioTrackCount;
        for (ushort i = 0; i < trackCount; i++)
            backgroundVideoPlayer.EnableAudioTrack(i, false);


        backgroundVideoPlayer.Prepare();
    }

    private void OnVideoPrepared(VideoPlayer vp)
    {
        Debug.Log($"[BG VIDEO] Prepared OK: url={vp.url} w={vp.width} h={vp.height} len={vp.length}");
        vp.Play();
    }

    private void OnVideoError(VideoPlayer vp, string msg)
    {
        Debug.LogError($"[BG VIDEO] ERROR: {msg} url={vp.url}");
        StopBackgroundVideo();
    }

    private string ToFileUrl(string path)
    {
        if (string.IsNullOrEmpty(path)) return path;
        if (path.StartsWith("file://")) return path;
        return "file://" + path;
    }
    
    private static Texture2D SpriteToTexture(Sprite sprite)
    {
        if (sprite == null) return null;

        // Cas simple : sprite = texture entière
        if (sprite.rect.width == sprite.texture.width && sprite.rect.height == sprite.texture.height)
            return sprite.texture;

        // Cas atlas : on extrait la portion du sprite
        var r = sprite.rect;
        var tex = new Texture2D((int)r.width, (int)r.height, TextureFormat.RGBA32, false);
        var pixels = sprite.texture.GetPixels((int)r.x, (int)r.y, (int)r.width, (int)r.height);
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    private void ApplyBallSkin(SkinData sk)
    {
        
        
        
        if (PlayerBall == null)
            return;

        var meshFilter = PlayerBall.GetComponent<MeshFilter>();
        if (meshFilter == null)
            return;

        if (SkinManager.TryGetBallMesh(sk.BallMeshName, out var mesh, out var bounds))
        {
            meshFilter.sharedMesh = mesh;

            if (playerBallCollider != null)
            {
                playerBallCollider.sharedMesh = null;
                playerBallCollider.sharedMesh = mesh;
                playerBallCollider.convex = true;
            }

            PlayerBall.transform.localScale = ComputeScaleFromBounds(bounds) * (1 + sk.BallSize);
            
            PlayerBall.transform.GetComponent<SphereCollider>().radius = PlayerBall.transform.localScale.x;
            PlayerBall.transform.GetComponent<SphereCollider>().center = bounds.center;
            Debug.Log("fixed colliders");
            return;
        }

        meshFilter.sharedMesh = defaultBallMesh ?? meshFilter.sharedMesh;

        if (playerBallCollider != null)
        {
            var colliderMesh = defaultBallMesh ?? meshFilter.sharedMesh;
            playerBallCollider.sharedMesh = null;
            playerBallCollider.sharedMesh = colliderMesh;
            playerBallCollider.convex = true;
        }

        PlayerBall.transform.localScale = defaultBallScale * (1 + sk.BallSize);

    }

    private Vector3 ComputeScaleFromBounds(Bounds bounds)
    {
        if (defaultBallMesh == null)
            return defaultBallScale;

        var defaultBounds = defaultBallMesh.bounds;
        var defaultSize = Mathf.Max(defaultBounds.size.x, defaultBounds.size.y, defaultBounds.size.z);
        var newSize = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);

        if (newSize <= 0.0001f || defaultSize <= 0.0001f)
            return defaultBallScale;

        float normalizedScale = defaultSize / newSize;
        return defaultBallScale * normalizedScale;
    }



    public void LoadSkin(SkinData sk)
    {
        //Modif de la balle
        ApplyBallSkin(sk);
        PlayerBall.gameObject.GetComponent<MeshRenderer>().material.SetColor("_EmissionColor",sk.BallColor*1f);
        PlayerBall.transform.GetChild(0).GetComponent<SpriteRenderer>().color = new Color(sk.BallColor.r, sk.BallColor.g, sk.BallColor.b, 0.03f);
        PlayerBall.transform.GetChild(1).GetComponent<SpriteRenderer>().color = new Color(sk.BallColor.r, sk.BallColor.g, sk.BallColor.b, 0.15f);

        //Modif du bg
        // Modif du bg (toujours d'abord récupérer refs)
                if (!EnsureBackgroundRefs())
                    return;

        #if UNITY_EDITOR
                if (forceEditorTestVideo)
                {
                    var testPath = System.IO.Path.Combine(Application.streamingAssetsPath, editorTestVideoFileName);
                    Debug.Log("[EDITOR TEST VIDEO] trying: " + testPath);

                    SetupAndPlayVideo(testPath);
                    return;
                }
        #endif

        GameObject bg = GameObject.FindGameObjectWithTag("Background");
        if (bg == null)
        {
            Debug.LogError("[BG] No GameObject with tag 'Background' found.");
            return;
        }

        if (backgroundRawImage == null)
            backgroundRawImage = bg.GetComponent<RawImage>();

        if (backgroundVideoPlayer == null)
            backgroundVideoPlayer = bg.GetComponent<VideoPlayer>();

        if (backgroundRawImage == null)
        {
            Debug.LogError("[BG] Background object has no RawImage. Add a RawImage component on the BG prefab.");
            return;
        }

        // --- VIDEO BG ---
        if (sk.UseBackgroundVideo)
        {
            Debug.Log("[BG] Using background video");
            string videoPath = SkinManager.GetBackgroundVideoPath(sk.BackgroundVideoName);

            if (!string.IsNullOrEmpty(videoPath))
            {
                if (backgroundVideoPlayer == null)
                {
                    Debug.LogError("[BG] Background object has no VideoPlayer. Add a VideoPlayer component on the BG prefab.");
                    return;
                }

                // Lance la vidéo (SetupAndPlayVideo s’occupe d’assigner la RT au RawImage)
                SetupAndPlayVideo(videoPath);

                // RawImage doit rester activé
                backgroundRawImage.enabled = true;
                backgroundRawImage.color = Color.white;
            }
            else
            {
                // Fallback couleur si path manquant
                StopBackgroundVideo();
                backgroundRawImage.enabled = true;
                backgroundRawImage.texture = null;
                backgroundRawImage.color = new Color(sk.BackgroundColor.r, sk.BackgroundColor.g, sk.BackgroundColor.b, 1f);
            }

            return;
        }

        // --- FIXED IMAGE BG ---
        if (sk.UseBackgroundImage)
        {
            Debug.Log("[BG] Using background image");

            // IMPORTANT : en full RawImage, il faut une TEXTURE, pas un Sprite.
            // Si ton SkinManager te retourne un Sprite, convertis-le en Texture2D (voir helper plus bas).
            Sprite bgSprite = SkinManager.LoadBackgroundSprite(sk.BackgroundSpriteName);

            StopBackgroundVideo(); // stoppe la vidéo + nettoie

            backgroundRawImage.enabled = true;
            backgroundRawImage.texture = SpriteToTexture(bgSprite);
            backgroundRawImage.color = Color.white; // laisse la texture visible

            return;
        }

        // --- COLOR BG ---
        StopBackgroundVideo();
        backgroundRawImage.enabled = true;
        backgroundRawImage.texture = null;
        backgroundRawImage.color = new Color(sk.BackgroundColor.r, sk.BackgroundColor.g, sk.BackgroundColor.b, 1f);




    }

    private IEnumerator PlayBackgroundVideo()
    {
        if (backgroundVideoPlayer == null || backgroundRawImage == null)
            yield break;

        backgroundVideoPlayer.Prepare();
        while (!backgroundVideoPlayer.isPrepared)
        {
            yield return null;
        }

        backgroundRawImage.texture = backgroundVideoPlayer.texture;
        backgroundVideoPlayer.Play();
    }

    private void StopBackgroundVideo()
    {
        if (backgroundVideoPlayer != null)
        {
            backgroundVideoPlayer.Stop();
        }

        if (backgroundRawImage != null)
        {
            backgroundRawImage.enabled = false;
            backgroundRawImage.texture = null;
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
        ObjectiveManager.Instance?.RegisterLevelCompleted(LevelManager.levelSelected);
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
