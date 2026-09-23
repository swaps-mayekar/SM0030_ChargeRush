using ChargeRush.Core;
using ChargeRush.Data;
using ChargeRush.Progression;
using ChargeRush.Save;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChargeRush.UI
{
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI careerText;
        [SerializeField] private TextMeshProUGUI creditsText;
        [SerializeField] private Button playButton;
        [SerializeField] private Button levelSelectButton;
        [SerializeField] private Button challengeButton;
        [SerializeField] private Button endlessButton;
        [SerializeField] private Button achievementsButton;
        [SerializeField] private Button upgradesButton;
        [SerializeField] private GameObject upgradesPanel;
        [SerializeField] private RectTransform upgradesContent;
        [SerializeField] private Button closeUpgradesButton;

        private void Start()
        {
            Refresh();
            if (playButton != null) playButton.onClick.AddListener(OnPlay);
            if (levelSelectButton != null) levelSelectButton.onClick.AddListener(() => SceneLoader.Instance.Load(SceneLoader.LevelSelectScene));
            if (challengeButton != null) challengeButton.onClick.AddListener(OnChallenge);
            if (endlessButton != null) endlessButton.onClick.AddListener(OnEndless);
            if (achievementsButton != null) achievementsButton.onClick.AddListener(() => SceneLoader.Instance.Load(SceneLoader.AchievementsScene));
            if (upgradesButton != null) upgradesButton.onClick.AddListener(OpenUpgrades);
            if (closeUpgradesButton != null) closeUpgradesButton.onClick.AddListener(() => upgradesPanel.SetActive(false));
        }

        private void Refresh()
        {
            CareerProgression.RefreshFromSave();
            if (SaveManager.Instance == null)
            {
                return;
            }

            if (careerText != null)
            {
                careerText.text = SaveManager.Instance.Data.CareerRank;
            }

            if (creditsText != null)
            {
                creditsText.text = CreditUi.Format(SaveManager.Instance.Data.TotalCredits);
                CreditUi.EnsureTrailingIcon(creditsText, 48f);
            }
        }

        private void OnPlay()
        {
            var catalog = GameBootstrap.Instance != null ? GameBootstrap.Instance.Catalog : null;
            if (catalog == null || catalog.StoryLevels.Count == 0)
            {
                return;
            }

            LevelData next = catalog.StoryLevels[0];
            if (SaveManager.Instance != null)
            {
                for (var i = 0; i < catalog.StoryLevels.Count; i++)
                {
                    var level = catalog.StoryLevels[i];
                    var record = SaveManager.Instance.GetOrCreateLevel(level.LevelId);
                    next = level;
                    if (!record.Completed)
                    {
                        break;
                    }
                }
            }

            GameBootstrap.Instance.PlayStoryLevel(next);
        }

        private void OnChallenge()
        {
            var catalog = GameBootstrap.Instance != null ? GameBootstrap.Instance.Catalog : null;
            if (catalog == null || catalog.Challenges.Count == 0)
            {
                return;
            }

            // First completed story level challenge, else first challenge.
            GameBootstrap.Instance.PlayChallenge(catalog.Challenges[0]);
        }

        private void OnEndless()
        {
            var catalog = GameBootstrap.Instance != null ? GameBootstrap.Instance.Catalog : null;
            if (catalog == null || catalog.StoryLevels.Count == 0)
            {
                return;
            }

            var level = catalog.StoryLevels[Mathf.Min(4, catalog.StoryLevels.Count - 1)];
            GameBootstrap.Instance.PlayEndless(level);
        }

        private void OpenUpgrades()
        {
            if (upgradesPanel != null)
            {
                upgradesPanel.SetActive(true);
            }

            BuildUpgradeList();
        }

        private void BuildUpgradeList()
        {
            if (upgradesContent == null || GameBootstrap.Instance == null || GameBootstrap.Instance.Catalog == null)
            {
                return;
            }

            var listRoot = UiScrollList.Ensure(upgradesContent, new Vector2(640f, 280f), new Vector2(0f, 40f));
            UiScrollList.ClearRows(listRoot);

            var upgrades = GameBootstrap.Instance.Catalog.Upgrades;
            var manager = FindFirstObjectByType<UpgradeManager>();
            if (manager == null)
            {
                var host = new GameObject("UpgradeManagerTemp");
                manager = host.AddComponent<UpgradeManager>();
                manager.SetCatalog(GameBootstrap.Instance.Catalog);
            }

            for (var i = 0; i < upgrades.Count; i++)
            {
                var upgrade = upgrades[i];
                var level = SaveManager.Instance != null ? SaveManager.Instance.GetUpgradeLevel(upgrade.UpgradeId) : 0;
                var cost = level < upgrade.MaxLevel ? upgrade.GetCost(level + 1) : -1;
                var label = UiScrollList.CreateTextRow(
                    listRoot,
                    $"Upgrade_{upgrade.UpgradeId}",
                    cost > 0
                        ? $"{upgrade.DisplayName} Lv {level}/{upgrade.MaxLevel} - {CreditUi.Format(cost)}"
                        : $"{upgrade.DisplayName} MAX",
                    26f,
                    48f,
                    UiTextRole.Heading);
                PanelTheme.ApplyText(label, PanelTheme.BodyColor);

                if (cost > 0)
                {
                    CreditUi.EnsureIconAfterText(label, 34f);
                }

                var button = label.gameObject.AddComponent<Button>();
                var captured = upgrade;
                button.onClick.AddListener(() =>
                {
                    if (manager.TryPurchase(captured))
                    {
                        Refresh();
                        BuildUpgradeList();
                    }
                });
            }
        }

    }
}
