using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChargeRush.Data
{
    [Serializable]
    public sealed class WeightedDeviceEntry
    {
        public DeviceData Device;
        [Range(0f, 100f)] public float Weight = 1f;
    }

    [Serializable]
    public sealed class WeightedCustomerEntry
    {
        public CustomerData Customer;
        [Range(0f, 100f)] public float Weight = 1f;
    }

    /// <summary>Data-driven definition for a story or challenge level.</summary>
    [CreateAssetMenu(fileName = "LevelData", menuName = "ChargeRush/Level Data")]
    public sealed class LevelData : ScriptableObject
    {
        [SerializeField] private string levelId = "level_01";
        [SerializeField] private int levelNumber = 1;
        [SerializeField] private string levelName = "Local Fair";
        [SerializeField] private string eventName = "Neighborhood Fair";
        [TextArea] [SerializeField] private string eventDescription = "Help a few guests charge their devices.";
        [SerializeField] private int targetEarnings = 150;
        [SerializeField] private int twoStarThreshold = 220;
        [SerializeField] private int threeStarThreshold = 300;
        [SerializeField] private int maximumMistakes = 3;
        [SerializeField] private float customerSpawnIntervalSeconds = 8f;
        [SerializeField] private float spawnIntervalVariance = 1.5f;
        [SerializeField] private int maximumVisibleQueue = 2;
        [SerializeField] private int chargingPortCount = 2;
        [SerializeField] private float patienceMultiplier = 1.2f;
        [SerializeField] private float chargingSpeedMultiplier = 1f;
        [SerializeField] private bool isTutorial;
        [SerializeField] private Color backgroundTint = new Color(0.55f, 0.78f, 0.95f);
        [SerializeField] private List<WeightedDeviceEntry> availableDevices = new List<WeightedDeviceEntry>();
        [SerializeField] private List<WeightedCustomerEntry> availableCustomers = new List<WeightedCustomerEntry>();

        public string LevelId => levelId;
        public int LevelNumber => levelNumber;
        public string LevelName => levelName;
        public string EventName => eventName;
        public string EventDescription => eventDescription;
        public int TargetEarnings => targetEarnings;
        public int TwoStarThreshold => twoStarThreshold;
        public int ThreeStarThreshold => threeStarThreshold;
        public int MaximumMistakes => maximumMistakes;
        public float CustomerSpawnIntervalSeconds => customerSpawnIntervalSeconds;
        public float SpawnIntervalVariance => spawnIntervalVariance;
        public int MaximumVisibleQueue => maximumVisibleQueue;
        public int ChargingPortCount => chargingPortCount;
        public float PatienceMultiplier => patienceMultiplier;
        public float ChargingSpeedMultiplier => chargingSpeedMultiplier;
        public bool IsTutorial => isTutorial;
        public Color BackgroundTint => backgroundTint;
        public IReadOnlyList<WeightedDeviceEntry> AvailableDevices => availableDevices;
        public IReadOnlyList<WeightedCustomerEntry> AvailableCustomers => availableCustomers;

        public void ConfigureBasics(
            string id,
            int number,
            string name,
            string evtName,
            string description,
            int target,
            int twoStar,
            int threeStar,
            int mistakes,
            float spawnInterval,
            int queueSize,
            int ports,
            float patienceMul,
            float chargeMul,
            bool tutorial,
            Color tint)
        {
            levelId = id;
            levelNumber = number;
            levelName = name;
            eventName = evtName;
            eventDescription = description;
            targetEarnings = target;
            twoStarThreshold = twoStar;
            threeStarThreshold = threeStar;
            maximumMistakes = mistakes;
            customerSpawnIntervalSeconds = spawnInterval;
            maximumVisibleQueue = queueSize;
            chargingPortCount = ports;
            patienceMultiplier = patienceMul;
            chargingSpeedMultiplier = chargeMul;
            isTutorial = tutorial;
            backgroundTint = tint;
        }

        public void SetContent(List<WeightedDeviceEntry> devices, List<WeightedCustomerEntry> customers)
        {
            availableDevices = devices ?? new List<WeightedDeviceEntry>();
            availableCustomers = customers ?? new List<WeightedCustomerEntry>();
        }

        public int EvaluateStars(int earnings, int mistakes, bool customerLeft)
        {
            if (earnings < targetEarnings)
            {
                return 0;
            }

            var stars = 1;
            if (earnings >= twoStarThreshold)
            {
                stars = 2;
            }

            if (earnings >= threeStarThreshold && mistakes == 0 && !customerLeft)
            {
                stars = 3;
            }
            else if (earnings >= threeStarThreshold)
            {
                stars = Mathf.Max(stars, 2);
                if (mistakes <= 1)
                {
                    stars = 3;
                }
            }

            return stars;
        }
    }
}
