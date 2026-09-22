using ChargeRush.Core;
using ChargeRush.Data;
using ChargeRush.Save;
using UnityEngine;
using UnityEngine.UI;

namespace ChargeRush.UI
{
    public sealed class LevelSelectController : MonoBehaviour
    {
        [SerializeField] private RectTransform contentRoot;
        [SerializeField] private Button backButton;

        private void Start()
        {
            if (backButton != null)
            {
                backButton.onClick.AddListener(() => SceneLoader.Instance.Load(SceneLoader.MainMenuScene));
            }

            Build();
        }

        private void Build()
        {
            if (contentRoot == null || GameBootstrap.Instance == null || GameBootstrap.Instance.Catalog == null)
            {
                return;
            }

            var listRoot = UiScrollList.Ensure(contentRoot);
            UiScrollList.ClearRows(listRoot);

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

                var label = UiScrollList.CreateTextRow(
                    listRoot,
                    $"Level_{level.LevelNumber}",
                    unlocked
                        ? $"{level.LevelNumber}. {level.LevelName}  Stars {record.Stars}/3"
                        : $"{level.LevelNumber}. Locked",
                    30f,
                    44f,
                    UiTextRole.Heading);

                var button = label.gameObject.AddComponent<Button>();
                button.interactable = unlocked;
                var captured = level;
                button.onClick.AddListener(() => GameBootstrap.Instance.PlayStoryLevel(captured));
            }
        }
    }
}
