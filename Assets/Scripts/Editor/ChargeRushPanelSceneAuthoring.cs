using ChargeRush.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ChargeRush.Editor
{
    public static class ChargeRushPanelSceneAuthoring
    {
        private const string GameplayScene = "Assets/Scenes/3_Gameplay.unity";
        private const string MainMenuScene = "Assets/Scenes/1_MainMenu.unity";

        [MenuItem("ChargeRush/Author Panel UI In Scenes")]
        public static void AuthorPanels()
        {
            var previousScene = SceneManager.GetActiveScene().path;
            AuthorGameplayPanels();
            AuthorUpgradesPanel();

            if (!string.IsNullOrEmpty(previousScene))
            {
                EditorSceneManager.OpenScene(previousScene, OpenSceneMode.Single);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("ChargeRush panel UI authored into gameplay and main-menu scenes.");
        }

        private static void AuthorGameplayPanels()
        {
            var scene = EditorSceneManager.OpenScene(GameplayScene, OpenSceneMode.Single);
            var canvas = Object.FindFirstObjectByType<Canvas>();
            var safe = canvas != null ? canvas.transform.Find("SafeArea") : null;
            if (safe == null)
            {
                return;
            }

            ConfigureHudLayout(safe);
            ConfigurePausePanel(safe.Find("PausePanel"));
            ConfigureResultPanel(safe.Find("CompletePanel"), new Vector2(900f, 500f));
            ConfigureResultPanel(safe.Find("FailPanel"), new Vector2(820f, 455f));

            var complete = safe.Find("CompletePanel");
            StyleText(complete?.Find("CompleteTitle"), new Vector2(0f, 184f), new Vector2(650f, 72f), 48f, PanelTheme.TitleColor, FontStyles.Bold);
            StyleText(complete?.Find("CompleteStats"), new Vector2(0f, 64f), new Vector2(690f, 115f), 27f, PanelTheme.BodyColor, FontStyles.Normal, 12f);
            StyleText(complete?.Find("Stars"), new Vector2(0f, -26f), new Vector2(500f, 48f), 20f, PanelTheme.MutedColor, FontStyles.Bold);
            ConfigureStars(complete);
            StyleButton(complete?.Find("Continue"), new Vector2(-155f, -181f), new Vector2(280f, 72f), true);
            StyleButton(complete?.Find("Replay"), new Vector2(155f, -181f), new Vector2(280f, 72f), false);

            var fail = safe.Find("FailPanel");
            StyleText(fail?.Find("FailTitle"), new Vector2(0f, 157f), new Vector2(620f, 72f), 46f, PanelTheme.FailureTitleColor, FontStyles.Bold);
            StyleText(fail?.Find("FailReason"), new Vector2(0f, 35f), new Vector2(640f, 116f), 27f, PanelTheme.BodyColor, FontStyles.Normal, 8f);
            StyleButton(fail?.Find("FailRetry"), new Vector2(-155f, -139f), new Vector2(280f, 72f), true);
            StyleButton(fail?.Find("FailLevels"), new Vector2(155f, -139f), new Vector2(280f, 72f), false);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void ConfigureHudLayout(Transform safe)
        {
            SetTopAnchor(safe.Find("Earnings") as RectTransform, new Vector2(72f, -38f), new Vector2(280f, 48f), false);
            SetTopAnchor(safe.Find("Target") as RectTransform, new Vector2(72f, -82f), new Vector2(280f, 48f), false);
            SetTopAnchor(safe.Find("Mistakes") as RectTransform, new Vector2(-500f, -38f), new Vector2(320f, 48f), true);
            SetTopAnchor(safe.Find("Active") as RectTransform, new Vector2(-500f, -82f), new Vector2(320f, 48f), true);
            SetTopAnchor(safe.Find("Tutorial") as RectTransform, new Vector2(0f, -112f), new Vector2(680f, 64f), null);
            SetTopAnchor(safe.Find("Pause") as RectTransform, new Vector2(-105f, -38f), new Vector2(170f, 52f), true);
            ConfigureHudIcon(safe.Find("CreditIcon"), new Vector2(32f, -38f), false);
            ConfigureHudIcon(safe.Find("MistakeIcon"), new Vector2(-540f, -38f), true);
        }

        private static void ConfigurePausePanel(Transform panel)
        {
            if (panel == null)
            {
                return;
            }

            PanelTheme.Apply(panel.gameObject, new Vector2(680f, 378f));
            StyleText(panel.Find("PauseTitle"), new Vector2(0f, 126f), new Vector2(500f, 66f), 44f, PanelTheme.TitleColor, FontStyles.Bold);
            StyleButton(panel.Find("Resume"), new Vector2(0f, 18f), new Vector2(280f, 70f), true);
            StyleButton(panel.Find("PauseLevels"), new Vector2(0f, -70f), new Vector2(280f, 70f), false);
        }

        private static void ConfigureResultPanel(Transform panel, Vector2 cardSize)
        {
            if (panel == null)
            {
                return;
            }

            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var scrim = panel.GetComponent<Image>();
            scrim.sprite = null;
            scrim.color = new Color(0.015f, 0.035f, 0.055f, 0.82f);
            scrim.raycastTarget = true;

            var art = EnsureImage(panel, "ResultPanelArt", 0);
            var artRect = art.rectTransform;
            artRect.anchorMin = new Vector2(0.5f, 0.5f);
            artRect.anchorMax = new Vector2(0.5f, 0.5f);
            artRect.pivot = new Vector2(0.5f, 0.5f);
            artRect.anchoredPosition = Vector2.zero;
            artRect.sizeDelta = cardSize;
            PanelTheme.Apply(art);
            art.raycastTarget = false;

            var group = panel.GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = panel.gameObject.AddComponent<CanvasGroup>();
            }

            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
        }

        private static void ConfigureStars(Transform complete)
        {
            if (complete == null)
            {
                return;
            }

            for (var i = 0; i < 3; i++)
            {
                var star = complete.Find($"Star_{i}");
                if (star == null)
                {
                    continue;
                }

                var rect = star.GetComponent<RectTransform>();
                rect.anchoredPosition = new Vector2(-92f + i * 92f, -101f);
                rect.sizeDelta = new Vector2(76f, 76f);
                var image = star.GetComponent<Image>();
                image.raycastTarget = false;
                image.preserveAspect = true;
                image.color = new Color(0.29f, 0.2f, 0.12f, 0.35f);
            }
        }

        private static void AuthorUpgradesPanel()
        {
            var scene = EditorSceneManager.OpenScene(MainMenuScene, OpenSceneMode.Single);
            var canvas = Object.FindFirstObjectByType<Canvas>();
            var panel = canvas != null ? canvas.transform.Find("SafeArea/UpgradesPanel") : null;
            if (panel == null)
            {
                return;
            }

            PanelTheme.Apply(panel.gameObject, new Vector2(900f, 500f));
            StyleText(panel.Find("UpgradesTitle"), new Vector2(0f, 185f), new Vector2(700f, 80f), 44f, PanelTheme.TitleColor, FontStyles.Bold);
            var close = panel.Find("CloseUpgrades");
            if (close != null)
            {
                close.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -190f);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static Image EnsureImage(Transform parent, string name, int siblingIndex)
        {
            var child = parent.Find(name);
            GameObject go;
            if (child == null)
            {
                go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(parent, false);
            }
            else
            {
                go = child.gameObject;
            }

            go.transform.SetSiblingIndex(Mathf.Clamp(siblingIndex, 0, parent.childCount - 1));
            return go.GetComponent<Image>();
        }

        private static void StyleText(
            Transform target,
            Vector2 position,
            Vector2 size,
            float fontSize,
            Color color,
            FontStyles style,
            float lineSpacing = 0f)
        {
            if (target == null)
            {
                return;
            }

            var text = target.GetComponent<TextMeshProUGUI>();
            var rect = text.rectTransform;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.lineSpacing = lineSpacing;
            text.raycastTarget = false;
        }

        private static void StyleButton(Transform target, Vector2 position, Vector2 size, bool primary)
        {
            if (target == null)
            {
                return;
            }

            var rect = target.GetComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var button = target.GetComponent<Button>();
            var image = button.targetGraphic as Image;
            if (image != null)
            {
                image.color = primary ? Color.white : new Color(0.55f, 0.78f, 0.8f, 1f);
            }

            var label = target.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                label.rectTransform.sizeDelta = size - new Vector2(30f, 12f);
                label.fontSize = 28f;
                label.fontStyle = FontStyles.Bold;
                label.color = primary ? new Color(0.025f, 0.19f, 0.23f, 1f) : Color.white;
                label.raycastTarget = false;
            }
        }

        private static void SetTopAnchor(RectTransform rect, Vector2 position, Vector2 size, bool? alignRight)
        {
            if (rect == null)
            {
                return;
            }

            var anchor = alignRight.HasValue
                ? new Vector2(alignRight.Value ? 1f : 0f, 1f)
                : new Vector2(0.5f, 1f);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = alignRight.HasValue
                ? new Vector2(alignRight.Value ? 1f : 0f, 1f)
                : new Vector2(0.5f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void ConfigureHudIcon(Transform target, Vector2 position, bool alignRight)
        {
            if (target == null)
            {
                return;
            }

            SetTopAnchor(target as RectTransform, position, new Vector2(42f, 42f), alignRight);
            var image = target.GetComponent<Image>();
            if (image != null)
            {
                image.raycastTarget = false;
            }
        }
    }
}
