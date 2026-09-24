using ChargeRush.Data;
using UnityEngine;

namespace ChargeRush.Core
{
    /// <summary>Persistent bootstrap that owns cross-scene services and selected session config.</summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        public static GameBootstrap Instance { get; private set; }

        [SerializeField] private GameCatalog catalog;

        public GameCatalog Catalog => catalog;
        public GameMode SelectedMode { get; private set; } = GameMode.Story;
        /// <summary>Controls whether Level Select shows story levels or challenges.</summary>
        public GameMode SelectBrowseMode { get; private set; } = GameMode.Story;
        public LevelData SelectedLevel { get; private set; }
        public ChallengeData SelectedChallenge { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;

            if (catalog == null)
            {
                catalog = Resources.Load<GameCatalog>("GameData/GameCatalog");
            }
        }

        private void Start()
        {
            if (SceneLoader.Instance != null)
            {
                SceneLoader.Instance.Load(SceneLoader.MainMenuScene);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void SetCatalog(GameCatalog gameCatalog)
        {
            catalog = gameCatalog;
        }

        public void OpenLevelSelect()
        {
            SelectBrowseMode = GameMode.Story;
            SceneLoader.Instance.Load(SceneLoader.LevelSelectScene);
        }

        public void OpenChallengeSelect()
        {
            SelectBrowseMode = GameMode.Challenge;
            SceneLoader.Instance.Load(SceneLoader.LevelSelectScene);
        }

        public void PlayStoryLevel(LevelData level)
        {
            SelectedMode = GameMode.Story;
            SelectBrowseMode = GameMode.Story;
            SelectedLevel = level;
            SelectedChallenge = null;
            SceneLoader.Instance.Load(SceneLoader.GameplayScene);
        }

        public void PlayChallenge(ChallengeData challenge)
        {
            SelectedMode = GameMode.Challenge;
            SelectBrowseMode = GameMode.Challenge;
            SelectedChallenge = challenge;
            SelectedLevel = challenge != null ? challenge.LinkedLevel : null;
            SceneLoader.Instance.Load(SceneLoader.GameplayScene);
        }

        public void PlayEndless(LevelData baseLevel)
        {
            SelectedMode = GameMode.Endless;
            SelectedLevel = baseLevel;
            SelectedChallenge = null;
            SceneLoader.Instance.Load(SceneLoader.GameplayScene);
        }
    }
}
