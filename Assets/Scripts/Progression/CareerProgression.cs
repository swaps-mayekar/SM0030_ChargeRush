using ChargeRush.Data;
using ChargeRush.Save;
using UnityEngine;

namespace ChargeRush.Progression
{
    /// <summary>Maps story progress to career rank labels.</summary>
    public sealed class CareerProgression
    {
        public static CareerRank EvaluateRank(int completedLevels)
        {
            if (completedLevels >= 10) return CareerRank.MajorEventService;
            if (completedLevels >= 8) return CareerRank.PremiumChargingService;
            if (completedLevels >= 6) return CareerRank.ProfessionalChargingBooth;
            if (completedLevels >= 4) return CareerRank.EventChargingService;
            if (completedLevels >= 2) return CareerRank.SmallChargingService;
            if (completedLevels >= 1) return CareerRank.LocalChargingHelper;
            return CareerRank.Unemployed;
        }

        public static string ToDisplay(CareerRank rank)
        {
            switch (rank)
            {
                case CareerRank.LocalChargingHelper: return "Local Charging Helper";
                case CareerRank.SmallChargingService: return "Small Charging Service";
                case CareerRank.EventChargingService: return "Event Charging Service";
                case CareerRank.ProfessionalChargingBooth: return "Professional Charging Booth";
                case CareerRank.PremiumChargingService: return "Premium Charging Service";
                case CareerRank.MajorEventService: return "Major Event Service";
                default: return "Unemployed";
            }
        }

        public static void RefreshFromSave()
        {
            if (SaveManager.Instance == null)
            {
                return;
            }

            var rank = EvaluateRank(SaveManager.Instance.Data.StoryLevelsCompleted);
            SaveManager.Instance.Data.CareerRank = ToDisplay(rank);
        }
    }
}
