using System;
using ChargeRush.Data;
using ChargeRush.Save;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChargeRush.UI
{
    /// <summary>Serialized view for one authored story-level card.</summary>
    public sealed class LevelSelectCardView : MonoBehaviour
    {
        [SerializeField] private string levelId;
        [SerializeField] private Button button;
        [SerializeField] private Image cardBackground;
        [SerializeField] private GameObject lockedOverlay;
        [SerializeField] private TextMeshProUGUI starsLabel;
        [Header("State Colors")]
        [SerializeField] private Color unlockedCardColor = new Color(1f, 0.87f, 0.58f, 0.98f);
        [SerializeField] private Color lockedCardColor = new Color(0.38f, 0.34f, 0.3f, 0.98f);

        public string LevelId => levelId;

        public void Bind(LevelData level, LevelProgressRecord record, bool unlocked, Action<LevelData> onSelected)
        {
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.interactable = unlocked;
                if (unlocked)
                {
                    button.onClick.AddListener(() => onSelected?.Invoke(level));
                }
            }

            if (cardBackground != null)
            {
                cardBackground.color = unlocked ? unlockedCardColor : lockedCardColor;
            }

            if (lockedOverlay != null)
            {
                lockedOverlay.SetActive(!unlocked);
            }

            if (starsLabel != null)
            {
                starsLabel.text = unlocked ? $"{record.Stars} / 3 STARS" : "LOCKED";
            }
        }

        public void Assign(
            string id,
            Button cardButton,
            Image background,
            GameObject overlay,
            TextMeshProUGUI stars)
        {
            levelId = id;
            button = cardButton;
            cardBackground = background;
            lockedOverlay = overlay;
            starsLabel = stars;
        }
    }
}
