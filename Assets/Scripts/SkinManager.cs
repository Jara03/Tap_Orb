using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public static class SkinManager
{
    private const string StorageKey = "skins.v1";
    private const string CurrentSkinKey = "skins.current";

    public static event Action<SkinData> OnSkinChanged;

    private static readonly List<SkinData> skins = new List<SkinData>();
    private static readonly Dictionary<string, CachedMeshEntry> cachedBallMeshes =
        new Dictionary<string, CachedMeshEntry>(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, Sprite> cachedBackgroundSprites =
        new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
    private static SkinData currentSkin;

    public static IReadOnlyList<SkinData> Skins => skins;
    public static SkinData CurrentSkin => currentSkin ?? EnsureDefault();

    static SkinManager()
    {
        LoadFromPrefs();
    }
     
    public static string ImportBackgroundFromGallery(string sourcePath)
    {
        if (string.IsNullOrEmpty(sourcePath) || !File.Exists(sourcePath))
            return string.Empty;

        string targetDir = Path.Combine(Application.persistentDataPath, "Backgrounds");

        if (!Directory.Exists(targetDir))
            Directory.CreateDirectory(targetDir);

        string fileName = "bg_" + DateTime.Now.Ticks + ".png";
        string destPath = Path.Combine(targetDir, fileName);
       // Debug.LogError("Persistent path: " + destPath);


        File.Copy(sourcePath, destPath, true);

        return fileName;
    }
        
        private static bool IsVideoFile(string nameOrPath)
    {
        if (string.IsNullOrEmpty(nameOrPath)) return false;
        string ext = Path.GetExtension(nameOrPath).ToLowerInvariant();
        return ext == ".mp4" || ext == ".mov" || ext == ".m4v" || ext == ".avi" || ext == ".webm";
    }

    private static bool IsImageFile(string nameOrPath)
    {
        if (string.IsNullOrEmpty(nameOrPath)) return false;
        string ext = Path.GetExtension(nameOrPath).ToLowerInvariant();
        return ext == ".png" || ext == ".jpg" || ext == ".jpeg";
    }

    /// <summary>
    /// Rend l'état du background cohérent.
    /// - Une seule source active: Video > Image > Color
    /// - Corrige les cas où un mp4 est rangé dans BackgroundSpriteName, etc.
    /// </summary>
    private static void NormalizeBackgroundMode(SkinData s)
    {
        if (s == null) return;

        bool hasVideo = !string.IsNullOrEmpty(s.BackgroundVideoName) && IsVideoFile(s.BackgroundVideoName);
        bool hasImage = !string.IsNullOrEmpty(s.BackgroundSpriteName)
            && (IsImageFile(s.BackgroundSpriteName) || !Path.HasExtension(s.BackgroundSpriteName));

        // Si mode couleur => purge le reste
        if (s.UseColorBackground)
        {
            s.UseBackgroundImage = false;
            s.UseBackgroundVideo = false;
            s.BackgroundSpriteName = string.Empty;
            s.BackgroundVideoName = string.Empty;
            return;
        }

        // Si l’utilisateur veut une vidéo, il faut une vidéo valide, sinon on coupe
        if (s.UseBackgroundVideo)
        {
            if (!hasVideo)
            {
                s.UseBackgroundVideo = false;
                s.BackgroundVideoName = string.Empty;
            }
            s.UseBackgroundImage = false;
            s.BackgroundSpriteName = string.Empty;
            return;
        }

        // Si l’utilisateur veut une image, il faut une image valide, sinon on coupe
        if (s.UseBackgroundImage)
        {
            if (!hasImage)
            {
                s.UseBackgroundImage = false;
                s.BackgroundSpriteName = string.Empty;
            }
            s.UseBackgroundVideo = false;
            s.BackgroundVideoName = string.Empty;
            return;
        }

        // Sinon rien => “aucun mode explicite”, on nettoie
        s.UseBackgroundImage = false;
        s.UseBackgroundVideo = false;
        s.BackgroundSpriteName = string.Empty;
        s.BackgroundVideoName = string.Empty;
    }



    public static string ImportBackgroundVideoFromGallery(string sourcePath)
    {
        string targetDir = Path.Combine(Application.persistentDataPath, "Backgrounds");

        if (!Directory.Exists(targetDir))
            Directory.CreateDirectory(targetDir);

        string fileName = "bg_" + DateTime.Now.Ticks + ".mp4";
        string destPath = Path.Combine(targetDir, fileName);

        File.Copy(sourcePath, destPath, true);

        return fileName;
    }
    
    public static IEnumerator ImportImageiOS(string sourcePath, Action<string> onFinished)
    {
        string url = "file://" + sourcePath;

        using (UnityWebRequest uwr = UnityWebRequest.Get(url))
        {
            yield return uwr.SendWebRequest();

            if (uwr.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("UWR failed: " + uwr.error);
                yield break;
            }

            byte[] data = uwr.downloadHandler.data;

            string dir = Path.Combine(Application.persistentDataPath, "Backgrounds");
            Directory.CreateDirectory(dir);

            string fileName = "bg_" + DateTime.Now.Ticks + ".png";
            string destPath = Path.Combine(dir, fileName);

            File.WriteAllBytes(destPath, data);

            Debug.Log("Image imported via UWR: " + destPath);

            // maintenant tu peux charger normalement depuis destPath
            onFinished?.Invoke(fileName);
        }
    }

    public static IEnumerator ImportVideoiOS(string sourcePath, Action<string> onFinished)
    {
        string url = "file://" + sourcePath;

        using (UnityWebRequest uwr = UnityWebRequest.Get(url))
        {
            yield return uwr.SendWebRequest();

            if (uwr.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("UWR failed: " + uwr.error);
                yield break;
            }

            byte[] data = uwr.downloadHandler.data;

            string dir = Path.Combine(Application.persistentDataPath, "Backgrounds");
            Directory.CreateDirectory(dir);

            string fileName = "bg_" + DateTime.Now.Ticks + ".mp4";
            string destPath = Path.Combine(dir, fileName);

            File.WriteAllBytes(destPath, data);

            onFinished?.Invoke(fileName);
        }
    }

    public static string ImportBallMeshFromGallery(string sourcePath)
    {
        if (string.IsNullOrEmpty(sourcePath) || !File.Exists(sourcePath))
            return string.Empty;

        string targetDir = Path.Combine(Application.persistentDataPath, "BallMeshes");

        if (!Directory.Exists(targetDir))
            Directory.CreateDirectory(targetDir);

        string extension = Path.GetExtension(sourcePath);
        if (string.IsNullOrEmpty(extension))
            extension = ".obj";

        string fileName = "ball_" + DateTime.Now.Ticks + extension;
        string destPath = Path.Combine(targetDir, fileName);

        File.Copy(sourcePath, destPath, true);

        return fileName;
    }

    public static bool TryGetBallMesh(string fileName, out Mesh mesh, out Bounds bounds)
    {
        mesh = null;
        bounds = default;

        if (string.IsNullOrEmpty(fileName))
            return false;

        if (cachedBallMeshes.TryGetValue(fileName, out var cached) && cached.Mesh != null)
        {
            mesh = cached.Mesh;
            bounds = cached.Bounds;
            return true;
        }

        string dir = Path.Combine(Application.persistentDataPath, "BallMeshes");
        string fullPath = Path.Combine(dir, fileName);

        if (!File.Exists(fullPath))
        {
            cachedBallMeshes.Remove(fileName);
            return false;
        }

        if (!RuntimeMeshImporter.TryLoadMeshFromFile(fullPath, out mesh, out bounds))
            return false;

        cachedBallMeshes[fileName] = new CachedMeshEntry(mesh, bounds);
        return true;
    }




    private static SkinData EnsureDefault()
    {
        if (currentSkin == null)
        {
            if (skins.Count == 0)
            {
                skins.AddRange(CreateDefaultSkins());
            }

            currentSkin = skins.Count > 0 ? skins[0].Clone() : new SkinData();
        }

        return currentSkin;
    }

    public static void SaveSkin(string name, SkinData edited)
    {
        if (string.IsNullOrWhiteSpace(name))
            name = "Custom Skin";

        var clone = edited.Clone();
        clone.Name = name.Trim();

        NormalizeBackgroundMode(clone);

        var existingIndex = skins.FindIndex(s => s.Name.Equals(clone.Name, StringComparison.OrdinalIgnoreCase));
        if (existingIndex >= 0) skins[existingIndex] = clone;
        else skins.Add(clone);

        currentSkin = clone;
        WriteToPrefs();
        OnSkinChanged?.Invoke(currentSkin);
        Debug.Log(edited.Name);
        Debug.Log(edited.BackgroundColor);
        Debug.Log(edited.UseBackgroundImage);
        Debug.Log(edited.UseBackgroundVideo);
      

    }

    public static void ApplySkin(string name)
    {
        var found = skins.Find(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (found != null)
        {
            var clone = found.Clone();
            NormalizeBackgroundMode(clone);
            currentSkin = clone;
            SaveCurrentSkinSelection(saveNow: true);
            OnSkinChanged?.Invoke(currentSkin);
        }
    }

    public static void UpdateWorkingCopy(SkinData workingCopy)
    {
        var clone = workingCopy.Clone();
        NormalizeBackgroundMode(clone);
        currentSkin = clone;
        OnSkinChanged?.Invoke(currentSkin);
    }

    public static Sprite LoadBackgroundSprite(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return null;

        if (cachedBackgroundSprites.TryGetValue(fileName, out var cachedSprite) && cachedSprite != null)
            return cachedSprite;

        string resourceName = Path.GetFileNameWithoutExtension(fileName);
        var resourceSprite = Resources.Load<Sprite>($"Backgrounds/{resourceName}");
        if (resourceSprite != null)
        {
            cachedBackgroundSprites[fileName] = resourceSprite;
            return resourceSprite;
        }

        string dir = Path.Combine(Application.persistentDataPath, "Backgrounds");
        string fullPath = Path.Combine(dir, fileName);

        if (!File.Exists(fullPath))
        {
            cachedBackgroundSprites.Remove(fileName);
            return null;
        }

        byte[] bytes = File.ReadAllBytes(fullPath);

        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!tex.LoadImage(bytes))
        {
            UnityEngine.Object.Destroy(tex);
            return null;
        }

        var sprite = Sprite.Create(
            tex,
            new Rect(0, 0, tex.width, tex.height),
            new Vector2(0.5f, 0.5f)
        );
        cachedBackgroundSprites[fileName] = sprite;
        return sprite;
    }

    public static string GetBackgroundVideoPath(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return null;

        string dir = Path.Combine(Application.persistentDataPath, "Backgrounds");
        string fullPath = Path.Combine(dir, fileName);

        return File.Exists(fullPath) ? fullPath : null;
    }


    private static void LoadFromPrefs()
    {
        skins.Clear();

        if (PlayerPrefs.HasKey(StorageKey))
        {
            var raw = PlayerPrefs.GetString(StorageKey);
            try
            {
                var wrapper = JsonUtility.FromJson<SkinWrapper>(raw);
                if (wrapper != null && wrapper.Items != null)
                {
                    skins.AddRange(wrapper.Items);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to load skins: {ex.Message}");
            }
        }

        if (skins.Count == 0)
            skins.AddRange(CreateDefaultSkins());

        // Normalise tout ce qui a été chargé
        for (int i = 0; i < skins.Count; i++)
            NormalizeBackgroundMode(skins[i]);

        var preferredName = PlayerPrefs.GetString(CurrentSkinKey, string.Empty);
        var preferredSkin = skins.Find(s => s.Name.Equals(preferredName, StringComparison.OrdinalIgnoreCase));
        currentSkin = (preferredSkin ?? skins[0]).Clone();
        NormalizeBackgroundMode(currentSkin);
    }


    private static void WriteToPrefs()
    {
        var wrapper = new SkinWrapper { Items = skins.ToArray() };
        var raw = JsonUtility.ToJson(wrapper);
        PlayerPrefs.SetString(StorageKey, raw);
        SaveCurrentSkinSelection(saveNow: false);
        PlayerPrefs.Save();
    }

    private static void SaveCurrentSkinSelection(bool saveNow)
    {
        if (currentSkin == null || string.IsNullOrWhiteSpace(currentSkin.Name))
            return;

        PlayerPrefs.SetString(CurrentSkinKey, currentSkin.Name);
        if (saveNow)
            PlayerPrefs.Save();
    }

    [Serializable]
    private class SkinWrapper
    {
        public SkinData[] Items;
    }

    private class CachedMeshEntry
    {
        public Mesh Mesh { get; }
        public Bounds Bounds { get; }

        public CachedMeshEntry(Mesh mesh, Bounds bounds)
        {
            Mesh = mesh;
            Bounds = bounds;
        }
    }

    private static List<SkinData> CreateDefaultSkins()
    {
        var skins = new List<SkinData>
        {
            new SkinData
            {
                Name = "Classic",
                BallColor = new Color(1f, 0.9f, 0.2f),
                BallSize = 0.4f,
                BackgroundColor = new Color(0.12f, 0.38f, 0.9f),
                UseColorBackground = true
            },
            new SkinData
            {
                Name = "Sakura Blossom",
                BallColor = new Color(0.85f, 0.35f, 0.45f),
                BallSize = 1f,
                BackgroundColor = new Color(0.96f, 0.75f, 0.75f),
                BackgroundSpriteName = "sakura_blossom",
                UseBackgroundImage = true
            },
            new SkinData
            {
                Name = "Cosy Plush",
                BallColor = new Color(0.87f, 0.78f, 0.64f),
                BallSize = 1f,
                BackgroundColor = new Color(0.24f, 0.14f, 0.08f),
                BackgroundSpriteName = "cosy_plush",
                UseBackgroundImage = true
            },
            new SkinData
            {
                Name = "Acid Funk",
                BallColor = new Color(0.99f, 0.2f, 0.62f),
                BallSize = 1f,
                BackgroundColor = new Color(0.83f, 0.9f, 0.35f),
                BackgroundSpriteName = "acid_funk",
                UseBackgroundImage = true
            }
        };

        for (int i = 0; i < skins.Count; i++)
            NormalizeBackgroundMode(skins[i]);

        return skins;
    }
}
