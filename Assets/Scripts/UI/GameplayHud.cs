using ChargeRush.Core;
using ChargeRush.Data;
using ChargeRush.Gameplay;
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
                earningsText.text = $"Credits: {value}";
            }

            if (targetText != null && LevelSession.Instance != null && LevelSession.Instance.Level != null)
            {
                targetText.text = $"Target: {LevelSession.Instance.Level.TargetEarnings}";
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
            paymentPopupText.text = $"+{amount} CR";
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
            if (completePanel != null) completePanel.SetActive(state == SessionState.Completed);
            if (failPanel != null) failPanel.SetActive(state == SessionState.Failed);

            if (state == SessionState.Completed && LevelSession.Instance != null)
            {
                var eco = LevelSession.Instance.Economy;
                if (completeStatsText != null)
                {
                    completeStatsText.text =
                        $"Earnings: {eco.CurrentEarnings} CR\nCustomers: {eco.CustomersServed}\nMistakes: {eco.Mistakes}";
                }
            }
        }

        private void OnStars(int stars)
        {
            if (starsText != null)
            {
                starsText.text = $"Stars: {stars} / 3";
            }
        }

        private void OnFailed(MistakeReason reason)
        {
            if (failReasonText != null)
            {
                failReasonText.text = reason == MistakeReason.CustomerLeft
                    ? "Too many guests left unhappy."
                    : "Too many service mistakes.";
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
    }
}
