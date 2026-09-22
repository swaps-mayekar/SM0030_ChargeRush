using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ChargeRush.Core
{
    /// <summary>Simple additive-safe scene loader with fade-ready hooks.</summary>
    public sealed class SceneLoader : MonoBehaviour
    {
        public static SceneLoader Instance { get; private set; }

        public const string BootScene = "0_Boot";
        public const string MainMenuScene = "1_MainMenu";
        public const string LevelSelectScene = "2_LevelSelect";
        public const string GameplayScene = "3_Gameplay";
        public const string AchievementsScene = "4_Achievements";

        private bool isLoading;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void Load(string sceneName)
        {
            if (isLoading || string.IsNullOrEmpty(sceneName))
            {
                return;
            }

            StartCoroutine(LoadRoutine(sceneName));
        }

        private IEnumerator LoadRoutine(string sceneName)
        {
            isLoading = true;
            var op = SceneManager.LoadSceneAsync(sceneName);
            while (op != null && !op.isDone)
            {
                yield return null;
            }

            isLoading = false;
        }
    }
}
