using ChargeRush.Core;
using ChargeRush.Save;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChargeRush.UI
{
    public sealed class AchievementsScreenController : MonoBehaviour
    {
        [SerializeField] private Button backButton;
        [SerializeField] private TextMeshProUGUI progressLabel;
        [SerializeField] private AchievementCardView[] cards;

        private void Start()
        {
            if (backButton != null)
            {
                backButton.onClick.AddListener(() => SceneLoader.Instance.Load(SceneLoader.MainMenuScene));
            }

            RefreshCards();
        }

        private void RefreshCards()
        {
            if (cards == null)
            {
                return;
            }

            var unlockedCount = 0;
            for (var i = 0; i < cards.Length; i++)
            {
                var card = cards[i];
                if (card == null)
                {
                    continue;
                }

                var unlocked = SaveManager.Instance != null
                    && SaveManager.Instance.HasAchievement(card.AchievementId);
                card.SetUnlocked(unlocked);
                if (unlocked)
                {
                    unlockedCount++;
                }
            }
            if (progressLabel != null)
            {
                progressLabel.text = $"{unlockedCount} / {cards.Length}  UNLOCKED";
            }
        }
    }
}
