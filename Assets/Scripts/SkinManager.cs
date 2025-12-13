using System;
using System.Collections.Generic;
using UnityEngine;

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

    public static Sprite LoadBackgroundSprite(string spriteName)
    {
        if (string.IsNullOrEmpty(spriteName)) return null;
        foreach (var sprite in Resources.LoadAll<Sprite>("UI/HomeScreen"))
        {
            if (sprite.name.Equals(spriteName, StringComparison.OrdinalIgnoreCase))
            {
                return sprite;
            }
        }

        return null;
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
