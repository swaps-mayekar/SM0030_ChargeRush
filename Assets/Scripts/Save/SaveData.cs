using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChargeRush.Save
{
    [Serializable]
    public sealed class LevelProgressRecord
    {
        public string LevelId;
        public bool Completed;
        public int Stars;
        public int BestEarnings;
    }

    [Serializable]
    public sealed class SaveData
    {
        public int Version = SaveManager.CurrentVersion;
        public int TotalCredits;
        public int LifetimeEarnings;
        public int CustomersServed;
        public int DevicesCharged;
        public int PerfectLevels;
        public int ThreeStarLevels;
        public int CurrentStreak;
        public int BestStreak;
        public int StoryLevelsCompleted;
        public bool TutorialCompleted;
        public string CareerRank = "Unemployed";
        public bool ReplayTutorialOnNextLevelOne;
        public int EndlessBestEarnings;
        public int EndlessBestCustomers;
        public int EndlessBestStreak;
        public List<LevelProgressRecord> Levels = new List<LevelProgressRecord>();
        public List<string> UnlockedAchievements = new List<string>();
        public List<string> CompletedChallenges = new List<string>();
        public List<UpgradeLevelRecord> Upgrades = new List<UpgradeLevelRecord>();
        public List<string> ChargedDeviceCategories = new List<string>();
    }

    [Serializable]
    public sealed class UpgradeLevelRecord
    {
        public string UpgradeId;
        public int Level;
    }
}
