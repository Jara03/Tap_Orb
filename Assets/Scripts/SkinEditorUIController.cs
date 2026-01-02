using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

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

    [Header("Ball Mesh")]
    public Button ImportBallMeshButton;
    public Button ResetBallMeshButton;
    public TMP_Text BallMeshNameLabel;

    public Button toggleButton;
    private Image sharedBackground;
    private SkinData workingCopy;

    public Transform SkinSelectorSection;
    public Transform BGEditorSection;
    public Transform OrbEditorSection;

    public Transform defaultSkinButtonPrefab;

    public void Start()
    {
        Debug.Log("SkinEditorUIController Start");
        Initialize();
    }

    public void Initialize()
    {
        workingCopy = SkinManager.CurrentSkin.Clone();

       // toggleButton.onClick.AddListener(TogglePanel);
        
        BindColorSliders(BallColorSliders, workingCopy.BallColor, OnBallColorChanged);
        BindColorSliders(BackgroundColorSliders, workingCopy.BackgroundColor, OnBackgroundColorChanged);
        BallSizeSlider.value = workingCopy.BallSize;
        BallSizeSlider.onValueChanged.AddListener(v => { workingCopy.BallSize = v; UpdatePreviews(); });
        UseImageToggle.isOn = workingCopy.UseBackgroundImage;
        UseImageToggle.onValueChanged.AddListener(OnUseImageToggled);

        PopulateBackgroundDropdown();
        BackgroundDropdown.onValueChanged.AddListener(OnBackgroundDropdownChanged);
        MobilePickerButton.onClick.AddListener(OnMobilePickRequested);

        if (ImportBallMeshButton != null)
            ImportBallMeshButton.onClick.AddListener(OnBallMeshImportRequested);

        if (ResetBallMeshButton != null)
            ResetBallMeshButton.onClick.AddListener(OnBallMeshResetRequested);

        SkinNameInput.text = workingCopy.Name;
        SaveButton.onClick.AddListener(SaveSkin);

        RefreshSavedSkins();
        UpdatePreviews();
        UpdateBallMeshLabel();
    }

    public void ToggleBGPanel()
    {
        Debug.Log("ToggleBGPanel");
        BGEditorSection.gameObject.SetActive(!BGEditorSection.gameObject.activeSelf);
    }
    
    public void ToggleOrbEditorPanel()
    {
        Debug.Log("ToggleOrbPanel");
        OrbEditorSection.gameObject.SetActive(!OrbEditorSection.gameObject.activeSelf);
    }

    public void ToggleSkinSelector()
    {
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
        // workingCopy.UseBackgroundImage = enabled;
        if (enabled)
        {
            workingCopy.UseBackgroundImage = true;
        }
        else
        {
            workingCopy.UseColorBackground = true;
        }
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
                StartCoroutine(SkinManager.ImportVideoiOS(path,(fileName) =>
                    {
                        // fileName = "bg_....mp4" (juste le nom, pas le path)
                        // Ici tu branches la suite logique : assigner au skin, sauvegarder, refresh UI, etc.

                        var skin = SkinManager.CurrentSkin; // adapte à ton code
                        skin.BackgroundVideoName = fileName;
                        skin.UseBackgroundVideo = true;
                        skin.UseBackgroundImage = false;
                    }));
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

    private void OnBallMeshImportRequested()
    {
#if UNITY_EDITOR
        var path = EditorUtility.OpenFilePanel("Choisir un mesh de balle", string.Empty, "obj,assetbundle,unity3d");
        if (!string.IsNullOrEmpty(path))
        {
            ApplyImportedBallMesh(path);
        }
        return;
#endif

        if (NativeGallery.IsMediaPickerBusy())
            return;

        NativeGallery.GetMixedMediaFromGallery(
            (path) =>
            {
                if (string.IsNullOrEmpty(path))
                    return;

                ApplyImportedBallMesh(path);
            },
            NativeGallery.MediaType.Image | NativeGallery.MediaType.Video,
            "Sélectionne un fichier 3D (OBJ ou AssetBundle)"
        );
    }

    private void ApplyImportedBallMesh(string path)
    {
        var importedName = SkinManager.ImportBallMeshFromGallery(path);
        if (string.IsNullOrEmpty(importedName))
            return;

        workingCopy.BallMeshName = importedName;
        UpdateBallMeshLabel();
        UpdatePreviews();
    }

    private void OnBallMeshResetRequested()
    {
        workingCopy.BallMeshName = string.Empty;
        UpdateBallMeshLabel();
        UpdatePreviews();
    }

    private void UpdateBallMeshLabel()
    {
        if (BallMeshNameLabel == null)
            return;

        BallMeshNameLabel.text = string.IsNullOrEmpty(workingCopy.BallMeshName)
            ? "Mesh par défaut"
            : workingCopy.BallMeshName;
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

        //var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        foreach (var skin in SkinManager.Skins)
        {
            // Instancie le prefab
            var btnGO = Instantiate(defaultSkinButtonPrefab, SkinsListParent, false);
            btnGO.name = skin.Name;

            // Récupère le label (Text) dans le prefab (enfant)
            var label = btnGO.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                label.color = Color.black;
                label.alignment = TextAnchor.MiddleLeft;
                label.text = skin.Name;
            }

            // Optionnel : si tu veux garder une légère transparence sur l'image de fond
            var img = btnGO.GetComponent<Image>();
            if (img != null)
                img.color = new Color(1f, 1f, 1f, 0.9f);

            // Hook du bouton
            var btn = btnGO.GetComponent<Button>();
            if (btn != null)
            {
                string skinName = skin.Name; // capture safe
                btn.onClick.AddListener(() =>
                {
                    SkinManager.ApplySkin(skinName);
                    workingCopy = SkinManager.CurrentSkin.Clone();
                    SyncFromWorkingCopy();
                });
            }
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
        UseImageToggle.SetIsOnWithoutNotify(workingCopy.UseColorBackground);

        UpdateBallMeshLabel();

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
