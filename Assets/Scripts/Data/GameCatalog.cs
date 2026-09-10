using System.Collections.Generic;
using UnityEngine;

namespace ChargeRush.Data
{
    /// <summary>Central catalog of all ChargeRush content assets.</summary>
    [CreateAssetMenu(fileName = "GameCatalog", menuName = "ChargeRush/Game Catalog")]
    public sealed class GameCatalog : ScriptableObject
    {
        [SerializeField] private List<DeviceData> devices = new List<DeviceData>();
        [SerializeField] private List<CustomerData> customers = new List<CustomerData>();
        [SerializeField] private List<LevelData> storyLevels = new List<LevelData>();
        [SerializeField] private List<UpgradeData> upgrades = new List<UpgradeData>();
        [SerializeField] private List<AchievementData> achievements = new List<AchievementData>();
        [SerializeField] private List<ChallengeData> challenges = new List<ChallengeData>();

        public IReadOnlyList<DeviceData> Devices => devices;
        public IReadOnlyList<CustomerData> Customers => customers;
        public IReadOnlyList<LevelData> StoryLevels => storyLevels;
        public IReadOnlyList<UpgradeData> Upgrades => upgrades;
        public IReadOnlyList<AchievementData> Achievements => achievements;
        public IReadOnlyList<ChallengeData> Challenges => challenges;

        public void AssignAll(
            List<DeviceData> deviceList,
            List<CustomerData> customerList,
            List<LevelData> levelList,
            List<UpgradeData> upgradeList,
            List<AchievementData> achievementList,
            List<ChallengeData> challengeList)
        {
            devices = deviceList ?? new List<DeviceData>();
            customers = customerList ?? new List<CustomerData>();
            storyLevels = levelList ?? new List<LevelData>();
            upgrades = upgradeList ?? new List<UpgradeData>();
            achievements = achievementList ?? new List<AchievementData>();
            challenges = challengeList ?? new List<ChallengeData>();
        }

        public LevelData GetLevelById(string levelId)
        {
            for (var i = 0; i < storyLevels.Count; i++)
            {
                if (storyLevels[i] != null && storyLevels[i].LevelId == levelId)
                {
                    return storyLevels[i];
                }
            }

            return null;
        }

        public DeviceData GetDeviceById(string deviceId)
        {
            for (var i = 0; i < devices.Count; i++)
            {
                if (devices[i] != null && devices[i].DeviceId == deviceId)
                {
                    return devices[i];
                }
            }

            return null;
        }
    }
}
