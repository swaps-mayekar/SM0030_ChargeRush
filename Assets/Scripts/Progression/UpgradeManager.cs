using ChargeRush.Data;
using ChargeRush.Save;
using UnityEngine;

namespace ChargeRush.Progression
{
    /// <summary>Applies optional upgrade modifiers to runtime gameplay values.</summary>
    public sealed class UpgradeManager : MonoBehaviour
    {
        [SerializeField] private GameCatalog catalog;

        public void SetCatalog(GameCatalog gameCatalog)
        {
            catalog = gameCatalog;
        }

        public int GetExtraPorts()
        {
            return Mathf.RoundToInt(GetValue(UpgradeType.ChargingCapacity));
        }

        public float GetChargeSpeedMultiplier()
        {
            var value = GetValue(UpgradeType.ChargingSpeed);
            return value <= 0f ? 1f : value;
        }

        public int GetExtraQueueSlots()
        {
            return Mathf.RoundToInt(GetValue(UpgradeType.QueueCapacity));
        }

        public float GetPatienceMultiplier()
        {
            var value = GetValue(UpgradeType.ServiceQuality);
            return value <= 0f ? 1f : value;
        }

        public bool HasProfessionalEquipment()
        {
            return GetUpgradeLevel(UpgradeType.ProfessionalEquipment) > 0;
        }

        public bool TryPurchase(UpgradeData upgrade)
        {
            if (upgrade == null || SaveManager.Instance == null)
            {
                return false;
            }

            var current = SaveManager.Instance.GetUpgradeLevel(upgrade.UpgradeId);
            if (current >= upgrade.MaxLevel)
            {
                return false;
            }

            var cost = upgrade.GetCost(current + 1);
            if (SaveManager.Instance.Data.TotalCredits < cost)
            {
                return false;
            }

            SaveManager.Instance.Data.TotalCredits -= cost;
            SaveManager.Instance.SetUpgradeLevel(upgrade.UpgradeId, current + 1);
            SaveManager.Instance.Save();
            return true;
        }

        private float GetValue(UpgradeType type)
        {
            var upgrade = FindUpgrade(type);
            if (upgrade == null || SaveManager.Instance == null)
            {
                return type == UpgradeType.ChargingSpeed || type == UpgradeType.ServiceQuality ? 1f : 0f;
            }

            return upgrade.GetValue(SaveManager.Instance.GetUpgradeLevel(upgrade.UpgradeId));
        }

        private int GetUpgradeLevel(UpgradeType type)
        {
            var upgrade = FindUpgrade(type);
            if (upgrade == null || SaveManager.Instance == null)
            {
                return 0;
            }

            return SaveManager.Instance.GetUpgradeLevel(upgrade.UpgradeId);
        }

        private UpgradeData FindUpgrade(UpgradeType type)
        {
            if (catalog == null)
            {
                return null;
            }

            for (var i = 0; i < catalog.Upgrades.Count; i++)
            {
                if (catalog.Upgrades[i] != null && catalog.Upgrades[i].UpgradeType == type)
                {
                    return catalog.Upgrades[i];
                }
            }

            return null;
        }
    }
}
