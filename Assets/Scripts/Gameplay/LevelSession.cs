using System.Collections.Generic;
using ChargeRush.Charging;
using ChargeRush.Core;
using ChargeRush.Customers;
using ChargeRush.Data;
using ChargeRush.Devices;
using ChargeRush.Economy;
using ChargeRush.Pooling;
using ChargeRush.Progression;
using ChargeRush.Save;
using ChargeRush.Tutorial;
using UnityEngine;

namespace ChargeRush.Gameplay
{
    /// <summary>Owns a single gameplay session for story, challenge, or endless mode.</summary>
    public sealed class LevelSession : MonoBehaviour
    {
        public static LevelSession Instance { get; private set; }

        [SerializeField] private GameCatalog catalog;
        [SerializeField] private CustomerInstance customerPrefab;
        [SerializeField] private DeviceInstance devicePrefab;
        [SerializeField] private ChargingPort portPrefab;
        [SerializeField] private CustomerQueue customerQueue;
        [SerializeField] private ChargingStation chargingStation;
        [SerializeField] private EconomyManager economy;
        [SerializeField] private UpgradeManager upgrades;
        [SerializeField] private AchievementManager achievements;
        [SerializeField] private DragDropController dragDrop;
        [SerializeField] private TutorialDirector tutorial;
        [SerializeField] private SpriteRenderer backgroundRenderer;
        [SerializeField] private Transform poolRoot;
        [SerializeField] private Transform counterRoot;

        private ObjectPool<CustomerInstance> customerPool;
        private ObjectPool<DeviceInstance> devicePool;
        private readonly List<CustomerInstance> activeCustomers = new List<CustomerInstance>();
        private LevelData level;
        private ChallengeData challenge;
        private GameMode mode;
        private SessionState state = SessionState.Loading;
        private float spawnTimer;
        private float sessionTime;
        private int spawnCounter;
        private bool wrapUpStarted;
        private Camera gameplayCamera;

        public SessionState State => state;
        public LevelData Level => level;
        public EconomyManager Economy => economy;
        public GameMode Mode => mode;

