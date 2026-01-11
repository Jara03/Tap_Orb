using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class SkinSelectionPreviewPanel : MonoBehaviour
{
    [Header("Background Preview")]
    [SerializeField] private Image backgroundPreview;      // image de preview (sprite ou thumbnail vidéo)
    [SerializeField] private Image bgColorImage;           // overlay couleur plein écran (optionnel)
    [SerializeField] private Image sharedBackground;       // optionnel (fallback = backgroundPreview)

    [Header("Video Thumbnail Preview")]
    [SerializeField] private int videoThumbMaxSize = 512;
    [SerializeField] private double videoThumbCaptureTimeSeconds = 0.2;
    [Tooltip("Optionnel : fallback thumbnail via VideoPlayer (Editor / si NativeGallery renvoie null).")]
    [SerializeField] private VideoPlayer thumbVideoPlayer;

    [Header("Orb 3D Preview")]
    [SerializeField] private OrbPreviewController orbPreviewController;
    [SerializeField] private RawImage orbPreviewRawImage;
    [SerializeField] private GameObject defaultBallPrefab;

    private RenderTexture thumbRT;
    private Sprite cachedVideoThumbSprite;
    private string cachedVideoThumbFor;
    private int videoThumbRequestId;

    public void ShowSkin(SkinData skin)
    {
        if (skin == null)
        {
            Clear();
            return;
        }

        if (backgroundPreview != null && sharedBackground == null)
            sharedBackground = backgroundPreview;

        UpdateBackgroundPreviewRobust(skin);
        UpdateOrbPreview(skin);
    }

    public void Clear()
    {
        // Invalide requêtes thumb en cours
        videoThumbRequestId++;

        // Background
        if (backgroundPreview != null)
        {
            backgroundPreview.enabled = false;
            backgroundPreview.sprite = null;
            backgroundPreview.color = Color.white;
        }

        if (bgColorImage != null)
        {
            bgColorImage.enabled = false;
            bgColorImage.color = Color.white;
        }

        if (sharedBackground != null)
        {
            sharedBackground.enabled = false;
            sharedBackground.sprite = null;
            sharedBackground.color = Color.white;
        }

        CleanupCachedVideoThumb();

        // Orb
        if (orbPreviewController != null) orbPreviewController.Clear();
        if (orbPreviewRawImage != null) orbPreviewRawImage.enabled = false;
    }

    // -------------------------
    // BACKGROUND (robuste)
    // -------------------------
    private void UpdateBackgroundPreviewRobust(SkinData skin)
    {
        // 1) Déduire "candidats" à partir des champs (même si booléens incohérents)
        string videoName = skin.BackgroundVideoName;
        string spriteName = skin.BackgroundSpriteName;

        // si quelqu’un a mis un .mp4 dans BackgroundSpriteName par erreur, on le récupère
        if (string.IsNullOrEmpty(videoName) && LooksLikeVideo(spriteName))
            videoName = spriteName;

        // si quelqu’un a mis une image dans BackgroundVideoName (rare), on la récupère
        if (string.IsNullOrEmpty(spriteName) && LooksLikeImage(videoName))
            spriteName = videoName;

        // 2) Priorité: Video > Image > Color (comme dans ton editor) :contentReference[oaicite:2]{index=2}
        bool useVideo =
            (skin.UseBackgroundVideo && !string.IsNullOrEmpty(videoName)) ||
            (!string.IsNullOrEmpty(videoName) && LooksLikeVideo(videoName)); // fallback si booléens pas fiables

        bool useImage =
            !useVideo && (
                (skin.UseBackgroundImage && !string.IsNullOrEmpty(spriteName)) ||
                (!string.IsNullOrEmpty(spriteName) && !LooksLikeVideo(spriteName)) // fallback
            );

        bool useColor = !useVideo && !useImage; // fallback final

        if (useVideo)
        {
            if (bgColorImage != null) bgColorImage.enabled = false;

            if (backgroundPreview != null)
            {
                backgroundPreview.enabled = true;
                backgroundPreview.preserveAspect = true;
            }

            EnsureVideoThumbnail(videoName, fallbackColor: skin.BackgroundColor);
        }
        else if (useImage)
        {
            // stop requêtes thumb
            videoThumbRequestId++;

            if (bgColorImage != null) bgColorImage.enabled = false;

            var sprite = LoadSpriteRobust(spriteName);

            if (backgroundPreview != null)
            {
                backgroundPreview.enabled = true;
                backgroundPreview.preserveAspect = true;
                backgroundPreview.sprite = sprite;
                backgroundPreview.color = (sprite != null) ? Color.white : skin.BackgroundColor;
            }

            if (sharedBackground != null)
            {
                sharedBackground.enabled = true;
                sharedBackground.preserveAspect = true;
                sharedBackground.sprite = sprite;
                sharedBackground.color = (sprite != null) ? Color.white : skin.BackgroundColor;
            }
        }
        else if (useColor)
        {
            // stop requêtes thumb
            videoThumbRequestId++;

            if (backgroundPreview != null)
            {
                backgroundPreview.enabled = false;
                backgroundPreview.sprite = null;
                backgroundPreview.color = Color.white;
            }

            if (bgColorImage != null)
            {
                bgColorImage.enabled = true;
                bgColorImage.color = skin.BackgroundColor;
            }

            if (sharedBackground != null)
            {
                sharedBackground.enabled = true;
                sharedBackground.sprite = null;
                sharedBackground.color = skin.BackgroundColor;
            }
        }
    }

    private Sprite LoadSpriteRobust(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;

        // 1) tente via SkinManager (Resources ou ce que tu as déjà)
        var sprite = SkinManager.LoadBackgroundSprite(name);
        if (sprite != null) return sprite;

        // 2) fallback : si c’est un fichier (png/jpg) dans persistentDataPath/Backgrounds
        if (!LooksLikeImage(name)) return null;

        string fullPath = Path.Combine(Application.persistentDataPath, "Backgrounds", name);
        if (!File.Exists(fullPath)) return null;

        try
        {
            byte[] bytes = File.ReadAllBytes(fullPath);
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!tex.LoadImage(bytes)) { Destroy(tex); return null; }

            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        }
        catch
        {
            return null;
        }
    }

    private void ApplyVideoThumbToUI(Sprite spriteOrNull, Color fallbackColor)
    {
        if (backgroundPreview != null)
        {
            backgroundPreview.enabled = true;
            backgroundPreview.preserveAspect = true;
            backgroundPreview.sprite = spriteOrNull;
            backgroundPreview.color = (spriteOrNull != null) ? Color.white : fallbackColor;
        }

        if (sharedBackground != null)
        {
            sharedBackground.enabled = true;
            sharedBackground.preserveAspect = true;
            sharedBackground.sprite = spriteOrNull;
            sharedBackground.color = (spriteOrNull != null) ? Color.white : fallbackColor;
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

    private void EnsureVideoThumbnail(string videoFileName, Color fallbackColor)
    {
        if (string.IsNullOrEmpty(videoFileName))
        {
            ApplyVideoThumbToUI(null, fallbackColor);
            return;
        }

        if (cachedVideoThumbSprite != null && cachedVideoThumbFor == videoFileName)
        {
            ApplyVideoThumbToUI(cachedVideoThumbSprite, fallbackColor);
            return;
        }

        // placeholder pendant chargement
        ApplyVideoThumbToUI(null, fallbackColor);

        int reqId = ++videoThumbRequestId;
        StartCoroutine(CoLoadVideoThumbnail(videoFileName, reqId, fallbackColor));
    }

    private IEnumerator CoLoadVideoThumbnail(string videoFileName, int reqId, Color fallbackColor)
    {
        string fullPath = Path.Combine(Application.persistentDataPath, "Backgrounds", videoFileName);
        if (!File.Exists(fullPath))
            yield break;

        Texture2D tex = null;

        // 1) NativeGallery sur device (comme ton editor) :contentReference[oaicite:3]{index=3}
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
        }
#endif

        // 2) Fallback VideoPlayer (Editor + fallback)
        if (tex == null && thumbVideoPlayer != null)
        {
            yield return StartCoroutine(CoThumbViaVideoPlayer(fullPath, reqId, t => tex = t));
        }

        if (reqId != videoThumbRequestId)
        {
            if (tex != null) Destroy(tex);
            yield break;
        }

        if (tex == null) yield break;

        CleanupCachedVideoThumb();

        cachedVideoThumbFor = videoFileName;
        cachedVideoThumbSprite = Sprite.Create(
            tex,
            new Rect(0, 0, tex.width, tex.height),
            new Vector2(0.5f, 0.5f),
            100f
        );

        ApplyVideoThumbToUI(cachedVideoThumbSprite, fallbackColor);
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

        // quelques frames pour remplir la RT
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

    private static bool LooksLikeVideo(string name)
    {
        var ext = Path.GetExtension(name)?.ToLowerInvariant();
        return ext == ".mp4" || ext == ".mov" || ext == ".m4v" || ext == ".avi" || ext == ".webm";
    }

    private static bool LooksLikeImage(string name)
    {
        var ext = Path.GetExtension(name)?.ToLowerInvariant();
        return ext == ".png" || ext == ".jpg" || ext == ".jpeg";
    }

    private void OnDestroy()
    {
        videoThumbRequestId++;
        CleanupCachedVideoThumb();

        if (thumbRT != null)
        {
            thumbRT.Release();
            Destroy(thumbRT);
            thumbRT = null;
        }
    }

    // -------------------------
    // ORB (inchangé)
    // -------------------------
    private void UpdateOrbPreview(SkinData skin)
    {
        bool canUse3D = orbPreviewController != null && orbPreviewRawImage != null;
        if (!canUse3D) return;

        bool hasMesh = SkinManager.TryGetBallMesh(skin.BallMeshName, out var mesh, out _);
        bool hasPrefab = defaultBallPrefab != null && string.IsNullOrWhiteSpace(skin.BallMeshName);

        if (hasMesh || hasPrefab)
        {
            orbPreviewRawImage.enabled = true;

            if (hasMesh) orbPreviewController.ShowMesh(mesh, skin.BallColor, skin.BallSize);
            else orbPreviewController.ShowOrbPrefab(defaultBallPrefab, skin.BallColor, skin.BallSize);

            orbPreviewController.SetPreviewColor(skin.BallColor);
            orbPreviewController.SetPreviewSize(skin.BallSize);
        }
        else
        {
            orbPreviewController.Clear();
            orbPreviewRawImage.enabled = false;
        }
    }
}
