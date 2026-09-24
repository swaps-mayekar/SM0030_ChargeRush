using ChargeRush.Core;
using ChargeRush.Data;
using ChargeRush.Save;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChargeRush.UI
{
    public sealed class LevelSelectController : MonoBehaviour
    {
        [SerializeField] private Button backButton;
        [SerializeField] private LevelSelectCardView[] cards;
        [SerializeField] private TextMeshProUGUI subtitleLabel;

        private void Start()
        {
            if (backButton != null)
            {
                backButton.onClick.AddListener(() => SceneLoader.Instance.Load(SceneLoader.MainMenuScene));
            }

            ResolveSubtitle();
            RefreshCards();
        }

        private void RefreshCards()
        {
            if (cards == null || GameBootstrap.Instance == null || GameBootstrap.Instance.Catalog == null)
            {
                return;
            }

            if (GameBootstrap.Instance.SelectBrowseMode == GameMode.Challenge)
            {
                RefreshChallengeCards();
            }
            else
            {
                RefreshStoryCards();
            }
        }

        private void RefreshStoryCards()
        {
            SetSubtitle("Choose your next event");

            var levels = GameBootstrap.Instance.Catalog.StoryLevels;
            var previousCompleted = true;
            for (var i = 0; i < levels.Count; i++)
            {
                var level = levels[i];
                var record = SaveManager.Instance != null
                    ? SaveManager.Instance.GetOrCreateLevel(level.LevelId)
                    : new LevelProgressRecord { LevelId = level.LevelId };
                var unlocked = i == 0 || previousCompleted;
                previousCompleted = record.Completed;

                var card = FindCard(level.LevelId);
                if (card != null)
                {
                    card.Bind(level, record, unlocked, selected => GameBootstrap.Instance.PlayStoryLevel(selected));
                }
            }

            // Keep unused authored slots hidden when fewer levels exist than cards.
            for (var i = 0; i < cards.Length; i++)
            {
                if (cards[i] == null)
                {
                    continue;
                }

                var matched = false;
                for (var l = 0; l < levels.Count; l++)
                {
                    if (cards[i].LevelId == levels[l].LevelId)
                    {
                        matched = true;
                        break;
                    }
                }

                if (!matched)
                {
                    cards[i].Hide();
                }
            }
        }

        private void RefreshChallengeCards()
        {
            SetSubtitle("Choose a challenge");

            var challenges = GameBootstrap.Instance.Catalog.Challenges;
            var completed = SaveManager.Instance != null
                ? SaveManager.Instance.Data.CompletedChallenges
                : null;

            for (var i = 0; i < cards.Length; i++)
            {
                var card = cards[i];
                if (card == null)
                {
                    continue;
                }

                if (i >= challenges.Count || challenges[i] == null)
                {
                    card.Hide();
                    continue;
                }

                var challenge = challenges[i];
                var done = completed != null && completed.Contains(challenge.ChallengeId);
                card.BindChallenge(challenge, done, i + 1, selected => GameBootstrap.Instance.PlayChallenge(selected));
            }
        }

        private LevelSelectCardView FindCard(string levelId)
        {
            for (var i = 0; i < cards.Length; i++)
            {
                if (cards[i] != null && cards[i].LevelId == levelId)
                {
                    return cards[i];
                }
            }

            return null;
        }

        private void SetSubtitle(string text)
        {
            ResolveSubtitle();
            if (subtitleLabel != null)
            {
                subtitleLabel.text = text;
            }
        }

        private void ResolveSubtitle()
        {
            if (subtitleLabel != null)
            {
                return;
            }

            var content = transform.Find("SafeArea/Content/Subtitle");
            if (content == null)
            {
                content = transform.Find("SafeArea")?.Find("Content")?.Find("Subtitle");
            }

            if (content != null)
            {
                subtitleLabel = content.GetComponent<TextMeshProUGUI>();
            }
        }
    }
}
