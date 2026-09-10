using ChargeRush.Core;
using ChargeRush.Save;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChargeRush.UI
{
    public sealed class AchievementsScreenController : MonoBehaviour
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

            var list = GameBootstrap.Instance.Catalog.Achievements;
            for (var i = 0; i < list.Count; i++)
            {
                var achievement = list[i];
                var unlocked = SaveManager.Instance != null && SaveManager.Instance.HasAchievement(achievement.AchievementId);
                var row = new GameObject(achievement.AchievementId, typeof(RectTransform));
                row.transform.SetParent(contentRoot, false);
                var text = row.AddComponent<TextMeshProUGUI>();
                text.fontSize = 28f;
                text.text = $"{(unlocked ? "[Done]" : "[ ]")} {achievement.DisplayName}\n{achievement.Description}";
            }
        }
    }
}
