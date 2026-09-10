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
        [SerializeField] private Transform contentRoot;
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

            for (var i = contentRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(contentRoot.GetChild(i).gameObject);
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

                var row = new GameObject($"Level_{level.LevelNumber}", typeof(RectTransform));
                row.transform.SetParent(contentRoot, false);
                var text = row.AddComponent<TextMeshProUGUI>();
                text.fontSize = 30f;
                text.text = unlocked
                    ? $"{level.LevelNumber}. {level.LevelName}  Stars {record.Stars}/3"
                    : $"{level.LevelNumber}. Locked";

                var button = row.AddComponent<Button>();
                button.interactable = unlocked;
                var captured = level;
                button.onClick.AddListener(() => GameBootstrap.Instance.PlayStoryLevel(captured));
            }
        }
    }
}
