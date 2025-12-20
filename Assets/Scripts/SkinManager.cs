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
        {
            name = "Custom Skin";
        }

        var clone = edited.Clone();
        clone.Name = name.Trim();

        var existingIndex = skins.FindIndex(s => s.Name.Equals(clone.Name, StringComparison.OrdinalIgnoreCase));
        if (existingIndex >= 0)
        {
            skins[existingIndex] = clone;
        }
        else
        {
            skins.Add(clone);
        }

        currentSkin = clone;
        WriteToPrefs();
        OnSkinChanged?.Invoke(currentSkin);
    }

    public static void ApplySkin(string name)
    {
        var found = skins.Find(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (found != null)
        {
            currentSkin = found.Clone();
            OnSkinChanged?.Invoke(currentSkin);
        }
    }

    public static void UpdateWorkingCopy(SkinData workingCopy)
    {
        currentSkin = workingCopy.Clone();
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
        {
            skins.Add(new SkinData());
        }

        currentSkin = skins[0].Clone();
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
