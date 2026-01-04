using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class SkinEditorUIController : MonoBehaviour
{
    [Header("Ball UI")]
    public Slider[] BallColorSliders;
    public Slider BallSizeSlider;

    [Header("Background UI")]
    public Slider[] BackgroundColorSliders;
    public Image BackgroundPreview;         // Image de preview (sert aussi pour thumbnail vidéo)
    public Image bgColorimage;              // Image plein écran couleur (panel)
    public Toggle UseImageToggle;           // si tu veux garder ce toggle
    public TMP_Dropdown BackgroundDropdown;
    public Button MobilePickerButton;

    [Header("Save UI")]
    public TMP_InputField SkinNameInput;
    public Button SaveButton;
    public Button SaveBgButton;

    [Header("Skins list")]
    public Transform SkinsListParent;
    public Transform defaultSkinButtonPrefab;

    [Header("Ball Mesh")]
    public Button ImportBallMeshButton;
    public Button ResetBallMeshButton;
    public TMP_Text BallMeshNameLabel;

    [Header("Panels")]
    public Transform SkinSelectorSection;
    public Transform BGEditorSection;
    public Transform OrbEditorSection;

    [Header("Orb 3D Preview")]
    public OrbPreviewController OrbPreviewController;
    public RawImage OrbPreviewRawImage;
    [SerializeField] private GameObject DefaultBallPrefab;

    [Header("Optional: Shared background image (fallback = BackgroundPreview)")]
    [SerializeField] private Image sharedBackground;

    [Header("Video Thumbnail Preview")]
    [SerializeField] private int videoThumbMaxSize = 512;
    [SerializeField] private double videoThumbCaptureTimeSeconds = 0.2;

    [Tooltip("Optionnel : assigner un VideoPlayer pour générer un thumbnail en Editor ou en fallback si NativeGallery renvoie null.")]
    [SerializeField] private VideoPlayer thumbVideoPlayer;

    private RenderTexture thumbRT;
    private Sprite cachedVideoThumbSprite;
    private string cachedVideoThumbFor;
    private int videoThumbRequestId;

    private SkinData workingCopy;
    private bool initialized;

    private void Start()
    {
        Initialize();
    }

    public void Initialize()
    {
        if (initialized) return;
        initialized = true;

        if (BackgroundPreview != null && sharedBackground == null)
            sharedBackground = BackgroundPreview;

        workingCopy = SkinManager.CurrentSkin.Clone();

        // --- Bind sliders (avoid double listeners) ---
        BindColorSliders(BallColorSliders, workingCopy.BallColor, OnBallColorChanged);
        BindColorSliders(BackgroundColorSliders, workingCopy.BackgroundColor, OnBackgroundColorChanged);

        if (BallSizeSlider != null)
        {
            BallSizeSlider.onValueChanged.RemoveAllListeners();
            BallSizeSlider.SetValueWithoutNotify(workingCopy.BallSize);
            BallSizeSlider.onValueChanged.AddListener(v =>
            {
                workingCopy.BallSize = v;
                UpdatePreviews();
            });
        }

        if (UseImageToggle != null)
        {
            UseImageToggle.onValueChanged.RemoveAllListeners();
            UseImageToggle.SetIsOnWithoutNotify(workingCopy.UseBackgroundImage);
            UseImageToggle.onValueChanged.AddListener(OnUseImageToggled);
        }

        PopulateBackgroundDropdown();
        if (BackgroundDropdown != null)
        {
            BackgroundDropdown.onValueChanged.RemoveAllListeners();
            BackgroundDropdown.onValueChanged.AddListener(OnBackgroundDropdownChanged);
        }

        if (MobilePickerButton != null)
        {
            MobilePickerButton.onClick.RemoveAllListeners();
            MobilePickerButton.onClick.AddListener(OnMobilePickRequested);
        }

        if (ImportBallMeshButton != null)
        {
            ImportBallMeshButton.onClick.RemoveAllListeners();
            ImportBallMeshButton.onClick.AddListener(OnBallMeshImportRequested);
        }

        if (ResetBallMeshButton != null)
        {
            ResetBallMeshButton.onClick.RemoveAllListeners();
            ResetBallMeshButton.onClick.AddListener(OnBallMeshResetRequested);
        }

        if (SkinNameInput != null)
            SkinNameInput.text = workingCopy.Name;

        if (SaveButton != null)
        {
            SaveButton.onClick.RemoveAllListeners();
            SaveButton.onClick.AddListener(SaveSkin);
        }

        if (SaveBgButton != null)
        {
            SaveBgButton.onClick.RemoveAllListeners();
            SaveBgButton.onClick.AddListener(SaveSkin);
        }

        // Orb preview defaults
        if (OrbPreviewRawImage != null)
            OrbPreviewRawImage.enabled = false;

        // Désactive composants gênants sur le prefab de preview
        SetupDefaultBallPrefabForPreview();

        RefreshSavedSkins();
        UpdateBallMeshLabel();
        UpdatePreviews();
    }

    private void SetupDefaultBallPrefabForPreview()
    {
        if (DefaultBallPrefab == null) return;

        var input = DefaultBallPrefab.GetComponent<InputController>();
        if (input != null) input.enabled = false;

        var impact = DefaultBallPrefab.GetComponent<BallImpactSFX>();
        if (impact != null) impact.enabled = false;

        var audio = DefaultBallPrefab.GetComponent<AudioSource>();
        if (audio != null) audio.enabled = false;

        var renderer = DefaultBallPrefab.GetComponent<MeshRenderer>();
        if (renderer != null && OrbPreviewController != null && OrbPreviewController.PreviewMaterial != null)
            renderer.material = OrbPreviewController.PreviewMaterial;
    }

    // -------------------------
    // Panel toggles
    // -------------------------
    public void ToggleBGPanel()
    {
        if (BGEditorSection != null)
            BGEditorSection.gameObject.SetActive(!BGEditorSection.gameObject.activeSelf);
    }

    public void ToggleOrbEditorPanel()
    {
        if (OrbEditorSection != null)
            OrbEditorSection.gameObject.SetActive(!OrbEditorSection.gameObject.activeSelf);
    }

    public void ToggleSkinSelector()
    {
        if (SkinSelectorSection != null)
            SkinSelectorSection.gameObject.SetActive(!SkinSelectorSection.gameObject.activeSelf);
    }

    // -------------------------
    // Sliders binding
    // -------------------------
    private void BindColorSliders(Slider[] sliders, Color initial, Action<Color> onChanged)
    {
        if (sliders == null || sliders.Length < 3) return;

        sliders[0].onValueChanged.RemoveAllListeners();
        sliders[1].onValueChanged.RemoveAllListeners();
        sliders[2].onValueChanged.RemoveAllListeners();

        sliders[0].SetValueWithoutNotify(initial.r);
        sliders[1].SetValueWithoutNotify(initial.g);
        sliders[2].SetValueWithoutNotify(initial.b);

        sliders[0].onValueChanged.AddListener(_ => onChanged(CollectColor(sliders)));
        sliders[1].onValueChanged.AddListener(_ => onChanged(CollectColor(sliders)));
        sliders[2].onValueChanged.AddListener(_ => onChanged(CollectColor(sliders)));
    }

    private static Color CollectColor(Slider[] sliders)
    {
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

    // -------------------------
    // Background mode toggles
    // -------------------------
    private void OnUseImageToggled(bool enabled)
    {
        if (enabled)
        {
            workingCopy.UseBackgroundImage = true;
            workingCopy.UseColorBackground = false;

            // coupe vidéo
            workingCopy.UseBackgroundVideo = false;
            workingCopy.BackgroundVideoName = string.Empty;
        }
        else
        {
            workingCopy.UseColorBackground = true;
            workingCopy.UseBackgroundImage = false;

            workingCopy.UseBackgroundVideo = false;
            workingCopy.BackgroundVideoName = string.Empty;
        }

        UpdatePreviews();
        SyncDropdownSelectionFromWorkingCopy();
    }

    private void OnBackgroundDropdownChanged(int index)
    {
        if (BackgroundDropdown == null || index < 0 || index >= BackgroundDropdown.options.Count)
            return;

        var option = BackgroundDropdown.options[index].text;

        if (option == "None")
        {
            workingCopy.BackgroundSpriteName = string.Empty;
            workingCopy.BackgroundVideoName  = string.Empty;
            workingCopy.UseBackgroundImage   = false;
            workingCopy.UseBackgroundVideo   = false;
            // laisse UseColorBackground tel quel si tu veux
            UpdatePreviews();
            return;
        }

        string ext = Path.GetExtension(option).ToLowerInvariant();

        bool hasExtension = !string.IsNullOrEmpty(ext);
        bool isVideo = ext == ".mp4" || ext == ".mov" || ext == ".m4v" || ext == ".webm";
        bool isImage = ext == ".png" || ext == ".jpg" || ext == ".jpeg";

        if (hasExtension && isVideo)
        {
            workingCopy.UseBackgroundVideo = true;
            workingCopy.UseBackgroundImage = false;
            workingCopy.UseColorBackground = false;

            workingCopy.BackgroundVideoName = option;
            workingCopy.BackgroundSpriteName = string.Empty;

            if (UseImageToggle != null)
                UseImageToggle.SetIsOnWithoutNotify(false);
        }
        else
        {
            // Soit fichier image (avec extension), soit sprite Resources (sans extension)
            if (hasExtension && !isImage)
            {
                Debug.LogWarning($"Unsupported background file type: {option}");
                return;
            }

            workingCopy.UseBackgroundImage = true;
            workingCopy.UseBackgroundVideo = false;
            workingCopy.UseColorBackground = false;

            workingCopy.BackgroundSpriteName = option;
            workingCopy.BackgroundVideoName = string.Empty;

            if (UseImageToggle != null)
                UseImageToggle.SetIsOnWithoutNotify(true);
        }

        UpdatePreviews();
    }

    public void OnMobilePickRequested()
    {
        if (NativeGallery.IsMediaPickerBusy())
            return;

        NativeGallery.GetMixedMediaFromGallery(
            (path) =>
            {
                if (string.IsNullOrEmpty(path))
                {
                    Debug.Log("Media selection cancelled");
                    return;
                }

                NativeGallery.MediaType mediaType;
                try { mediaType = NativeGallery.GetMediaTypeOfFile(path); }
                catch { mediaType = IsVideoPath(path) ? NativeGallery.MediaType.Video : NativeGallery.MediaType.Image; }

#if UNITY_IOS && !UNITY_EDITOR
                if (mediaType == NativeGallery.MediaType.Video)
                {
                    StartCoroutine(SkinManager.ImportVideoiOS(path, (fileName) =>
                    {
                        // branche workingCopy
                        workingCopy.BackgroundVideoName = fileName;
                        workingCopy.UseBackgroundVideo = true;
                        workingCopy.UseBackgroundImage = false;
                        workingCopy.UseColorBackground = false;

                        PopulateBackgroundDropdown();
                        SyncDropdownSelectionFromWorkingCopy();
                        UpdatePreviews();
                    }));
                }
                else
                {
                    StartCoroutine(SkinManager.ImportImageiOS(path));
                    // si ImportImageiOS met à jour SkinManager.CurrentSkin plutôt que workingCopy,
                    // tu peux juste refresh après :
                    PopulateBackgroundDropdown();
                    UpdatePreviews();
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

                // Refresh UI après import
                PopulateBackgroundDropdown();
                UpdatePreviews();
#endif
            },
            NativeGallery.MediaType.Image | NativeGallery.MediaType.Video,
            "Select an image or video"
        );
    }

    // -------------------------
    // Video thumbnail system
    // -------------------------
    private void ApplyVideoThumbToUI(Sprite spriteOrNull)
    {
        if (BackgroundPreview != null)
        {
            BackgroundPreview.enabled = true;
            BackgroundPreview.preserveAspect = true;
            BackgroundPreview.sprite = spriteOrNull;
            BackgroundPreview.color = (spriteOrNull != null) ? Color.white : workingCopy.BackgroundColor;
        }

        if (sharedBackground != null)
        {
            sharedBackground.enabled = true;
            sharedBackground.preserveAspect = true;
            sharedBackground.sprite = spriteOrNull;
            sharedBackground.color = (spriteOrNull != null) ? Color.white : workingCopy.BackgroundColor;
        }
    }

    private void CleanupCachedVideoThumb()
    {
        if (cachedVideoThumbSprite != null)
        {
            var tex = cachedVideoThumbSprite.texture;
            Destroy(cachedVideoThumbSprite);
            if (tex != null) Destroy(tex);
            cachedVideoThumbSprite = null;
            cachedVideoThumbFor = null;
        }
    }

    private void EnsureVideoThumbnail(string videoFileName)
    {
        if (string.IsNullOrEmpty(videoFileName))
        {
            ApplyVideoThumbToUI(null);
            return;
        }

        if (cachedVideoThumbSprite != null && cachedVideoThumbFor == videoFileName)
        {
            ApplyVideoThumbToUI(cachedVideoThumbSprite);
            return;
        }

        // placeholder pendant chargement (ça affiche au moins une surface)
        ApplyVideoThumbToUI(null);

        int reqId = ++videoThumbRequestId;
        StartCoroutine(CoLoadVideoThumbnail(videoFileName, reqId));
    }

    private IEnumerator CoLoadVideoThumbnail(string videoFileName, int reqId)
    {
        string fullPath = Path.Combine(Application.persistentDataPath, "Backgrounds", videoFileName);
        if (!File.Exists(fullPath))
        {
            Debug.LogWarning("[Thumb] Video file not found: " + fullPath);
            yield break;
        }

        Texture2D tex = null;

        // 1) Try NativeGallery on device
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
        {
            Task<Texture2D> task = NativeGallery.GetVideoThumbnailAsync(
                fullPath,
                maxSize: videoThumbMaxSize,
                captureTimeInSeconds: videoThumbCaptureTimeSeconds,
                markTextureNonReadable: false
            );

            while (!task.IsCompleted) yield return null;

            if (reqId != videoThumbRequestId)
            {
                if (task.Result != null) Destroy(task.Result);
                yield break;
            }

            tex = task.Result;
            if (tex == null)
                Debug.LogWarning("[Thumb] NativeGallery returned null thumbnail, trying VideoPlayer fallback (if assigned).");
        }
#endif

        // 2) Fallback: grab one frame via VideoPlayer (Editor + fallback)
        if (tex == null && thumbVideoPlayer != null)
        {
            yield return StartCoroutine(CoThumbViaVideoPlayer(fullPath, reqId, t => tex = t));
        }

        if (reqId != videoThumbRequestId)
        {
            if (tex != null) Destroy(tex);
            yield break;
        }

        if (tex == null)
        {
            Debug.LogWarning("[Thumb] Thumbnail is null (no fallback or codec/time issue).");
            yield break;
        }

        // Still same mode/video?
        if (!workingCopy.UseBackgroundVideo || workingCopy.BackgroundVideoName != videoFileName)
        {
            Destroy(tex);
            yield break;
        }

        CleanupCachedVideoThumb();

        cachedVideoThumbFor = videoFileName;
        cachedVideoThumbSprite = Sprite.Create(
            tex,
            new Rect(0, 0, tex.width, tex.height),
            new Vector2(0.5f, 0.5f),
            100f
        );

        ApplyVideoThumbToUI(cachedVideoThumbSprite);
    }

    private IEnumerator CoThumbViaVideoPlayer(string fullPath, int reqId, Action<Texture2D> onDone)
    {
        onDone?.Invoke(null);

        if (thumbVideoPlayer == null)
            yield break;

        if (thumbRT == null)
        {
            int s = Mathf.Max(256, videoThumbMaxSize);
            thumbRT = new RenderTexture(s, s, 0, RenderTextureFormat.ARGB32);
            thumbRT.Create();
        }

        thumbVideoPlayer.Stop();
        thumbVideoPlayer.playOnAwake = false;
        thumbVideoPlayer.isLooping = false;
        thumbVideoPlayer.audioOutputMode = VideoAudioOutputMode.None;
        thumbVideoPlayer.renderMode = VideoRenderMode.RenderTexture;
        thumbVideoPlayer.targetTexture = thumbRT;
        thumbVideoPlayer.waitForFirstFrame = true;
        thumbVideoPlayer.skipOnDrop = true;

        string url = fullPath;
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
        url = "file://" + fullPath;
#endif
        thumbVideoPlayer.url = url;

        thumbVideoPlayer.Prepare();
        while (!thumbVideoPlayer.isPrepared)
        {
            if (reqId != videoThumbRequestId) yield break;
            yield return null;
        }

        // Jouer juste assez pour remplir la RT
        thumbVideoPlayer.Play();
        yield return null;
        yield return null;

        if (reqId != videoThumbRequestId)
        {
            thumbVideoPlayer.Stop();
            yield break;
        }

        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = thumbRT;

        Texture2D tex = new Texture2D(thumbRT.width, thumbRT.height, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, thumbRT.width, thumbRT.height), 0, 0);
        tex.Apply(false, false);

        RenderTexture.active = prev;
        thumbVideoPlayer.Stop();

        onDone?.Invoke(tex);
    }

    // -------------------------
    // Previews
    // -------------------------
    private void UpdatePreviews()
    {
        // --- Orb 3D preview (mesh OR prefab) ---
        bool hasMesh = SkinManager.TryGetBallMesh(workingCopy.BallMeshName, out var mesh, out _);
        bool hasPrefab = DefaultBallPrefab != null && string.IsNullOrWhiteSpace(workingCopy.BallMeshName);
        bool canUse3D = OrbPreviewController != null && OrbPreviewRawImage != null;

        if (canUse3D && (hasMesh || hasPrefab))
        {
            OrbPreviewRawImage.enabled = true;

            if (hasMesh)
                OrbPreviewController.ShowMesh(mesh, workingCopy.BallColor, workingCopy.BallSize);
            else
                OrbPreviewController.ShowOrbPrefab(DefaultBallPrefab, workingCopy.BallColor, workingCopy.BallSize);

            OrbPreviewController.SetPreviewColor(workingCopy.BallColor);
            OrbPreviewController.SetPreviewSize(workingCopy.BallSize);
        }
        else
        {
            OrbPreviewController?.Clear();
            if (OrbPreviewRawImage != null) OrbPreviewRawImage.enabled = false;
        }

        // --- Background previews (PRIORITÉ: Video > Image > Color) ---
        bool useVideo = workingCopy.UseBackgroundVideo && !string.IsNullOrEmpty(workingCopy.BackgroundVideoName);
        bool useImage = workingCopy.UseBackgroundImage && !useVideo;
        bool useColor = workingCopy.UseColorBackground && !useVideo && !useImage;

        if (useVideo)
        {
            if (bgColorimage != null) bgColorimage.enabled = false;

            // Assure au moins que le preview est activé
            if (BackgroundPreview != null)
            {
                BackgroundPreview.enabled = true;
                BackgroundPreview.preserveAspect = true;
            }

            EnsureVideoThumbnail(workingCopy.BackgroundVideoName);
        }
        else if (useImage)
        {
            // invalidate async thumb request
            videoThumbRequestId++;

            if (bgColorimage != null) bgColorimage.enabled = false;

            var sprite = SkinManager.LoadBackgroundSprite(workingCopy.BackgroundSpriteName);

            if (BackgroundPreview != null)
            {
                BackgroundPreview.enabled = true;
                BackgroundPreview.preserveAspect = true;
                BackgroundPreview.sprite = sprite;
                BackgroundPreview.color = (sprite != null) ? Color.white : workingCopy.BackgroundColor;
            }

            if (sharedBackground != null)
            {
                sharedBackground.enabled = true;
                sharedBackground.preserveAspect = true;
                sharedBackground.sprite = sprite;
                sharedBackground.color = (sprite != null) ? Color.white : workingCopy.BackgroundColor;
            }
        }
        else if (useColor)
        {
            videoThumbRequestId++;

            if (BackgroundPreview != null)
            {
                BackgroundPreview.enabled = false;
                BackgroundPreview.sprite = null;
                BackgroundPreview.color = Color.white;
            }

            if (bgColorimage != null)
            {
                bgColorimage.enabled = true;
                bgColorimage.color = workingCopy.BackgroundColor;
            }

            if (sharedBackground != null)
            {
                sharedBackground.enabled = true;
                sharedBackground.sprite = null;
                sharedBackground.color = workingCopy.BackgroundColor;
            }
        }
        else
        {
            videoThumbRequestId++;

            if (BackgroundPreview != null)
            {
                BackgroundPreview.enabled = false;
                BackgroundPreview.sprite = null;
                BackgroundPreview.color = Color.white;
            }

            if (bgColorimage != null)
            {
                bgColorimage.enabled = false;
                bgColorimage.color = Color.white;
            }

            if (sharedBackground != null)
                sharedBackground.enabled = false;
        }

        SkinManager.UpdateWorkingCopy(workingCopy);
    }

    // -------------------------
    // Dropdown population + sync
    // -------------------------
    private void PopulateBackgroundDropdown()
    {
        if (BackgroundDropdown == null) return;

        BackgroundDropdown.options.Clear();
        BackgroundDropdown.options.Add(new TMP_Dropdown.OptionData("None"));

        // 1) User backgrounds (persistentDataPath/Backgrounds)
        string dir = Path.Combine(Application.persistentDataPath, "Backgrounds");
        if (Directory.Exists(dir))
        {
            foreach (var file in Directory.GetFiles(dir, "*.*"))
            {
                string ext = Path.GetExtension(file).ToLowerInvariant();
                bool ok = ext == ".png" || ext == ".jpg" || ext == ".jpeg" ||
                          ext == ".mp4" || ext == ".mov" || ext == ".m4v" || ext == ".webm";
                if (!ok) continue;

                string fileName = Path.GetFileName(file);
                BackgroundDropdown.options.Add(new TMP_Dropdown.OptionData(fileName));
            }
        }

        // 2) Default backgrounds (Resources/Backgrounds)
        Sprite[] defaults = Resources.LoadAll<Sprite>("Backgrounds");
        foreach (var sprite in defaults)
        {
            if (sprite == null) continue;
            BackgroundDropdown.options.Add(new TMP_Dropdown.OptionData(sprite.name));
        }

        SyncDropdownSelectionFromWorkingCopy();
        BackgroundDropdown.RefreshShownValue();
    }

    private void SyncDropdownSelectionFromWorkingCopy()
    {
        if (BackgroundDropdown == null) return;

        string selected =
            (workingCopy.UseBackgroundVideo && !string.IsNullOrEmpty(workingCopy.BackgroundVideoName))
                ? workingCopy.BackgroundVideoName
                : workingCopy.BackgroundSpriteName;

        int index = 0;
        if (!string.IsNullOrEmpty(selected))
        {
            index = BackgroundDropdown.options.FindIndex(o => o.text == selected);
            if (index < 0) index = 0;
        }

        BackgroundDropdown.SetValueWithoutNotify(index);
    }

    private void SyncFromWorkingCopy()
    {
        if (BallColorSliders != null && BallColorSliders.Length >= 3)
        {
            BallColorSliders[0].SetValueWithoutNotify(workingCopy.BallColor.r);
            BallColorSliders[1].SetValueWithoutNotify(workingCopy.BallColor.g);
            BallColorSliders[2].SetValueWithoutNotify(workingCopy.BallColor.b);
        }

        if (BackgroundColorSliders != null && BackgroundColorSliders.Length >= 3)
        {
            BackgroundColorSliders[0].SetValueWithoutNotify(workingCopy.BackgroundColor.r);
            BackgroundColorSliders[1].SetValueWithoutNotify(workingCopy.BackgroundColor.g);
            BackgroundColorSliders[2].SetValueWithoutNotify(workingCopy.BackgroundColor.b);
        }

        if (BallSizeSlider != null)
            BallSizeSlider.SetValueWithoutNotify(workingCopy.BallSize);

        if (UseImageToggle != null)
            UseImageToggle.SetIsOnWithoutNotify(workingCopy.UseBackgroundImage);

        UpdateBallMeshLabel();

        SyncDropdownSelectionFromWorkingCopy();

        if (SkinNameInput != null)
            SkinNameInput.text = workingCopy.Name;

        UpdatePreviews();
    }

    // -------------------------
    // Ball mesh import
    // -------------------------
    private void OnBallMeshImportRequested()
    {
#if UNITY_EDITOR
        var path = EditorUtility.OpenFilePanel("Choisir un mesh de balle", string.Empty, "obj,assetbundle,unity3d");
        if (!string.IsNullOrEmpty(path))
            ApplyImportedBallMesh(path);
        return;
#endif

        NativeFilePicker.PickFile(
            (path) =>
            {
                if (string.IsNullOrEmpty(path)) return;
                ApplyImportedBallMesh(path);
            },
            new[] { "obj", "assetbundle", "unity3d" }
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
        if (BallMeshNameLabel == null) return;

        BallMeshNameLabel.text = string.IsNullOrEmpty(workingCopy.BallMeshName)
            ? "Mesh par défaut"
            : workingCopy.BallMeshName;
    }

    // -------------------------
    // Save + list
    // -------------------------
    public void SaveSkin()
    {
        SkinManager.SaveSkin(SkinNameInput != null ? SkinNameInput.text : workingCopy.Name, workingCopy);
        RefreshSavedSkins();
    }

    private void RefreshSavedSkins()
    {
        if (SkinsListParent == null) return;

        foreach (Transform child in SkinsListParent)
        {
            if (child.gameObject.name != "Label")
                Destroy(child.gameObject);
        }

        foreach (var skin in SkinManager.Skins)
        {
            if (defaultSkinButtonPrefab == null) break;

            var btnGO = Instantiate(defaultSkinButtonPrefab, SkinsListParent, false);
            btnGO.name = skin.Name;

            var label = btnGO.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                label.color = Color.black;
                label.alignment = TextAnchor.MiddleLeft;
                label.text = skin.Name;
            }

            var img = btnGO.GetComponent<Image>();
            if (img != null)
                img.color = new Color(1f, 1f, 1f, 0.9f);

            var btn = btnGO.GetComponent<Button>();
            if (btn != null)
            {
                string skinName = skin.Name;
                btn.onClick.AddListener(() =>
                {
                    SkinManager.ApplySkin(skinName);
                    workingCopy = SkinManager.CurrentSkin.Clone();
                    SyncFromWorkingCopy();
                });
            }
        }
    }

    // -------------------------
    // Utils
    // -------------------------
    private static bool IsVideoPath(string path)
    {
        var ext = Path.GetExtension(path)?.ToLowerInvariant();
        return ext == ".mp4" || ext == ".mov" || ext == ".m4v" || ext == ".avi" || ext == ".webm";
    }

    private void OnDestroy()
    {
        // Invalide requêtes thumb en cours
        videoThumbRequestId++;

        CleanupCachedVideoThumb();

        if (thumbRT != null)
        {
            thumbRT.Release();
            Destroy(thumbRT);
            thumbRT = null;
        }
    }
}
