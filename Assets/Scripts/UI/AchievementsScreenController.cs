using ChargeRush.Core;
using ChargeRush.Save;
using UnityEngine;
using UnityEngine.UI;

namespace ChargeRush.UI
{
    public sealed class AchievementsScreenController : MonoBehaviour
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

            var list = GameBootstrap.Instance.Catalog.Achievements;
            for (var i = 0; i < list.Count; i++)
            {
                var achievement = list[i];
                var unlocked = SaveManager.Instance != null && SaveManager.Instance.HasAchievement(achievement.AchievementId);
                UiScrollList.CreateTextRow(
                    listRoot,
                    achievement.AchievementId,
                    $"{(unlocked ? "[Done]" : "[ ]")} {achievement.DisplayName}\n{achievement.Description}",
                    26f,
                    72f);
            }
        }
    }
}
