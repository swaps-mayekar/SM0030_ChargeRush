using ChargeRush.Core;
using ChargeRush.Data;
using ChargeRush.Gameplay;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChargeRush.UI
{
    /// <summary>Gameplay HUD and result overlays.</summary>
    public sealed class GameplayHud : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI earningsText;
        [SerializeField] private TextMeshProUGUI targetText;
        [SerializeField] private TextMeshProUGUI mistakesText;
        [SerializeField] private TextMeshProUGUI activeChargesText;
        [SerializeField] private TextMeshProUGUI tutorialText;
        [SerializeField] private TextMeshProUGUI paymentPopupText;
        [SerializeField] private Button pauseButton;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject completePanel;
        [SerializeField] private GameObject failPanel;
        [SerializeField] private TextMeshProUGUI completeStatsText;
        [SerializeField] private TextMeshProUGUI failReasonText;
        [SerializeField] private TextMeshProUGUI starsText;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button failRetryButton;
        [SerializeField] private Button levelSelectButton;
        [SerializeField] private Button failLevelSelectButton;
        [SerializeField] private Button continueButton;

        private float paymentPopupTimer;
        private Image[] resultStars;
        private Coroutine resultTransition;

        private static readonly Color ModalScrim = new Color(0.015f, 0.035f, 0.055f, 0.82f);

        private void Awake()
        {
            ConfigureResponsiveLayout();
            ConfigureResultScreens();
        }

        private void OnEnable()
        {
            GameEvents.EarningsChanged += OnEarnings;
            GameEvents.MistakesChanged += OnMistakes;
            GameEvents.PaymentReceived += OnPayment;
            GameEvents.TutorialStepChanged += OnTutorial;
            GameEvents.SessionStateChanged += OnSession;
            GameEvents.StarsEarned += OnStars;
            GameEvents.LevelFailed += OnFailed;

            if (pauseButton != null) pauseButton.onClick.AddListener(OnPause);
            if (resumeButton != null) resumeButton.onClick.AddListener(OnResume);
            if (retryButton != null) retryButton.onClick.AddListener(OnRetry);
            if (failRetryButton != null) failRetryButton.onClick.AddListener(OnRetry);
            if (levelSelectButton != null) levelSelectButton.onClick.AddListener(OnLevelSelect);
            if (failLevelSelectButton != null) failLevelSelectButton.onClick.AddListener(OnLevelSelect);
            if (continueButton != null) continueButton.onClick.AddListener(OnContinue);
        }

        private void OnDisable()
        {
            GameEvents.EarningsChanged -= OnEarnings;
            GameEvents.MistakesChanged -= OnMistakes;
            GameEvents.PaymentReceived -= OnPayment;
            GameEvents.TutorialStepChanged -= OnTutorial;
            GameEvents.SessionStateChanged -= OnSession;
            GameEvents.StarsEarned -= OnStars;
            GameEvents.LevelFailed -= OnFailed;

            if (pauseButton != null) pauseButton.onClick.RemoveListener(OnPause);
            if (resumeButton != null) resumeButton.onClick.RemoveListener(OnResume);
            if (retryButton != null) retryButton.onClick.RemoveListener(OnRetry);
            if (failRetryButton != null) failRetryButton.onClick.RemoveListener(OnRetry);
            if (levelSelectButton != null) levelSelectButton.onClick.RemoveListener(OnLevelSelect);
            if (failLevelSelectButton != null) failLevelSelectButton.onClick.RemoveListener(OnLevelSelect);
            if (continueButton != null) continueButton.onClick.RemoveListener(OnContinue);
        }

        private void Update()
        {
            if (activeChargesText != null)
            {
                var station = FindFirstObjectByType<ChargeRush.Charging.ChargingStation>();
                if (station != null)
                {
                    activeChargesText.text = $"Charging: {station.OccupiedCount}";
                }
            }

            if (paymentPopupTimer > 0f)
            {
                paymentPopupTimer -= Time.unscaledDeltaTime;
                if (paymentPopupTimer <= 0f && paymentPopupText != null)
                {
                    paymentPopupText.gameObject.SetActive(false);
                }
            }
        }

        public void BindTexts(
            TextMeshProUGUI earnings,
            TextMeshProUGUI target,
            TextMeshProUGUI mistakes,
            TextMeshProUGUI active,
            TextMeshProUGUI tutorial,
            TextMeshProUGUI payment)
        {
            earningsText = earnings;
            targetText = target;
            mistakesText = mistakes;
            activeChargesText = active;
            tutorialText = tutorial;
            paymentPopupText = payment;
        }

        private void OnEarnings(int value)
        {
            if (earningsText != null)
            {
                // CreditIcon sits beside this label in the HUD chrome.
                earningsText.text = CreditUi.Format(value);
            }

            if (targetText != null && LevelSession.Instance != null && LevelSession.Instance.Level != null)
            {
                targetText.text = $"Target: {CreditUi.Format(LevelSession.Instance.Level.TargetEarnings)}";
            }
        }

        private void OnMistakes(int current, int max)
        {
            if (mistakesText != null)
            {
                mistakesText.text = $"Mistakes: {current} / {max}";
            }
        }

        private void OnPayment(int amount)
        {
            if (paymentPopupText == null)
            {
                return;
            }

            paymentPopupText.gameObject.SetActive(true);
            paymentPopupText.text = CreditUi.FormatSigned(amount);
            CreditUi.EnsureIconAfterText(paymentPopupText, 52f);
            paymentPopupTimer = 1.1f;
        }

        private void OnTutorial(string step)
        {
            if (tutorialText != null)
            {
                tutorialText.gameObject.SetActive(!string.IsNullOrEmpty(step));
                tutorialText.text = step;
            }
        }

        private void OnSession(SessionState state)
        {
            if (pausePanel != null) pausePanel.SetActive(state == SessionState.Paused);
            SetResultPanelVisible(completePanel, state == SessionState.Completed);
            SetResultPanelVisible(failPanel, state == SessionState.Failed);

            if (state == SessionState.Completed && LevelSession.Instance != null)
            {
                var eco = LevelSession.Instance.Economy;
                if (completeStatsText != null)
                {
                    completeStatsText.text =
                        $"<color=#{ColorUtility.ToHtmlStringRGB(PanelTheme.MutedColor)}>EARNINGS</color>  {CreditUi.Format(eco.CurrentEarnings)}" +
                        $"     <color=#{ColorUtility.ToHtmlStringRGB(PanelTheme.MutedColor)}>GUESTS SERVED</color>  {eco.CustomersServed}\n" +
                        $"<color=#{ColorUtility.ToHtmlStringRGB(PanelTheme.MutedColor)}>SERVICE MISTAKES</color>  {eco.Mistakes}";
                }
            }
        }

        private void OnStars(int stars)
        {
            if (starsText != null)
            {
                starsText.text = $"{stars} OF 3 STARS";
            }

            if (resultStars == null)
            {
                return;
            }

            for (var i = 0; i < resultStars.Length; i++)
            {
                if (resultStars[i] != null)
                {
                    resultStars[i].color = i < stars
                        ? Color.white
                        : new Color(0.35f, 0.43f, 0.46f, 0.42f);
                }
            }
        }

        private void OnFailed(MistakeReason reason)
        {
            if (failReasonText != null)
            {
                failReasonText.text = reason == MistakeReason.CustomerLeft
                    ? "Too many guests left unhappy.\n<color=#634529>Serve waiting guests before their patience runs out.</color>"
                    : "Too many service mistakes.\n<color=#634529>Match each device with the correct charger.</color>";
            }
        }

        private void OnPause()
        {
            if (LevelSession.Instance != null)
            {
                LevelSession.Instance.Pause();
            }
        }

        private void OnResume()
        {
            if (LevelSession.Instance != null)
            {
                LevelSession.Instance.Resume();
            }
        }

        private void OnRetry()
        {
            if (LevelSession.Instance != null)
            {
                LevelSession.Instance.Retry();
            }
        }

        private void OnLevelSelect()
        {
            if (LevelSession.Instance != null)
            {
                LevelSession.Instance.ReturnToLevelSelect();
            }
            else
            {
                SceneLoader.Instance.Load(SceneLoader.LevelSelectScene);
            }
        }

        private void OnContinue()
        {
            SceneLoader.Instance.Load(SceneLoader.LevelSelectScene);
        }

        private void ConfigureResponsiveLayout()
        {
            SetTopAnchor(earningsText != null ? earningsText.rectTransform : null, new Vector2(72f, -38f), new Vector2(280f, 48f), false);
            SetTopAnchor(targetText != null ? targetText.rectTransform : null, new Vector2(72f, -82f), new Vector2(280f, 48f), false);
            SetTopAnchor(mistakesText != null ? mistakesText.rectTransform : null, new Vector2(-500f, -38f), new Vector2(320f, 48f), true);
            SetTopAnchor(activeChargesText != null ? activeChargesText.rectTransform : null, new Vector2(-500f, -82f), new Vector2(320f, 48f), true);
            SetTopAnchor(tutorialText != null ? tutorialText.rectTransform : null, new Vector2(0f, -112f), new Vector2(680f, 64f), null);
            SetTopAnchor(pauseButton != null ? pauseButton.GetComponent<RectTransform>() : null, new Vector2(-105f, -38f), new Vector2(170f, 52f), true);

            var safeArea = transform.Find("SafeArea");
            ConfigureHudIcon(safeArea != null ? safeArea.Find("CreditIcon") : null, new Vector2(32f, -38f), false);
            ConfigureHudIcon(safeArea != null ? safeArea.Find("MistakeIcon") : null, new Vector2(-540f, -38f), true);
        }

        private void ConfigureResultScreens()
        {
            ConfigurePausePanel();
            ConfigureResultPanel(completePanel, new Vector2(900f, 500f));
            ConfigureResultPanel(failPanel, new Vector2(820f, 455f));

            var completeTitle = FindText(completePanel, "CompleteTitle");
            StyleText(completeTitle, new Vector2(0f, 184f), new Vector2(650f, 72f), 48f, PanelTheme.TitleColor, FontStyles.Bold);
            StyleText(completeStatsText, new Vector2(0f, 64f), new Vector2(690f, 115f), 27f, PanelTheme.BodyColor, FontStyles.Normal);
            if (completeStatsText != null)
            {
                completeStatsText.lineSpacing = 12f;
            }

            StyleText(starsText, new Vector2(0f, -26f), new Vector2(500f, 48f), 20f, PanelTheme.MutedColor, FontStyles.Bold);
            ConfigureStars();
            StyleResultButton(continueButton, new Vector2(-155f, -181f), new Vector2(280f, 72f), true);
            StyleResultButton(retryButton, new Vector2(155f, -181f), new Vector2(280f, 72f), false);

            var failTitle = FindText(failPanel, "FailTitle");
            StyleText(failTitle, new Vector2(0f, 157f), new Vector2(620f, 72f), 46f, PanelTheme.FailureTitleColor, FontStyles.Bold);
            StyleText(failReasonText, new Vector2(0f, 35f), new Vector2(640f, 116f), 27f, PanelTheme.BodyColor, FontStyles.Normal);
            if (failReasonText != null)
            {
                failReasonText.lineSpacing = 8f;
            }

            StyleResultButton(failRetryButton, new Vector2(-155f, -139f), new Vector2(280f, 72f), true);
            StyleResultButton(failLevelSelectButton, new Vector2(155f, -139f), new Vector2(280f, 72f), false);
        }

        private void ConfigurePausePanel()
        {
            PanelTheme.Apply(pausePanel, new Vector2(680f, 378f));
            StyleText(
                FindText(pausePanel, "PauseTitle"),
                new Vector2(0f, 126f),
                new Vector2(500f, 66f),
                44f,
                PanelTheme.TitleColor,
                FontStyles.Bold);
            StyleResultButton(resumeButton, new Vector2(0f, 18f), new Vector2(280f, 70f), true);
            StyleResultButton(levelSelectButton, new Vector2(0f, -70f), new Vector2(280f, 70f), false);
        }

        private static void ConfigureResultPanel(GameObject panel, Vector2 cardSize)
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

            var panelImage = panel.GetComponent<Image>();
            var frameSprite = PanelTheme.PanelSprite;
            if (frameSprite == null && panelImage != null)
            {
                frameSprite = panelImage.sprite;
            }

            if (panelImage != null)
            {
                panelImage.sprite = null;
                panelImage.color = ModalScrim;
                panelImage.raycastTarget = true;
            }

            var panelArt = EnsureLayer(panel.transform, "ResultPanelArt", 0);
            ConfigureLayer(panelArt, cardSize, Vector2.zero, Color.white, frameSprite);
            panelArt.raycastTarget = false;

            var group = panel.GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = panel.AddComponent<CanvasGroup>();
            }

            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
        }

        private void ConfigureStars()
        {
            if (completePanel == null)
            {
                resultStars = new Image[0];
                return;
            }

            resultStars = new Image[3];
            for (var i = 0; i < resultStars.Length; i++)
            {
                var star = completePanel.transform.Find($"Star_{i}");
                if (star == null)
                {
                    continue;
                }

                var rect = star as RectTransform;
                rect.anchoredPosition = new Vector2(-92f + i * 92f, -101f);
                rect.sizeDelta = new Vector2(76f, 76f);
                var image = star.GetComponent<Image>();
                if (image != null)
                {
                    image.raycastTarget = false;
                    image.preserveAspect = true;
                    image.color = new Color(0.29f, 0.2f, 0.12f, 0.35f);
                    resultStars[i] = image;
                }
            }
        }

        private static Image EnsureLayer(Transform parent, string name, int siblingIndex)
        {
            var existing = parent.Find(name);
            GameObject layer;
            if (existing != null)
            {
                layer = existing.gameObject;
            }
            else
            {
                layer = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                layer.transform.SetParent(parent, false);
            }

            layer.transform.SetSiblingIndex(Mathf.Clamp(siblingIndex, 0, parent.childCount - 1));
            return layer.GetComponent<Image>();
        }

        private static void ConfigureLayer(Image image, Vector2 size, Vector2 position, Color color, Sprite sprite)
        {
            var rect = image.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = color;
            image.raycastTarget = false;
        }

        private static TextMeshProUGUI FindText(GameObject root, string childName)
        {
            if (root == null)
            {
                return null;
            }

            var child = root.transform.Find(childName);
            return child != null ? child.GetComponent<TextMeshProUGUI>() : null;
        }

        private static void StyleText(
            TextMeshProUGUI text,
            Vector2 position,
            Vector2 size,
            float fontSize,
            Color color,
            FontStyles style)
        {
            if (text == null)
            {
                return;
            }

            text.rectTransform.anchoredPosition = position;
            text.rectTransform.sizeDelta = size;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false;
        }

        private static void StyleResultButton(Button button, Vector2 position, Vector2 size, bool primary)
        {
            if (button == null)
            {
                return;
            }

            var rect = button.GetComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var image = button.targetGraphic as Image;
            if (image != null)
            {
                image.color = primary ? Color.white : new Color(0.55f, 0.78f, 0.8f, 1f);
            }

            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
            colors.pressedColor = new Color(0.78f, 0.88f, 0.9f, 1f);
            colors.disabledColor = new Color(0.45f, 0.5f, 0.52f, 0.55f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            var label = button.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                label.rectTransform.sizeDelta = size - new Vector2(30f, 12f);
                label.fontSize = 28f;
                label.fontStyle = FontStyles.Bold;
                label.color = primary ? new Color(0.025f, 0.19f, 0.23f, 1f) : Color.white;
                label.raycastTarget = false;
            }
        }

        private void SetResultPanelVisible(GameObject panel, bool visible)
        {
            if (panel == null)
            {
                return;
            }

            if (!visible)
            {
                panel.SetActive(false);
                return;
            }

            panel.SetActive(true);
            if (resultTransition != null)
            {
                StopCoroutine(resultTransition);
            }

            resultTransition = StartCoroutine(FadeInResult(panel));
        }

        private static IEnumerator FadeInResult(GameObject panel)
        {
            var group = panel.GetComponent<CanvasGroup>();
            if (group == null)
            {
                yield break;
            }

            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = true;
            var elapsed = 0f;
            const float duration = 0.22f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                group.alpha = 1f - Mathf.Pow(1f - t, 3f);
                yield return null;
            }

            group.alpha = 1f;
            group.interactable = true;
            group.blocksRaycasts = true;
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

        private static void ConfigureHudIcon(Transform iconTransform, Vector2 position, bool alignRight)
        {
            if (iconTransform == null)
            {
                return;
            }

            SetTopAnchor(iconTransform as RectTransform, position, new Vector2(42f, 42f), alignRight);
            var image = iconTransform.GetComponent<Image>();
            if (image != null)
            {
                image.raycastTarget = false;
            }
        }
    }
}
