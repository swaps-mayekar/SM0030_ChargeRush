using System.Collections.Generic;
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
            if (levelSelectButton != null) levelSelectButton.onClick.AddListener(OnLevelSelect);
            if (challengeButton != null) challengeButton.onClick.AddListener(OnChallengeSelect);
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

        private void OnLevelSelect()
        {
            if (GameBootstrap.Instance != null)
            {
                GameBootstrap.Instance.OpenLevelSelect();
            }
        }

        private void OnChallengeSelect()
        {
            if (GameBootstrap.Instance != null)
            {
                GameBootstrap.Instance.OpenChallengeSelect();
            }
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

            // Keep scene-authored upgradesContent size/position (do not pass viewport overrides).
            var listRoot = UiScrollList.Ensure(upgradesContent);
            UiScrollList.ClearRows(listRoot);

            var upgrades = GameBootstrap.Instance.Catalog.Upgrades;
            var manager = FindFirstObjectByType<UpgradeManager>();
            if (manager == null)
            {
                var host = new GameObject("UpgradeManagerTemp");
                manager = host.AddComponent<UpgradeManager>();
                manager.SetCatalog(GameBootstrap.Instance.Catalog);
            }

            const float iconSize = 34f;
            const float iconGap = 8f;
            var iconLabels = new List<TextMeshProUGUI>();
            var rowLayouts = new List<LayoutElement>();
            var columnWidth = 0f;

            for (var i = 0; i < upgrades.Count; i++)
            {
                var upgrade = upgrades[i];
                var level = SaveManager.Instance != null ? SaveManager.Instance.GetUpgradeLevel(upgrade.UpgradeId) : 0;
                var cost = level < upgrade.MaxLevel ? upgrade.GetCost(level + 1) : -1;
                var hasCost = cost > 0;
                var label = UiScrollList.CreateTextRow(
                    listRoot,
                    $"Upgrade_{upgrade.UpgradeId}",
                    hasCost
                        ? $"{upgrade.DisplayName} Lv {level}/{upgrade.MaxLevel} - {CreditUi.Format(cost)}"
                        : $"{upgrade.DisplayName} MAX",
                    26f,
                    48f,
                    UiTextRole.Heading,
                    hasCost ? iconSize + iconGap : 0f);
                PanelTheme.ApplyText(label, PanelTheme.BodyColor);

                var layoutElement = label.GetComponent<LayoutElement>();
                rowLayouts.Add(layoutElement);
                columnWidth = Mathf.Max(columnWidth, layoutElement.preferredWidth);

                if (hasCost)
                {
                    iconLabels.Add(label);
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

            // Shared width keeps a left-aligned column centered over the Close button.
            for (var i = 0; i < rowLayouts.Count; i++)
            {
                rowLayouts[i].minWidth = columnWidth;
                rowLayouts[i].preferredWidth = columnWidth;
            }

            if (listRoot is RectTransform listRect)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(listRect);
            }

            for (var i = 0; i < iconLabels.Count; i++)
            {
                CreditUi.EnsureIconAfterText(iconLabels[i], iconSize, iconGap);
            }
        }
    }
}
