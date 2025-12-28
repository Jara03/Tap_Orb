using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Petit widget optionnel pour afficher la progression des récompenses sur l'écran d'accueil.
/// </summary>
public class HomeRewardIndicator : MonoBehaviour
{
    [SerializeField] private Text winCounterLabel;
    [SerializeField] private Text nextRewardLabel;
    [SerializeField] private Text skipStockLabel;
    [SerializeField] private Text feedbackLabel;

    [Header("Badges de lock")]
    [SerializeField] private GameObject skinEditorLockedBadge;
    [SerializeField] private GameObject levelEditorLockedBadge;

    public void Start()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (winCounterLabel != null)
        {
            winCounterLabel.text = $"Victoires : {RewardSystem.TotalWins}";
        }

        if (skipStockLabel != null)
        {
            skipStockLabel.text = $"Bonus skip restants : {RewardSystem.SkipBonusesAvailable}";
        }

        var next = RewardSystem.GetNextReward();
        if (nextRewardLabel != null)
        {
            if (next != null)
            {
                nextRewardLabel.text = $"Prochain palier ({next.WinsRequired}) : {next.DisplayName}";
            }
            else
            {
                nextRewardLabel.text = "Tous les paliers sont débloqués";
            }
        }

        if (skinEditorLockedBadge != null)
        {
            skinEditorLockedBadge.SetActive(!RewardSystem.SkinEditorUnlocked);
        }

        if (levelEditorLockedBadge != null)
        {
            levelEditorLockedBadge.SetActive(!RewardSystem.LevelEditorUnlocked);
        }
    }

    /// <summary>
    /// Bouton rapide pour utiliser un bonus de skip de départ.
    /// </summary>
    public void UseSkipBonus()
    {
        StringBuilder builder = new StringBuilder();

        if (RewardSystem.TryConsumeSkipBonus(out int targetLevel))
        {
            LevelManager.levelSelected = Mathf.Max(LevelManager.levelSelected, targetLevel);
            builder.Append($"Skip appliqué : reprise au niveau {LevelManager.levelSelected}");
        }
        else
        {
            builder.Append("Pas de bonus disponible");
        }

        if (feedbackLabel != null)
        {
            feedbackLabel.text = builder.ToString();
        }

        Refresh();
    }
}
