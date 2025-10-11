using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public static int[][] starscoreLevel;
    
    public static ScoreManager Instance;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // reste entre les scènes
        }
        else
        {
            Destroy(gameObject);
        }
    }
    void Start()
    {
        starscoreLevel = new int[10][];
        starscoreLevel[0] = new int[2];
        starscoreLevel[1] = new int[2];

    }

   public static void updateStarScore(int world,int level, int score)
    {
        starscoreLevel[world][level] = score;
        
    }
}
