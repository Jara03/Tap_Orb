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
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkinEditorUIController : MonoBehaviour
{
   
    public Slider[] BallColorSliders;
    public Slider BallSizeSlider;
    public Image BallPreview;
    public Slider[] BackgroundColorSliders;
    public Image BackgroundPreview;
    public Toggle UseImageToggle;
    public TMP_Dropdown BackgroundDropdown;
    public Button MobilePickerButton;
    public TMP_InputField SkinNameInput;
    public Button SaveButton;
    public Transform SkinsListParent;

    public Button toggleButton;
    private Image sharedBackground;
    private SkinData workingCopy;

    public Transform SkinSelectorSection;
    public Transform SkinEditorSection;

    public void Start()
    {
        Debug.Log("SkinEditorUIController Start");
        Initialize();
    }

    public void Initialize()
    {
        workingCopy = SkinManager.CurrentSkin.Clone();

        toggleButton.onClick.AddListener(TogglePanel);
        
        BindColorSliders(BallColorSliders, workingCopy.BallColor, OnBallColorChanged);
        BindColorSliders(BackgroundColorSliders, workingCopy.BackgroundColor, OnBackgroundColorChanged);
        BallSizeSlider.value = workingCopy.BallSize;
        BallSizeSlider.onValueChanged.AddListener(v => { workingCopy.BallSize = v; UpdatePreviews(); });
        UseImageToggle.isOn = workingCopy.UseBackgroundImage;
        UseImageToggle.onValueChanged.AddListener(OnUseImageToggled);

        PopulateBackgroundDropdown();
        BackgroundDropdown.onValueChanged.AddListener(OnBackgroundDropdownChanged);
        MobilePickerButton.onClick.AddListener(OnMobilePickRequested);

        SkinNameInput.text = workingCopy.Name;
        SaveButton.onClick.AddListener(SaveSkin);

        RefreshSavedSkins();
        UpdatePreviews();
    }

    public void TogglePanel()
    {
        Debug.Log("TogglePanel");
        SkinEditorSection.gameObject.SetActive(!SkinEditorSection.gameObject.activeSelf);
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
        Debug.Log("ToggleSkinSelector");
        SkinSelectorSection.gameObject.SetActive(!SkinSelectorSection.gameObject.activeSelf);
    }
    private void BindColorSliders(Slider[] sliders, Color initial, Action<Color> onChanged)
    {
        
        //Debug.Log("BindColorSliders");
        if (sliders == null || sliders.Length < 3) return;
        sliders[0].value = initial.r;
        sliders[1].value = initial.g;
        sliders[2].value = initial.b;

        sliders[0].onValueChanged.AddListener(_ => onChanged(CollectColor(sliders)));
        sliders[1].onValueChanged.AddListener(_ => onChanged(CollectColor(sliders)));
        sliders[2].onValueChanged.AddListener(_ => onChanged(CollectColor(sliders)));
    }

    private Color CollectColor(Slider[] sliders)
    {
        Debug.Log("CollectColor");
        return new Color(sliders[0].value, sliders[1].value, sliders[2].value, 1f);
    }

    private void OnBallColorChanged(Color color)
    {
        workingCopy.BallColor = color;
        UpdatePreviews();
    }

    private void OnBackgroundColorChanged(Color color)
    {
        workingCopy.BackgroundColor = color;
        UpdatePreviews();
    }

    private void OnUseImageToggled(bool enabled)
    {
        workingCopy.UseBackgroundImage = enabled;
        UpdatePreviews();
    }

    private void OnBackgroundDropdownChanged(int index)
    {
        if (index < 0 || index >= BackgroundDropdown.options.Count) return;
        var option = BackgroundDropdown.options[index];
        workingCopy.BackgroundSpriteName = option.text == "None" ? string.Empty : option.text;
        UpdatePreviews();
    }
    public void OnMobilePickRequested()
    {
        // Évite les appels multiples si le picker est déjà ouvert
        if (NativeGallery.IsMediaPickerBusy())
            return;

        // Pick image OR video
        NativeGallery.GetMixedMediaFromGallery(
            (path) =>
            {
                if (string.IsNullOrEmpty(path))
                {
                    Debug.Log("Media selection cancelled");
                    return;
                }

                Debug.Log("Picked path: " + path);

                // Détection type (méthode NativeGallery si dispo), sinon fallback extension
                NativeGallery.MediaType mediaType;
                try
                {
                    mediaType = NativeGallery.GetMediaTypeOfFile(path);
                }
                catch
                {
                    mediaType = IsVideoPath(path) ? NativeGallery.MediaType.Video : NativeGallery.MediaType.Image;
                }

#if UNITY_IOS && !UNITY_EDITOR
            // iOS : tu avais déjà un flow spécial pour l'image
            if (mediaType == NativeGallery.MediaType.Video)
            {
                StartCoroutine(SkinManager.ImportVideoiOS(path));
            }
            else
            {
                StartCoroutine(SkinManager.ImportImageiOS(path));
            }
#else
                if (mediaType == NativeGallery.MediaType.Video)
                {
                    SkinManager.ImportBackgroundVideoFromGallery(path);
                }
                else
                {
                    SkinManager.ImportBackgroundFromGallery(path);
                }
#endif

                // IMPORTANT: refresh UI après le choix + import déclenché
                UpdatePreviews();
                PopulateBackgroundDropdown();
            },
            NativeGallery.MediaType.Image | NativeGallery.MediaType.Video,
            "Select an image or video"
        );
    }

    private static bool IsVideoPath(string path)
    {
        var ext = Path.GetExtension(path)?.ToLowerInvariant();
        return ext == ".mp4" || ext == ".mov" || ext == ".m4v" || ext == ".avi" || ext == ".webm";
    }
    public void SaveSkin()
    {
        SkinManager.SaveSkin(SkinNameInput.text, workingCopy);
        RefreshSavedSkins();
    }

    private void RefreshSavedSkins()
    {
        foreach (Transform child in SkinsListParent)
        {
            if (child.gameObject.name != "Label")
            {
                Destroy(child.gameObject);
            }
        }

        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        foreach (var skin in SkinManager.Skins)
        {
            var btnObj = new GameObject(skin.Name, typeof(RectTransform), typeof(Image), typeof(Button));
            var rect = btnObj.GetComponent<RectTransform>();
            rect.SetParent(SkinsListParent, false);
            rect.sizeDelta = new Vector2(60, 60);
            btnObj.GetComponent<Image>().color = new Color(1, 1, 1, 0.9f);
            var textObj = new GameObject("Label", typeof(RectTransform), typeof(Text));
            var tRect = textObj.GetComponent<RectTransform>();
            tRect.SetParent(btnObj.transform, false);
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.offsetMin = Vector2.zero;
            tRect.offsetMax = Vector2.zero;
            var txt = textObj.GetComponent<Text>();
            txt.font = font;
            txt.color = Color.black;
            txt.alignment = TextAnchor.MiddleLeft;
            txt.text = skin.Name;

            var btn = btnObj.GetComponent<Button>();
            btn.onClick.AddListener(() => { SkinManager.ApplySkin(skin.Name); workingCopy = SkinManager.CurrentSkin.Clone(); SyncFromWorkingCopy(); });
        }
    }

    private void SyncFromWorkingCopy()
    {
        BallColorSliders[0].SetValueWithoutNotify(workingCopy.BallColor.r);
        BallColorSliders[1].SetValueWithoutNotify(workingCopy.BallColor.g);
        BallColorSliders[2].SetValueWithoutNotify(workingCopy.BallColor.b);
        BackgroundColorSliders[0].SetValueWithoutNotify(workingCopy.BackgroundColor.r);
        BackgroundColorSliders[1].SetValueWithoutNotify(workingCopy.BackgroundColor.g);
        BackgroundColorSliders[2].SetValueWithoutNotify(workingCopy.BackgroundColor.b);
        BallSizeSlider.SetValueWithoutNotify(workingCopy.BallSize);
        UseImageToggle.SetIsOnWithoutNotify(workingCopy.UseBackgroundImage);

        var index = BackgroundDropdown.options.FindIndex(o => o.text == workingCopy.BackgroundSpriteName);
        if (index < 0) index = 0;
        BackgroundDropdown.SetValueWithoutNotify(index);

        SkinNameInput.text = workingCopy.Name;
        UpdatePreviews();
    }

    private void UpdatePreviews()
    {
        BallPreview.color = workingCopy.BallColor;
        BallPreview.transform.localScale = Vector3.one * workingCopy.BallSize;

        BackgroundPreview.color = workingCopy.BackgroundColor;
        BackgroundPreview.sprite = null;
        BackgroundPreview.enabled = true;

        if (workingCopy.UseBackgroundImage)
        {
            var sprite = SkinManager.LoadBackgroundSprite(workingCopy.BackgroundSpriteName);
            
            BackgroundPreview.sprite = sprite;
            BackgroundPreview.color = sprite == null ? workingCopy.BackgroundColor : Color.white;
            BackgroundPreview.preserveAspect = true;
        }

        if (sharedBackground != null)
        {
            if (workingCopy.UseBackgroundImage)
            {
                var sprite = SkinManager.LoadBackgroundSprite(workingCopy.BackgroundSpriteName);
                
                sharedBackground.sprite = sprite ?? sharedBackground.sprite;
                sharedBackground.color = sprite == null ? workingCopy.BackgroundColor : Color.white;
            }
            else
            {
                sharedBackground.color = workingCopy.BackgroundColor;
            }
        }

        SkinManager.UpdateWorkingCopy(workingCopy);
    }

    private void PopulateBackgroundDropdown()
    {
        BackgroundDropdown.options.Clear();
        BackgroundDropdown.options.Add(new TMP_Dropdown.OptionData("None"));

        List<string> ids = new List<string>();

        // 1️⃣ Backgrounds utilisateur (persistent)
        string dir = Path.Combine(Application.persistentDataPath, "Backgrounds");
        if (Directory.Exists(dir))
        {
            foreach (var file in Directory.GetFiles(dir, "*.*"))
            {
                string fileName = Path.GetFileName(file);
                ids.Add(fileName);
                BackgroundDropdown.options.Add(
                    new TMP_Dropdown.OptionData(fileName)
                );
            }
        }

        // 2️⃣ Backgrounds par défaut (Resources)
        Sprite[] defaults = Resources.LoadAll<Sprite>("Backgrounds");
        foreach (var sprite in defaults)
        {
            ids.Add(sprite.name);
            BackgroundDropdown.options.Add(
                new TMP_Dropdown.OptionData(sprite.name)
            );
        }

        // 3️⃣ Restaurer la sélection
        int index = 0;
        if (!string.IsNullOrEmpty(workingCopy.BackgroundSpriteName))
        {
            index = BackgroundDropdown.options.FindIndex(
                o => o.text == workingCopy.BackgroundSpriteName
            );
            if (index < 0) index = 0;
        }

        BackgroundDropdown.value = index;
        BackgroundDropdown.RefreshShownValue();
    }

}
