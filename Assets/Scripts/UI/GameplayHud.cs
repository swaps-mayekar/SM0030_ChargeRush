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

        private void Awake()
        {
            CacheResultStars();
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

        private void CacheResultStars()
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

                var image = star.GetComponent<Image>();
                if (image != null)
                {
                    resultStars[i] = image;
                }
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

    }
}