        private void Awake()
        {
            Instance = this;
            gameplayCamera = Camera.main;
            if (catalog == null && GameBootstrap.Instance != null)
            {
                catalog = GameBootstrap.Instance.Catalog;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            GameEvents.PaymentReceived -= OnPayment;
            GameEvents.CustomerLeftAngry -= OnCustomerLeftAngry;
            GameEvents.ChargingCompleted -= OnChargingCompleted;
            GameEvents.DevicePickedUp -= OnDevicePickedUp;
        }

        private void Start()
        {
            GameEvents.PaymentReceived += OnPayment;
            GameEvents.CustomerLeftAngry += OnCustomerLeftAngry;
            GameEvents.ChargingCompleted += OnChargingCompleted;
            GameEvents.DevicePickedUp += OnDevicePickedUp;

            if (poolRoot == null)
            {
                poolRoot = new GameObject("Pools").transform;
                poolRoot.SetParent(transform, false);
            }

            if (customerPrefab == null || devicePrefab == null || portPrefab == null)
            {
                Debug.LogError(
                    "ChargeRush LevelSession: assign Customer, Device, and ChargingPort prefabs " +
                    "(Assets/Prefabs). Missing: " +
                    (customerPrefab == null ? "customerPrefab " : string.Empty) +
                    (devicePrefab == null ? "devicePrefab " : string.Empty) +
                    (portPrefab == null ? "portPrefab" : string.Empty));
                return;
            }

            customerPool = new ObjectPool<CustomerInstance>(customerPrefab, poolRoot, 8);
            devicePool = new ObjectPool<DeviceInstance>(devicePrefab, poolRoot, 8);

            var bootstrap = GameBootstrap.Instance;
            mode = bootstrap != null ? bootstrap.SelectedMode : GameMode.Story;
            level = bootstrap != null ? bootstrap.SelectedLevel : (catalog != null && catalog.StoryLevels.Count > 0 ? catalog.StoryLevels[0] : null);
            challenge = bootstrap != null ? bootstrap.SelectedChallenge : null;

            if (level == null)
            {
                Debug.LogError("ChargeRush LevelSession: missing LevelData.");
                return;
            }

            BeginSession();
        }

        private void Update()
        {
            if (state != SessionState.Playing && state != SessionState.Completing)
            {
                return;
            }

            var dt = Time.deltaTime;
            sessionTime += dt;

            chargingStation.Tick(dt);

            for (var i = activeCustomers.Count - 1; i >= 0; i--)
            {
                var customer = activeCustomers[i];
                if (customer == null)
                {
                    activeCustomers.RemoveAt(i);
                    continue;
                }

                customer.Tick(dt);
                if (customer.HasLeftScreen())
                {
                    DespawnCustomer(customer);
                }
            }

            if (state == SessionState.Playing)
            {
                spawnTimer -= dt;
                if (spawnTimer <= 0f)
                {
                    SpawnCustomer();
                    ResetSpawnTimer();
                }

                EvaluateModeObjectives();
            }
            else if (state == SessionState.Completing)
            {
                if (activeCustomers.Count == 0)
                {
                    FinishSuccess();
                }
            }
        }

        public float GetAdjustedChargeDuration(float baseDuration)
        {
            var levelMul = level != null ? level.ChargingSpeedMultiplier : 1f;
            var upgradeMul = upgrades != null ? upgrades.GetChargeSpeedMultiplier() : 1f;
            var combined = Mathf.Max(0.25f, levelMul) / Mathf.Max(0.25f, upgradeMul);
            return Mathf.Max(0.75f, baseDuration * combined);
        }

        public void Pause()
        {
            if (state != SessionState.Playing)
            {
                return;
            }

            SetState(SessionState.Paused);
            Time.timeScale = 0f;
        }

        public void Resume()
        {
            if (state != SessionState.Paused)
            {
                return;
            }

            Time.timeScale = 1f;
            SetState(SessionState.Playing);
        }

        public void Retry()
        {
            Time.timeScale = 1f;
            SceneLoader.Instance.Load(SceneLoader.GameplayScene);
        }

        public void ReturnToLevelSelect()
        {
            Time.timeScale = 1f;
            SceneLoader.Instance.Load(SceneLoader.LevelSelectScene);
        }

        private void BeginSession()
        {
            Time.timeScale = 1f;
            wrapUpStarted = false;
            sessionTime = 0f;
            spawnCounter = 0;
            economy.Configure(level);
            upgrades.SetCatalog(catalog);
            achievements.SetCatalog(catalog);

            if (backgroundRenderer != null)
            {
                if (level.BackgroundSprite != null)
                {
                    backgroundRenderer.sprite = level.BackgroundSprite;
                    backgroundRenderer.color = Color.white;
                    backgroundRenderer.drawMode = SpriteDrawMode.Simple;
                    FitBackground(backgroundRenderer);
                }
                else
                {
                    backgroundRenderer.color = level.BackgroundTint;
                }
            }

            var connectors = CollectConnectors(level);
            var ports = level.ChargingPortCount + (upgrades != null ? upgrades.GetExtraPorts() : 0);
            ports = Mathf.Clamp(ports, 1, 6);
            var portSprite = Resources.Load<Sprite>("Art/Environment/port_base");
            if (portSprite == null)
            {
                // Fallback for editor-assigned prefab art.
            }
            chargingStation.EnsurePortCount(ports, portPrefab, chargingStation.transform, connectors, portSprite);

            var queueSize = level.MaximumVisibleQueue + (upgrades != null ? upgrades.GetExtraQueueSlots() : 0);
            customerQueue.Configure(Mathf.Clamp(queueSize, 1, 6));

            dragDrop.Configure(gameplayCamera, chargingStation, customerQueue, counterRoot, economy, tutorial);

            var tutorialNeeded = level.IsTutorial &&
                                 (SaveManager.Instance == null ||
                                  !SaveManager.Instance.Data.TutorialCompleted ||
                                  SaveManager.Instance.Data.ReplayTutorialOnNextLevelOne);
            tutorial.Begin(tutorialNeeded);

            ResetSpawnTimer();
            spawnTimer = level.IsTutorial ? 0.5f : 1.5f;
            SetState(SessionState.Playing);
        }

        private void SpawnCustomer()
        {
            if (level == null || level.AvailableCustomers.Count == 0 || level.AvailableDevices.Count == 0)
            {
                return;
            }

            if (mode == GameMode.Story && economy.TargetReached && !wrapUpStarted)
            {
                return;
            }

            var customerData = PickCustomer();
            var deviceData = PickDevice();
            if (customerData == null || deviceData == null)
            {
                return;
            }

            spawnCounter++;
            var ownerId = $"cust_{spawnCounter}";
            var customer = customerPool.Get();
            var device = devicePool.Get();
            device.Initialize(deviceData, ownerId, $"dev_{spawnCounter}");

            var patience = customerData.BasePatienceSeconds *
                           (level != null ? level.PatienceMultiplier : 1f) *
                           (upgrades != null ? upgrades.GetPatienceMultiplier() : 1f);
            if (mode == GameMode.Endless)
            {
                patience *= Mathf.Max(0.55f, 1f - sessionTime / 600f);
            }

            customer.Initialize(customerData, ownerId, device, patience);
            customer.transform.position = customerQueue.transform.position + Vector3.left * 6f;
            customerQueue.TryEnqueue(customer);
            activeCustomers.Add(customer);
            GameEvents.RaiseCustomerArrived(ownerId);
            tutorial.NotifyCustomerSpawned();
        }

        private void DespawnCustomer(CustomerInstance customer)
        {
            if (customer == null)
            {
                return;
            }

            customerQueue.Remove(customer);
            activeCustomers.Remove(customer);

            if (customer.ServicedDevice != null)
            {
                devicePool.Release(customer.ServicedDevice);
                customer.ServicedDevice.ResetForPool();
            }

            if (customer.HeldDevice != null)
            {
                devicePool.Release(customer.HeldDevice);
                customer.HeldDevice.ResetForPool();
            }

            customer.ResetForPool();
            customerPool.Release(customer);
        }

        private void EvaluateModeObjectives()
        {
            if (economy.IsFailed)
            {
                Fail(MistakeReason.WrongCustomer);
                return;
            }

            if (mode == GameMode.Challenge && challenge != null)
            {
                if (challenge.ObjectiveType == ChallengeObjectiveType.FinishWithinTime &&
                    challenge.TimeLimitSeconds > 0f &&
                    sessionTime >= challenge.TimeLimitSeconds &&
                    !IsChallengeComplete())
                {
                    Fail(MistakeReason.CustomerLeft);
                    return;
                }

                if (IsChallengeComplete())
                {
                    BeginWrapUp();
                }

                return;
            }

            if (mode == GameMode.Story && economy.TargetReached)
            {
                BeginWrapUp();
            }
        }

        private bool IsChallengeComplete()
        {
            if (challenge == null)
            {
                return false;
            }

            switch (challenge.ObjectiveType)
            {
                case ChallengeObjectiveType.EarnCredits:
                    return economy.CurrentEarnings >= challenge.TargetValue;
                case ChallengeObjectiveType.ServeCustomers:
                    return economy.CustomersServed >= challenge.TargetValue;
                case ChallengeObjectiveType.ZeroMistakes:
                    return economy.CurrentEarnings >= level.TargetEarnings && economy.Mistakes == 0;
                case ChallengeObjectiveType.FinishWithinTime:
                    return economy.CurrentEarnings >= challenge.TargetValue;
                default:
                    return false;
            }
        }

        private void BeginWrapUp()
        {
            if (wrapUpStarted)
            {
                return;
            }

            wrapUpStarted = true;
            SetState(SessionState.Completing);
            if (activeCustomers.Count == 0)
            {
                FinishSuccess();
            }
        }

        private void FinishSuccess()
        {
            if (state == SessionState.Completed || state == SessionState.Failed)
            {
                return;
            }

            SetState(SessionState.Completed);
            var stars = economy.EvaluateStars(level);
            PersistProgress(true, stars);
            GameEvents.RaiseStarsEarned(stars);
            GameEvents.RaiseLevelCompleted();
        }

        private void Fail(MistakeReason reason)
        {
            if (state == SessionState.Failed || state == SessionState.Completed)
            {
                return;
            }

            SetState(SessionState.Failed);
            PersistProgress(false, 0);
            GameEvents.RaiseLevelFailed(reason);
        }

        private void PersistProgress(bool success, int stars)
        {
            var save = SaveManager.Instance;
            if (save == null)
            {
                return;
            }

            save.Data.LifetimeEarnings += economy.CurrentEarnings;
            save.Data.TotalCredits += economy.CurrentEarnings;
            save.Data.CustomersServed += economy.CustomersServed;
            save.Data.DevicesCharged += economy.DevicesCharged;
            save.Data.BestStreak = Mathf.Max(save.Data.BestStreak, economy.BestStreak);

            if (mode == GameMode.Endless)
            {
                save.Data.EndlessBestEarnings = Mathf.Max(save.Data.EndlessBestEarnings, economy.CurrentEarnings);
                save.Data.EndlessBestCustomers = Mathf.Max(save.Data.EndlessBestCustomers, economy.CustomersServed);
                save.Data.EndlessBestStreak = Mathf.Max(save.Data.EndlessBestStreak, economy.BestStreak);
            }

            if (mode == GameMode.Story && level != null)
            {
                var record = save.GetOrCreateLevel(level.LevelId);
                if (success)
                {
                    if (!record.Completed)
                    {
                        record.Completed = true;
                        save.Data.StoryLevelsCompleted++;
                    }

                    record.Stars = Mathf.Max(record.Stars, stars);
                    record.BestEarnings = Mathf.Max(record.BestEarnings, economy.CurrentEarnings);
                    if (stars >= 3)
                    {
                        save.Data.ThreeStarLevels = CountThreeStarLevels(save);
                    }

                    if (economy.Mistakes == 0)
                    {
                        save.Data.PerfectLevels++;
                    }

                    if (level.IsTutorial)
                    {
                        save.Data.TutorialCompleted = true;
                        save.Data.ReplayTutorialOnNextLevelOne = false;
                    }
                }
            }

            if (mode == GameMode.Challenge && challenge != null && success)
            {
                if (!save.Data.CompletedChallenges.Contains(challenge.ChallengeId))
                {
                    save.Data.CompletedChallenges.Add(challenge.ChallengeId);
                }
            }

            CareerProgression.RefreshFromSave();
            achievements.EvaluateAfterSession(
                economy.CustomersServed,
                economy.DevicesCharged,
                stars,
                economy.Mistakes == 0,
                success,
                null);
            save.Save();
        }

        private static int CountThreeStarLevels(SaveManager save)
        {
            var count = 0;
            for (var i = 0; i < save.Data.Levels.Count; i++)
            {
                if (save.Data.Levels[i].Stars >= 3)
                {
                    count++;
                }
            }

            return count;
        }

        private void ResetSpawnTimer()
        {
            var interval = level.CustomerSpawnIntervalSeconds;
            if (mode == GameMode.Endless)
            {
                interval = Mathf.Max(2.5f, interval - sessionTime * 0.01f);
            }

            var variance = level.SpawnIntervalVariance;
            spawnTimer = interval + Random.Range(-variance, variance);
            spawnTimer = Mathf.Max(1.5f, spawnTimer);
        }

        private CustomerData PickCustomer()
        {
            return PickWeighted(level.AvailableCustomers, e => e.Customer, e => e.Weight);
        }

        private DeviceData PickDevice()
        {
            return PickWeighted(level.AvailableDevices, e => e.Device, e => e.Weight);
        }

        private static TResult PickWeighted<TEntry, TResult>(
            IReadOnlyList<TEntry> entries,
            System.Func<TEntry, TResult> selector,
            System.Func<TEntry, float> weightSelector) where TResult : class
        {
            if (entries == null || entries.Count == 0)
            {
                return null;
            }

            float total = 0f;
            for (var i = 0; i < entries.Count; i++)
            {
                if (selector(entries[i]) != null)
                {
                    total += Mathf.Max(0f, weightSelector(entries[i]));
                }
            }

            if (total <= 0f)
            {
                return selector(entries[0]);
            }

            var roll = Random.Range(0f, total);
            float cumulative = 0f;
            for (var i = 0; i < entries.Count; i++)
            {
                var item = selector(entries[i]);
                if (item == null)
                {
                    continue;
                }

                cumulative += Mathf.Max(0f, weightSelector(entries[i]));
                if (roll <= cumulative)
                {
                    return item;
                }
            }

            return selector(entries[entries.Count - 1]);
        }

        private static List<ConnectorType> CollectConnectors(LevelData data)
        {
            var list = new List<ConnectorType>();
            if (data == null)
            {
                list.Add(ConnectorType.PowerLinkA);
                return list;
            }

            for (var i = 0; i < data.AvailableDevices.Count; i++)
            {
                var device = data.AvailableDevices[i].Device;
                if (device == null)
                {
                    continue;
                }

                if (!list.Contains(device.RequiredConnector))
                {
                    list.Add(device.RequiredConnector);
                }
            }

            if (list.Count == 0)
            {
                list.Add(ConnectorType.PowerLinkA);
            }

            return list;
        }

        private void SetState(SessionState next)
        {
            state = next;
            GameEvents.RaiseSessionStateChanged(next);
        }

        private void OnPayment(int amount)
        {
            tutorial.NotifyPayment();
        }

        private void OnCustomerLeftAngry(string ownerId)
        {
            economy.RegisterMistake(MistakeReason.CustomerLeft, true);
            if (economy.IsFailed)
            {
                Fail(MistakeReason.CustomerLeft);
            }
        }

        private void OnChargingCompleted(DeviceInstance device)
        {
            tutorial.NotifyChargeComplete(device);
            if (SaveManager.Instance != null && device != null && device.Data != null)
            {
                var key = device.Data.Category.ToString();
                if (!SaveManager.Instance.Data.ChargedDeviceCategories.Contains(key))
                {
                    SaveManager.Instance.Data.ChargedDeviceCategories.Add(key);
                }
            }
        }

        private void OnDevicePickedUp(DeviceInstance device)
        {
            if (device != null && device.State != DeviceState.FullyCharged)
            {
                tutorial.NotifyDevicePickedFromCustomer();
            }
        }

        private static void FitBackground(SpriteRenderer renderer)
        {
            if (renderer == null || renderer.sprite == null)
            {
                return;
            }

            var cam = Camera.main;
            if (cam == null || !cam.orthographic)
            {
                renderer.transform.localScale = new Vector3(18f, 10f, 1f);
                return;
            }

            var height = cam.orthographicSize * 2f;
            var width = height * cam.aspect;
            var spriteSize = renderer.sprite.bounds.size;
            if (spriteSize.x <= 0.01f || spriteSize.y <= 0.01f)
            {
                return;
            }

            renderer.transform.localScale = new Vector3(width / spriteSize.x, height / spriteSize.y, 1f);
            renderer.transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, 0f);
        }
    }
}
