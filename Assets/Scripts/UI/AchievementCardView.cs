using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChargeRush.UI
{
    /// <summary>Serialized view for one authored achievement card.</summary>
    public sealed class AchievementCardView : MonoBehaviour
    {
        [SerializeField] private string achievementId;
        [SerializeField] private Image cardBackground;
        [SerializeField] private Image icon;
        [SerializeField] private TextMeshProUGUI titleLabel;
        [SerializeField] private TextMeshProUGUI objectiveLabel;
        [SerializeField] private TextMeshProUGUI statusLabel;
        [Header("State Colors")]
        [SerializeField] private Color unlockedCardColor = new Color(1f, 0.87f, 0.58f, 0.98f);
        [SerializeField] private Color lockedCardColor = new Color(0.46f, 0.37f, 0.29f, 0.94f);
        [SerializeField] private Color unlockedTitleColor = new Color(0.2f, 0.1f, 0.045f, 1f);
        [SerializeField] private Color lockedTitleColor = Color.white;
        [SerializeField] private Color unlockedObjectiveColor = new Color(0.28f, 0.16f, 0.08f, 1f);
        [SerializeField] private Color lockedObjectiveColor = new Color(0.84f, 0.78f, 0.68f, 1f);
        [SerializeField] private Color unlockedStatusColor = new Color(0.1f, 0.42f, 0.2f, 1f);
        [SerializeField] private Color lockedStatusColor = new Color(0.95f, 0.67f, 0.27f, 1f);

        public string AchievementId => achievementId;

        public void SetUnlocked(bool unlocked)
        {
            if (cardBackground != null)
            {
                cardBackground.color = unlocked ? unlockedCardColor : lockedCardColor;
            }

            if (icon != null)
            {
                icon.color = unlocked ? Color.white : new Color(0.4f, 0.4f, 0.4f, 0.92f);
            }

            if (titleLabel != null)
            {
                titleLabel.color = unlocked ? unlockedTitleColor : lockedTitleColor;
            }

            if (objectiveLabel != null)
            {
                objectiveLabel.color = unlocked ? unlockedObjectiveColor : lockedObjectiveColor;
            }

            if (statusLabel != null)
            {
                statusLabel.text = unlocked ? "UNLOCKED" : "LOCKED";
                statusLabel.color = unlocked ? unlockedStatusColor : lockedStatusColor;
            }
        }

        public void Assign(
            string id,
            Image background,
            Image iconImage,
            TextMeshProUGUI title,
            TextMeshProUGUI objective,
            TextMeshProUGUI status)
        {
            achievementId = id;
            cardBackground = background;
            icon = iconImage;
            titleLabel = title;
            objectiveLabel = objective;
            statusLabel = status;
        }
    }
}
