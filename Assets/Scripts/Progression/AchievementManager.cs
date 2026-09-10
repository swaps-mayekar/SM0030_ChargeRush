using ChargeRush.Data;
using ChargeRush.Save;
using UnityEngine;

namespace ChargeRush.Progression
{
    /// <summary>Evaluates achievement progress from session and lifetime stats.</summary>
    public sealed class AchievementManager : MonoBehaviour
    {
        [SerializeField] private GameCatalog catalog;

        public void SetCatalog(GameCatalog gameCatalog)
        {
            catalog = gameCatalog;
        }

        public void EvaluateAfterSession(int sessionCustomers, int sessionDevices, int stars, bool zeroMistakes, bool levelCompleted, DeviceCategory? chargedCategory)
        {
            var save = SaveManager.Instance;
            if (save == null || catalog == null)
            {
                return;
            }

            if (chargedCategory.HasValue)
            {
                var key = chargedCategory.Value.ToString();
                if (!save.Data.ChargedDeviceCategories.Contains(key))
                {
                    save.Data.ChargedDeviceCategories.Add(key);
                }
            }

            TryUnlock("first_charge", save.Data.CustomersServed >= 1);
            TryUnlock("getting_started", save.Data.LifetimeEarnings >= 500);
            TryUnlock("busy_counter", save.Data.CustomersServed >= 25);
            TryUnlock("perfect_service", zeroMistakes && levelCompleted);
            TryUnlock("fully_charged", save.Data.DevicesCharged >= 100);
            TryUnlock("speed_service", save.Data.BestStreak >= 10);
            TryUnlock("device_expert", save.Data.ChargedDeviceCategories.Count >= 7);
            TryUnlock("three_star_service", save.Data.ThreeStarLevels >= 5);
            TryUnlock("charging_professional", save.Data.StoryLevelsCompleted >= 10);
            TryUnlock("charging_tycoon", save.Data.LifetimeEarnings >= 100000);
        }

        private void TryUnlock(string id, bool condition)
        {
            if (!condition || SaveManager.Instance == null)
            {
                return;
            }

            SaveManager.Instance.UnlockAchievement(id);
        }
    }
}
