using System;
using ChargeRush.Data;
using ChargeRush.Save;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChargeRush.UI
{
    /// <summary>Serialized view for one authored story-level / challenge card.</summary>
    public sealed class LevelSelectCardView : MonoBehaviour
    {
        [SerializeField] private string levelId;
        [SerializeField] private Button button;
        [SerializeField] private Image cardBackground;
        [SerializeField] private GameObject lockedOverlay;
        [SerializeField] private TextMeshProUGUI starsLabel;
        [SerializeField] private TextMeshProUGUI titleLabel;
        [SerializeField] private TextMeshProUGUI targetLabel;
        [SerializeField] private TextMeshProUGUI levelNumberLabel;
        [SerializeField] private Image thumbnailImage;
        [Header("State Colors")]
        [SerializeField] private Color unlockedCardColor = new Color(1f, 0.87f, 0.58f, 0.98f);
        [SerializeField] private Color lockedCardColor = new Color(0.38f, 0.34f, 0.3f, 0.98f);

        public string LevelId => levelId;

        private void Awake()
        {
            ResolveLabels();
        }

        public void Bind(LevelData level, LevelProgressRecord record, bool unlocked, Action<LevelData> onSelected)
        {
            ResolveLabels();
            gameObject.SetActive(true);

            if (titleLabel != null && level != null)
            {
                titleLabel.text = level.LevelName;
            }

            if (targetLabel != null && level != null)
            {
                targetLabel.text = $"Target: {level.TargetEarnings} Credits";
            }

            if (levelNumberLabel != null && level != null)
            {
                levelNumberLabel.text = level.LevelNumber.ToString();
            }

            ApplyThumbnail(level);
            ApplyState(unlocked, unlocked ? $"{record.Stars} / 3 STARS" : "LOCKED");

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.interactable = unlocked;
                if (unlocked)
                {
                    button.onClick.AddListener(() => onSelected?.Invoke(level));
                }
            }
        }

        public void BindChallenge(ChallengeData challenge, bool completed, int displayNumber, Action<ChallengeData> onSelected)
        {
            ResolveLabels();
            gameObject.SetActive(true);

            if (titleLabel != null && challenge != null)
            {
                titleLabel.text = challenge.DisplayName;
            }

            if (targetLabel != null && challenge != null)
            {
                targetLabel.text = challenge.Description;
            }

            if (levelNumberLabel != null)
            {
                levelNumberLabel.text = displayNumber.ToString();
            }

            ApplyThumbnail(challenge != null ? challenge.LinkedLevel : null);
            ApplyState(true, completed ? "DONE" : "PLAY");

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.interactable = true;
                button.onClick.AddListener(() => onSelected?.Invoke(challenge));
            }
        }

        public void Hide()
        {
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.interactable = false;
            }

            gameObject.SetActive(false);
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

        private void ApplyState(bool unlocked, string statusText)
        {
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
                starsLabel.text = statusText;
            }
        }

        private void ApplyThumbnail(LevelData level)
        {
            if (thumbnailImage == null || level == null)
            {
                return;
            }

            thumbnailImage.sprite = level.BackgroundSprite;
            thumbnailImage.color = level.BackgroundSprite != null ? Color.white : level.BackgroundTint;
        }

        private void ResolveLabels()
        {
            if (titleLabel == null)
            {
                var title = transform.Find("Title");
                if (title != null)
                {
                    titleLabel = title.GetComponent<TextMeshProUGUI>();
                }
            }

            if (targetLabel == null)
            {
                var target = transform.Find("Target");
                if (target != null)
                {
                    targetLabel = target.GetComponent<TextMeshProUGUI>();
                }
            }

            if (levelNumberLabel == null)
            {
                var number = transform.Find("LevelNumber");
                if (number != null)
                {
                    levelNumberLabel = number.GetComponent<TextMeshProUGUI>();
                }
            }

            if (thumbnailImage == null)
            {
                var thumb = transform.Find("Thumbnail");
                if (thumb != null)
                {
                    thumbnailImage = thumb.GetComponent<Image>();
                }
            }
        }
    }
}
