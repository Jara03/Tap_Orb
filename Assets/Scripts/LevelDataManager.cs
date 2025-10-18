using System;
using DefaultNamespace;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelDataManager : MonoBehaviour
{
    public GameObject[] stars;
    private int starwon = 0;

    public GameObject FinishedLevelUI;
    public GameObject StarsCount;
    public GameObject PlayerBall;

    private GameObject player;
    
    private Vector3 playerStartPosition;
    private Quaternion playerStartRotation;
    private Rigidbody playerRigidbody;

    private Level level;
    
    public Collider endGameCollider;
    
    
    public float resetDelay = 0.5f; // délai avant de reset après disparition
    public float minResetCooldown = 1.0f; // délai minimum entre deux resets
    
    public float lastSeenTime = 0f;

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
                StarPickup trigger = star.AddComponent<StarPickup>();
                trigger.onCollected = catchStar; // on abonne la méthode
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



    void catchStar(GameObject star)
    {
        starwon++;
        Debug.Log("⭐ Star won : " + starwon);

        // Désactive ou détruit l'étoile ramassée

        if (starwon >= stars.Length)
        {
            Debug.Log("🎉 All stars collected!");
            EndLevel();
        }
    }

    public void EndLevel()
    {
        //afficher l'UI de fin de partie
        FinishedLevelUI.SetActive(true);
        displayStarWon();
        Destroy(PlayerBall);
        //mettre à jour les données de jeu
        LevelManager.updateLevelDatas(starwon);

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

    public void displayStarWon()
    {

        //afficher le nombre d'enfants en fonction du score starwon
        for (int i = 0; i < StarsCount.transform.childCount; i++)
        {
            if (i < starwon)
            {
                StarsCount.transform.GetChild(i).gameObject.SetActive(true);
            }
        }


    }




// Classe interne pour gérer la détection sur chaque étoile
public class StarPickup : MonoBehaviour
{
    public System.Action<GameObject> onCollected;
    private GameObject starObject;

    private void Awake()
    {
        //le gameObject parent de ce gameObject
        starObject = gameObject.transform.parent.gameObject;
    }
    void OnTriggerEnter(Collider other)
    {
        // Suppose que le joueur a le tag "Player"
        if (other.CompareTag("Player"))
        {
            starObject.SetActive(false);
            onCollected?.Invoke(gameObject);
            
        }
    }
    }
}
