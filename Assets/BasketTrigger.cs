using UnityEngine;

public class BasketTrigger : MonoBehaviour
{
    [SerializeField]
    private LevelDataManager levelDataManager;

    [SerializeField]
    private string targetTag = "Player";

    private bool hasTriggeredEnd;

    private void Reset()
    {
        CacheLevelDataManager();
        EnsureColliderIsTrigger();
    }

    private void Awake()
    {
        CacheLevelDataManager();
        EnsureColliderIsTrigger();
    }

    private void CacheLevelDataManager()
    {
        if (levelDataManager != null)
        {
            return;
        }

#if UNITY_2023_1_OR_NEWER
        levelDataManager = FindFirstObjectByType<LevelDataManager>();
#else
        levelDataManager = FindObjectOfType<LevelDataManager>();
#endif
    }

    private void EnsureColliderIsTrigger()
    {
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggeredEnd)
        {
            return;
        }

        if (!string.IsNullOrEmpty(targetTag) && !other.CompareTag(targetTag))
        {
            return;
        }

        if (levelDataManager != null)
        {
            levelDataManager.EndLevel();
            hasTriggeredEnd = true;
        }
        else
        {
            Debug.LogWarning("BasketTrigger: LevelDataManager reference is missing. Unable to end level.");
        }
    }
}
