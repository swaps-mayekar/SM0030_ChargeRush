using ChargeRush.Audio;
using ChargeRush.Core;
using ChargeRush.Save;
using UnityEngine;
using UnityEngine.UI;

namespace ChargeRush.UI
{
    public sealed class SettingsController : MonoBehaviour
    {
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private Toggle replayTutorialToggle;
        [SerializeField] private Button backButton;
        [SerializeField] private Button resetButton;

        private void Start()
        {
            var save = SaveManager.Instance;
            if (save != null)
            {
                if (musicSlider != null) musicSlider.value = save.Data.MusicVolume;
                if (sfxSlider != null) sfxSlider.value = save.Data.SfxVolume;
                if (replayTutorialToggle != null) replayTutorialToggle.isOn = save.Data.ReplayTutorialOnNextLevelOne;
            }

            if (musicSlider != null) musicSlider.onValueChanged.AddListener(OnMusic);
            if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(OnSfx);
            if (replayTutorialToggle != null) replayTutorialToggle.onValueChanged.AddListener(OnReplayTutorial);
            if (backButton != null) backButton.onClick.AddListener(() =>
            {
                SaveManager.Instance?.Save();
                SceneLoader.Instance.Load(SceneLoader.MainMenuScene);
            });
            if (resetButton != null) resetButton.onClick.AddListener(() =>
            {
                SaveManager.Instance?.ResetProgress();
                SceneLoader.Instance.Load(SceneLoader.MainMenuScene);
            });
        }

        private void OnMusic(float value)
        {
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.Data.MusicVolume = value;
            }

            AudioManager.Instance?.ApplyVolumes();
        }

        private void OnSfx(float value)
        {
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.Data.SfxVolume = value;
            }

            AudioManager.Instance?.ApplyVolumes();
        }

        private void OnReplayTutorial(bool value)
        {
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.Data.ReplayTutorialOnNextLevelOne = value;
            }
        }
    }
}
