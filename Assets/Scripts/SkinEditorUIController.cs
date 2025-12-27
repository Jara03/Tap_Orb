using System;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class SkinEditorUIController : MonoBehaviour
{
    [Header("Balle")]
    [SerializeField] private Slider[] BallColorSliders;
    [SerializeField] private Slider BallSizeSlider;
    [SerializeField] private GameObject BallPreview;
    [SerializeField] private Button ImportMeshButton;
    [SerializeField] private Text BallMeshLabel;

    [Header("Arrière-plan")]
    [SerializeField] private Slider[] BackgroundColorSliders;
    [SerializeField] private Image BackgroundPreview;
    [SerializeField] private Toggle UseImageToggle;
    [SerializeField] private Dropdown BackgroundDropdown;
    [SerializeField] private Button MobilePickerButton;

    [Header("Gestion")]
    [SerializeField] private InputField SkinNameInput;
    [SerializeField] private Button SaveButton;
    [SerializeField] private Transform SkinsListParent;
    [SerializeField] private Button toggleButton;
    [SerializeField] private GameObject SkinSelectorSection;
    [SerializeField] private GameObject SkinEditorSection;

    private SkinData workingCopy;
    private Mesh defaultPreviewMesh;
    private MeshFilter previewMeshFilter;

    private void Awake()
    {
        workingCopy = SkinManager.CurrentSkin.Clone();

        if (BallPreview != null)
        {
            previewMeshFilter = BallPreview.GetComponentInChildren<MeshFilter>();
            if (previewMeshFilter != null)
            {
                defaultPreviewMesh = previewMeshFilter.sharedMesh;
            }
        }

        HookupEvents();
        SyncUiFromSkin();
    }

    public void SaveSkin()
    {
        var name = SkinNameInput != null ? SkinNameInput.text : workingCopy.Name;
        SkinManager.SaveSkin(name, workingCopy);
    }

    public void ToggleSkinSelector()
    {
        if (SkinSelectorSection == null || SkinEditorSection == null)
            return;

        bool selectorActive = !SkinSelectorSection.activeSelf;
        SkinSelectorSection.SetActive(selectorActive);
        SkinEditorSection.SetActive(!selectorActive);
    }

    public void ApplyBallMeshFromPath(string sourcePath)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
            return;

        try
        {
            string storedFile = SkinManager.ImportBallMesh(sourcePath);
            workingCopy.BallMeshFileName = storedFile;
            UpdateMeshPreview();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Impossible d'importer le mesh: {ex.Message}");
        }
    }

    public void ClearBallMesh()
    {
        workingCopy.BallMeshFileName = string.Empty;
        UpdateMeshPreview();
    }

    private void HookupEvents()
    {
        if (BallColorSliders != null)
        {
            foreach (var slider in BallColorSliders)
            {
                if (slider == null) continue;
                slider.onValueChanged.AddListener(_ => OnBallColorChanged());
            }
        }

        if (BallSizeSlider != null)
        {
            BallSizeSlider.onValueChanged.AddListener(OnBallSizeChanged);
        }

        if (BackgroundColorSliders != null)
        {
            foreach (var slider in BackgroundColorSliders)
            {
                if (slider == null) continue;
                slider.onValueChanged.AddListener(_ => OnBackgroundColorChanged());
            }
        }

        if (UseImageToggle != null)
        {
            UseImageToggle.onValueChanged.AddListener(OnUseImageChanged);
        }

        if (SaveButton != null)
        {
            SaveButton.onClick.AddListener(SaveSkin);
        }

        if (toggleButton != null)
        {
            toggleButton.onClick.AddListener(ToggleSkinSelector);
        }

        if (ImportMeshButton != null)
        {
            ImportMeshButton.onClick.AddListener(OpenMeshPicker);
        }
    }

    private void OpenMeshPicker()
    {
#if UNITY_EDITOR
        string startDir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        string path = EditorUtility.OpenFilePanel("Importer un mesh de balle", startDir, "*");
        if (!string.IsNullOrEmpty(path))
        {
            ApplyBallMeshFromPath(path);
        }
#else
        Debug.Log("Utiliser un sélecteur natif pour fournir un chemin vers ApplyBallMeshFromPath.");
#endif
    }

    private void SyncUiFromSkin()
    {
        if (BallColorSliders != null && BallColorSliders.Length >= 3)
        {
            BallColorSliders[0].SetValueWithoutNotify(workingCopy.BallColor.r);
            BallColorSliders[1].SetValueWithoutNotify(workingCopy.BallColor.g);
            BallColorSliders[2].SetValueWithoutNotify(workingCopy.BallColor.b);
        }

        if (BallSizeSlider != null)
        {
            BallSizeSlider.SetValueWithoutNotify(workingCopy.BallSize);
        }

        if (BackgroundColorSliders != null && BackgroundColorSliders.Length >= 3)
        {
            BackgroundColorSliders[0].SetValueWithoutNotify(workingCopy.BackgroundColor.r);
            BackgroundColorSliders[1].SetValueWithoutNotify(workingCopy.BackgroundColor.g);
            BackgroundColorSliders[2].SetValueWithoutNotify(workingCopy.BackgroundColor.b);
        }

        if (UseImageToggle != null)
        {
            UseImageToggle.SetIsOnWithoutNotify(workingCopy.UseBackgroundImage);
        }

        if (SkinNameInput != null)
        {
            SkinNameInput.text = workingCopy.Name;
        }

        UpdateMeshPreview();
        UpdatePreviewVisuals();
    }

    private void OnBallColorChanged()
    {
        workingCopy.BallColor = ReadColor(BallColorSliders, workingCopy.BallColor);
        UpdatePreviewVisuals();
    }

    private void OnBallSizeChanged(float newValue)
    {
        workingCopy.BallSize = newValue;
        UpdatePreviewVisuals();
    }

    private void OnBackgroundColorChanged()
    {
        workingCopy.BackgroundColor = ReadColor(BackgroundColorSliders, workingCopy.BackgroundColor);
        UpdatePreviewVisuals();
    }

    private void OnUseImageChanged(bool useImage)
    {
        workingCopy.UseBackgroundImage = useImage;
        UpdatePreviewVisuals();
    }

    private Color ReadColor(Slider[] sliders, Color fallback)
    {
        if (sliders == null || sliders.Length < 3)
            return fallback;

        float r = sliders[0] != null ? sliders[0].value : fallback.r;
        float g = sliders[1] != null ? sliders[1].value : fallback.g;
        float b = sliders[2] != null ? sliders[2].value : fallback.b;
        return new Color(r, g, b, 1f);
    }

    private void UpdateMeshPreview()
    {
        Mesh mesh = null;
        GameObject prefab;
        if (!string.IsNullOrEmpty(workingCopy.BallMeshFileName) && SkinManager.TryLoadBallMesh(workingCopy.BallMeshFileName, out mesh, out prefab))
        {
            SetPreviewMesh(mesh);
            if (BallMeshLabel != null)
            {
                BallMeshLabel.text = prefab != null ? prefab.name : workingCopy.BallMeshFileName;
            }
        }
        else
        {
            SetPreviewMesh(defaultPreviewMesh);
            if (BallMeshLabel != null)
            {
                BallMeshLabel.text = string.IsNullOrEmpty(workingCopy.BallMeshFileName) ? "Mesh par défaut" : "Mesh non lisible";
            }
        }

        UpdatePreviewVisuals();
    }

    private void SetPreviewMesh(Mesh mesh)
    {
        if (previewMeshFilter == null || mesh == null)
            return;

        previewMeshFilter.sharedMesh = mesh;
    }

    private void UpdatePreviewVisuals()
    {
        if (BallPreview != null)
        {
            var renderer = BallPreview.GetComponentInChildren<MeshRenderer>();
            if (renderer != null)
            {
                renderer.material.SetColor("_EmissionColor", workingCopy.BallColor * 1f);
            }

            BallPreview.transform.localScale = Vector3.one * (1f + workingCopy.BallSize);
        }

        if (BackgroundPreview != null)
        {
            if (workingCopy.UseBackgroundImage)
            {
                BackgroundPreview.color = Color.white;
            }
            else
            {
                BackgroundPreview.color = workingCopy.BackgroundColor;
            }
        }
    }
}
