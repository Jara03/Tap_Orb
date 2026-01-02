using UnityEngine;
using UnityEngine.UI;

public class ScrollToTopOnStart : MonoBehaviour
{
    [SerializeField] private ScrollRect scrollRect;

    void Start()
    {
        Canvas.ForceUpdateCanvases();                 // laisse Unity calculer les tailles
        scrollRect.verticalNormalizedPosition = 1f;   // top
        Canvas.ForceUpdateCanvases();
    }
}