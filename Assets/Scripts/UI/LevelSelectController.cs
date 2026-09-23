using ChargeRush.Core;
using ChargeRush.Data;
using ChargeRush.Save;
using UnityEngine;
using UnityEngine.UI;

namespace ChargeRush.UI
{
    public sealed class LevelSelectController : MonoBehaviour
    {
        [SerializeField] private Button backButton;
        [SerializeField] private LevelSelectCardView[] cards;

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
            if (cards == null || GameBootstrap.Instance == null || GameBootstrap.Instance.Catalog == null)
            {
                return;
            }

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
    }
}
