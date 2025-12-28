using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gère un système simple de récompenses basé sur le nombre de victoires.
/// Les paliers sont pensés pour un flow mobile : nouvelles skins, accès aux éditeurs
/// et bonus consommables (skip de niveaux de départ).
/// </summary>
public static class RewardSystem
{
    private const string TotalWinsKey = "rewards.totalWins";
    private const string SkipStockKey = "rewards.skipStock";
    private const int SkipBonusLevels = 10;

    private static readonly List<RewardDefinition> rewardPlan = new List<RewardDefinition>
    {
        new RewardDefinition(
            id: "starter_skin",
            winsRequired: 3,
            type: RewardType.Skin,
            displayName: "Skin flash"),
        new RewardDefinition(
            id: "skin_editor",
            winsRequired: 5,
            type: RewardType.Feature,
            displayName: "Skin Editor"),
        new RewardDefinition(
            id: "level_editor",
            winsRequired: 8,
            type: RewardType.Feature,
            displayName: "Level Editor"),
        new RewardDefinition(
            id: "skip_bonus",
            winsRequired: 10,
            type: RewardType.Consumable,
            displayName: "Skip x10 niveaux",
            quantity: 1,
            skipLevels: SkipBonusLevels),
        new RewardDefinition(
            id: "legendary_skin",
            winsRequired: 15,
            type: RewardType.Skin,
            displayName: "Skin stellaire")
    };

    public static int TotalWins => PlayerPrefs.GetInt(TotalWinsKey, 0);
    public static int SkipBonusesAvailable => PlayerPrefs.GetInt(SkipStockKey, 0);

    public static bool SkinEditorUnlocked => IsUnlocked("skin_editor");
    public static bool LevelEditorUnlocked => IsUnlocked("level_editor");

    public static IReadOnlyList<RewardDefinition> RewardPlan => rewardPlan;

    /// <summary>
    /// À appeler quand un niveau est gagné. Retourne la liste des récompenses débloquées.
    /// </summary>
    public static List<RewardDefinition> RegisterWin()
    {
        int wins = TotalWins + 1;
        PlayerPrefs.SetInt(TotalWinsKey, wins);

        List<RewardDefinition> unlocked = new List<RewardDefinition>();

        foreach (var reward in rewardPlan)
        {
            if (wins < reward.WinsRequired || IsUnlocked(reward.Id))
            {
                continue;
            }

            Unlock(reward);
            unlocked.Add(reward);
        }

        PlayerPrefs.Save();
        return unlocked;
    }

    public static RewardDefinition GetNextReward()
    {
        foreach (var reward in rewardPlan)
        {
            if (!IsUnlocked(reward.Id))
            {
                return reward;
            }
        }

        return null;
    }

    /// <summary>
    /// Consomme un bonus de skip. Retourne le niveau de départ obtenu.
    /// </summary>
    public static bool TryConsumeSkipBonus(out int targetLevel)
    {
        int stock = SkipBonusesAvailable;
        if (stock <= 0)
        {
            targetLevel = 0;
            return false;
        }

        stock--;
        PlayerPrefs.SetInt(SkipStockKey, stock);
        PlayerPrefs.Save();

        targetLevel = SkipBonusLevels;
        return true;
    }

    private static void Unlock(RewardDefinition reward)
    {
        PlayerPrefs.SetInt(GetUnlockKey(reward.Id), 1);

        switch (reward.Type)
        {
            case RewardType.Skin:
                GrantSkinReward(reward.Id);
                break;
            case RewardType.Consumable:
                AddSkipBonus(reward.Quantity);
                break;
        }
    }

    private static bool IsUnlocked(string rewardId)
    {
        return PlayerPrefs.GetInt(GetUnlockKey(rewardId), 0) == 1;
    }

    private static string GetUnlockKey(string rewardId)
    {
        return $"rewards.unlocked.{rewardId}";
    }

    private static void GrantSkinReward(string rewardId)
    {
        SkinData skin = rewardId switch
        {
            "starter_skin" => new SkinData
            {
                Name = "Flash",
                BallColor = new Color(0.95f, 0.4f, 1f),
                BackgroundColor = new Color(0.1f, 0.1f, 0.1f)
            },
            "legendary_skin" => new SkinData
            {
                Name = "Stellar",
                BallColor = new Color(0.35f, 0.8f, 1f),
                BallSize = 1.1f,
                BackgroundColor = new Color(0.02f, 0.05f, 0.08f)
            },
            _ => null
        };

        if (skin != null)
        {
            // Ajoute la skin au catalogue utilisateur. SaveSkin règle la persistance.
            SkinManager.SaveSkin(skin.Name, skin);
        }
    }

    private static void AddSkipBonus(int quantity)
    {
        int stock = SkipBonusesAvailable + Math.Max(1, quantity);
        PlayerPrefs.SetInt(SkipStockKey, stock);
    }
}

public enum RewardType
{
    Skin,
    Feature,
    Consumable
}

public class RewardDefinition
{
    public string Id { get; }
    public int WinsRequired { get; }
    public RewardType Type { get; }
    public string DisplayName { get; }
    public int Quantity { get; }
    public int SkipLevels { get; }

    public RewardDefinition(string id, int winsRequired, RewardType type, string displayName, int quantity = 1, int skipLevels = 0)
    {
        Id = id;
        WinsRequired = winsRequired;
        Type = type;
        DisplayName = displayName;
        Quantity = Math.Max(1, quantity);
        SkipLevels = skipLevels;
    }
}
