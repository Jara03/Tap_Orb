using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public static class SkinManager
{
    private const string StorageKey = "skins.v1";

    public static event Action<SkinData> OnSkinChanged;

    private static readonly List<SkinData> skins = new List<SkinData>();
    private static SkinData currentSkin;

    public static IReadOnlyList<SkinData> Skins => skins;
    public static SkinData CurrentSkin => currentSkin ?? EnsureDefault();

    static SkinManager()
    {
        LoadFromPrefs();
    }
     
    public static void ImportBackgroundFromGallery(string sourcePath)
    {
        string targetDir = Path.Combine(Application.persistentDataPath, "Backgrounds");

        if (!Directory.Exists(targetDir))
            Directory.CreateDirectory(targetDir);

        string fileName = "bg_" + DateTime.Now.Ticks + ".png";
        string destPath = Path.Combine(targetDir, fileName);
       // Debug.LogError("Persistent path: " + destPath);


        File.Copy(sourcePath, destPath, true);
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

        // Cas où l'UI met tout dans BackgroundSpriteName (dropdown unique) :
        // si c'est une vidéo, on migre vers BackgroundVideoName.
        if (!string.IsNullOrEmpty(s.BackgroundSpriteName) && IsVideoFile(s.BackgroundSpriteName))
        {
            s.BackgroundVideoName = s.BackgroundSpriteName;
            s.BackgroundSpriteName = string.Empty;
        }

        // Cas inverse (rare) : image rangée dans VideoName
        if (!string.IsNullOrEmpty(s.BackgroundVideoName) && IsImageFile(s.BackgroundVideoName))
        {
            s.BackgroundSpriteName = s.BackgroundVideoName;
            s.BackgroundVideoName = string.Empty;
        }

        bool hasVideo = !string.IsNullOrEmpty(s.BackgroundVideoName) && IsVideoFile(s.BackgroundVideoName);
        bool hasImage = !string.IsNullOrEmpty(s.BackgroundSpriteName) && IsImageFile(s.BackgroundSpriteName);

        if (s.UseColorBackground)
        {
            s.UseBackgroundVideo = false;
            s.UseBackgroundImage = false;
            return;
        }

        if (hasVideo)
        {
            s.UseBackgroundVideo = true;
            s.UseBackgroundImage = false;
            return;
        }

        if (hasImage)
        {
            s.UseBackgroundImage = true;
            s.UseBackgroundVideo = false;
            return;
        }

        // Aucun fichier valide => fond couleur
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
    
    public static IEnumerator ImportImageiOS(string sourcePath)
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

        string dir = Path.Combine(Application.persistentDataPath, "BallMeshes");
        string fullPath = Path.Combine(dir, fileName);

        return RuntimeMeshImporter.TryLoadMeshFromFile(fullPath, out mesh, out bounds);
    }




    private static SkinData EnsureDefault()
    {
        if (currentSkin == null)
        {
            currentSkin = new SkinData();
            if (skins.Count == 0)
            {
                skins.Add(currentSkin.Clone());
            }
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

        string dir = Path.Combine(Application.persistentDataPath, "Backgrounds");
        string fullPath = Path.Combine(dir, fileName);

        if (!File.Exists(fullPath))
            return null;

        byte[] bytes = File.ReadAllBytes(fullPath);

        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!tex.LoadImage(bytes))
            return null;

        return Sprite.Create(
            tex,
            new Rect(0, 0, tex.width, tex.height),
            new Vector2(0.5f, 0.5f)
        );
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
            skins.Add(new SkinData());

        // Normalise tout ce qui a été chargé
        for (int i = 0; i < skins.Count; i++)
            NormalizeBackgroundMode(skins[i]);

        currentSkin = skins[0].Clone();
        NormalizeBackgroundMode(currentSkin);
    }


    private static void WriteToPrefs()
    {
        var wrapper = new SkinWrapper { Items = skins.ToArray() };
        var raw = JsonUtility.ToJson(wrapper);
        PlayerPrefs.SetString(StorageKey, raw);
        PlayerPrefs.Save();
    }

    [Serializable]
    private class SkinWrapper
    {
        public SkinData[] Items;
    }
}
