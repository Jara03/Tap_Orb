using UnityEngine;
using UnityEngine.UI;

public class PillsController : MonoBehaviour
{
    [Header("Pills (Buttons)")]
    [SerializeField] private Button sizeButton;
    [SerializeField] private Button colorButton;
    [SerializeField] private Button modelButton;

    [Header("Panels")]
    [SerializeField] private GameObject sizePanel;
    [SerializeField] private GameObject colorPanel;
    [SerializeField] private GameObject modelPanel;

    private void Awake()
    {
        if (sizeButton)  sizeButton.onClick.AddListener(() => TogglePanel(sizePanel));
        if (colorButton) colorButton.onClick.AddListener(() => TogglePanel(colorPanel));
        if (modelButton) modelButton.onClick.AddListener(() => TogglePanel(modelPanel));
    }

    private void OnEnable()
    {
        OpenSizeByDefault();
    }

    private void OpenSizeByDefault()
    {
        CloseAll();
        if (sizePanel) sizePanel.SetActive(true);
    }

    private void TogglePanel(GameObject target)
    {
        if (target == null) return;

        bool willOpen = !target.activeSelf;

        // Un seul panel ouvert à la fois
        CloseAll();

        // Si on voulait l'ouvrir, on l'ouvre
        if (willOpen)
            target.SetActive(true);
    }

    public void CloseAll()
    {
        if (sizePanel)  sizePanel.SetActive(false);
        if (colorPanel) colorPanel.SetActive(false);
        if (modelPanel) modelPanel.SetActive(false);
    }
}