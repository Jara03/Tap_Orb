using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelDataManager : MonoBehaviour
{
    public GameObject[] stars;
    private int starwon = 0;

    public GameObject FinishedLevelUI;
    public GameObject StarsCount;

    void Start()
    {
        
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
    }

    void catchStar(GameObject star)
    {
        starwon++;
        Debug.Log("⭐ Star won : " + starwon);

        // Désactive ou détruit l'étoile ramassée

        if (starwon >= stars.Length)
        {
            Debug.Log("🎉 All stars collected!");
            endLevel();
        }
    }

    void endLevel()
    {
        //afficher l'UI de fin de partie 
        FinishedLevelUI.SetActive(true);
        displayStarWon();
        
        //mettre à jour les données de jeu
        LevelManager.updateLevelDatas(starwon);

    }

    public void LoadNextLevel()
    {
        Debug.Log("Next Level");
        //TODO afficher le prochain niveau
        
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
