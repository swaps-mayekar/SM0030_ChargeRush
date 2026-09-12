using ChargeRush.Core;
using ChargeRush.Data;
using UnityEngine;

namespace ChargeRush.Economy
{
    /// <summary>Tracks session earnings, mistakes, and completion thresholds.</summary>
    public sealed class EconomyManager : MonoBehaviour
    {
        public int CurrentEarnings { get; private set; }
        public int Mistakes { get; private set; }
        public int CustomersServed { get; private set; }
        public int DevicesCharged { get; private set; }
        public int CurrentStreak { get; private set; }
        public int BestStreak { get; private set; }
        public bool CustomerLeftDuringLevel { get; private set; }
        public int MaximumMistakes { get; private set; } = 3;
        public int TargetEarnings { get; private set; }
        public int TwoStarThreshold { get; private set; }
        public int ThreeStarThreshold { get; private set; }
        public bool TargetReached => CurrentEarnings >= TargetEarnings;
        public bool IsFailed => Mistakes >= MaximumMistakes;

        public void Configure(LevelData level)
        {
            CurrentEarnings = 0;
            Mistakes = 0;
            CustomersServed = 0;
            DevicesCharged = 0;
            CurrentStreak = 0;
            BestStreak = 0;
            CustomerLeftDuringLevel = false;
            MaximumMistakes = level != null ? level.MaximumMistakes : 3;
            TargetEarnings = level != null ? level.TargetEarnings : 100;
            TwoStarThreshold = level != null ? level.TwoStarThreshold : 150;
            ThreeStarThreshold = level != null ? level.ThreeStarThreshold : 200;
            GameEvents.RaiseEarningsChanged(CurrentEarnings);
            GameEvents.RaiseMistakesChanged(Mistakes, MaximumMistakes);
        }

        public int ApplyPayment(int basePrice, float multiplier)
        {
            var amount = Mathf.Max(1, Mathf.RoundToInt(basePrice * multiplier));
            CurrentEarnings += amount;
            CustomersServed++;
            DevicesCharged++;
            CurrentStreak++;
            BestStreak = Mathf.Max(BestStreak, CurrentStreak);
            GameEvents.RaiseEarningsChanged(CurrentEarnings);
            return amount;
        }

        public void RegisterMistake(MistakeReason reason, bool countsTowardFail = true)
        {
            if (countsTowardFail)
            {
                Mistakes++;
                CurrentStreak = 0;
                GameEvents.RaiseMistakesChanged(Mistakes, MaximumMistakes);

                if (reason == MistakeReason.CustomerLeft)
                {
                    CustomerLeftDuringLevel = true;
                }
            }
        }

        public int EvaluateStars(LevelData level)
        {
            if (level == null)
            {
                return 0;
            }

            return level.EvaluateStars(CurrentEarnings, Mistakes, CustomerLeftDuringLevel);
        }
    }
}
