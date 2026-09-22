using System;
using System.IO;
using UnityEngine;
using ChargeRush.Core;

namespace ChargeRush.Save
{
    /// <summary>Versioned JSON save system with atomic write and backup recovery.</summary>
    public sealed class SaveManager : MonoBehaviour
    {
        public const int CurrentVersion = 1;
        private const string FileName = "chargerush_save.json";
        private const string BackupFileName = "chargerush_save.bak.json";

        public static SaveManager Instance { get; private set; }

        public SaveData Data { get; private set; } = new SaveData();

        private string FilePath => Path.Combine(Application.persistentDataPath, FileName);
        private string BackupPath => Path.Combine(Application.persistentDataPath, BackupFileName);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            Load();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                Save();
            }
        }

        private void OnApplicationQuit()
        {
            Save();
        }

        public void Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    Data = Deserialize(File.ReadAllText(FilePath));
                }
                else if (File.Exists(BackupPath))
                {
                    Data = Deserialize(File.ReadAllText(BackupPath));
                }
                else
                {
                    Data = CreateDefault();
                }

                Data = Migrate(Data);
                Validate(Data);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"ChargeRush save load failed, using defaults. {ex.Message}");
                Data = CreateDefault();
            }
        }

        public void Save()
        {
            try
            {
                Data.Version = CurrentVersion;
                var json = JsonUtility.ToJson(Data, true);
                var tempPath = FilePath + ".tmp";
                File.WriteAllText(tempPath, json);

                if (File.Exists(FilePath))
                {
                    File.Copy(FilePath, BackupPath, true);
                }

                if (File.Exists(FilePath))
                {
                    File.Delete(FilePath);
                }

                File.Move(tempPath, FilePath);
                GameEvents.RaiseSaveCompleted();
            }
            catch (Exception ex)
            {
                Debug.LogError($"ChargeRush save failed: {ex.Message}");
            }
        }

        public void ResetProgress()
        {
            Data = CreateDefault();
            Save();
        }

        public LevelProgressRecord GetOrCreateLevel(string levelId)
        {
            for (var i = 0; i < Data.Levels.Count; i++)
            {
                if (Data.Levels[i].LevelId == levelId)
                {
                    return Data.Levels[i];
                }
            }

            var record = new LevelProgressRecord { LevelId = levelId };
            Data.Levels.Add(record);
            return record;
        }

        public int GetUpgradeLevel(string upgradeId)
        {
            for (var i = 0; i < Data.Upgrades.Count; i++)
            {
                if (Data.Upgrades[i].UpgradeId == upgradeId)
                {
                    return Data.Upgrades[i].Level;
                }
            }

            return 0;
        }

        public void SetUpgradeLevel(string upgradeId, int level)
        {
            for (var i = 0; i < Data.Upgrades.Count; i++)
            {
                if (Data.Upgrades[i].UpgradeId == upgradeId)
                {
                    Data.Upgrades[i].Level = level;
                    return;
                }
            }

            Data.Upgrades.Add(new UpgradeLevelRecord { UpgradeId = upgradeId, Level = level });
        }

        public bool HasAchievement(string achievementId)
        {
            return Data.UnlockedAchievements.Contains(achievementId);
        }

        public void UnlockAchievement(string achievementId)
        {
            if (HasAchievement(achievementId))
            {
                return;
            }

            Data.UnlockedAchievements.Add(achievementId);
            GameEvents.RaiseAchievementUnlocked(achievementId);
        }

        public static SaveData CreateDefault()
        {
            return new SaveData();
        }

        public static SaveData Migrate(SaveData data)
        {
            if (data == null)
            {
                return CreateDefault();
            }

            if (data.Version < 1)
            {
                data.Version = 1;
            }

            data.Levels ??= new System.Collections.Generic.List<LevelProgressRecord>();
            data.UnlockedAchievements ??= new System.Collections.Generic.List<string>();
            data.CompletedChallenges ??= new System.Collections.Generic.List<string>();
            data.Upgrades ??= new System.Collections.Generic.List<UpgradeLevelRecord>();
            data.ChargedDeviceCategories ??= new System.Collections.Generic.List<string>();
            return data;
        }

        public static void Validate(SaveData data)
        {
            if (data == null)
            {
                return;
            }

            data.TotalCredits = Mathf.Max(0, data.TotalCredits);
            data.LifetimeEarnings = Mathf.Max(0, data.LifetimeEarnings);
        }

        public static SaveData Deserialize(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return CreateDefault();
            }

            return JsonUtility.FromJson<SaveData>(json) ?? CreateDefault();
        }
    }
}
