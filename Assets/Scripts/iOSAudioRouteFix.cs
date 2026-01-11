using UnityEngine;

public class iOSAudioRouteFix : MonoBehaviour
{
    [Tooltip("Appelle le fix à chaque focus (recommandé car certains SDK reset la session).")]
    [SerializeField] private bool applyOnFocus = true;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        Apply();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (applyOnFocus && hasFocus)
            Apply();
    }

    private void Apply()
    {
#if UNITY_IOS && !UNITY_EDITOR
        // Important : joue via le speaker par défaut
        iOSAudioRoute.SetPlaybackCategory();
#endif
    }
}