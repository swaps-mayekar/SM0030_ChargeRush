using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChargeRush.Data
{
    [Serializable]
    public sealed class UpgradeTier
    {
        public int CostCredits = 100;
        public float Value = 1f;
        public string Description = string.Empty;
    }

    /// <summary>Configurable upgrade definition.</summary>
    [CreateAssetMenu(fileName = "UpgradeData", menuName = "ChargeRush/Upgrade Data")]
    public sealed class UpgradeData : ScriptableObject
    {
        [SerializeField] private string upgradeId = "upgrade_capacity";
        [SerializeField] private string displayName = "Charging Capacity";
        [SerializeField] private UpgradeType upgradeType = UpgradeType.ChargingCapacity;
        [SerializeField] private List<UpgradeTier> tiers = new List<UpgradeTier>();

        public string UpgradeId => upgradeId;
        public string DisplayName => displayName;
        public UpgradeType UpgradeType => upgradeType;
        public IReadOnlyList<UpgradeTier> Tiers => tiers;
        public int MaxLevel => tiers.Count;

        public void Configure(string id, string name, UpgradeType type, List<UpgradeTier> tierList)
        {
            upgradeId = id;
            displayName = name;
            upgradeType = type;
            tiers = tierList ?? new List<UpgradeTier>();
        }

        public float GetValue(int level)
        {
            if (level <= 0 || tiers.Count == 0)
            {
                return upgradeType == UpgradeType.ChargingSpeed || upgradeType == UpgradeType.ServiceQuality
                    ? 1f
                    : 0f;
            }

            var index = Mathf.Clamp(level - 1, 0, tiers.Count - 1);
            return tiers[index].Value;
        }

        public int GetCost(int nextLevel)
        {
            if (nextLevel <= 0 || nextLevel > tiers.Count)
            {
                return int.MaxValue;
            }

            return tiers[nextLevel - 1].CostCredits;
        }
    }
}
