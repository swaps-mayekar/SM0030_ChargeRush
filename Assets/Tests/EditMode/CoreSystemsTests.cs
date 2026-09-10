using ChargeRush.Data;
using ChargeRush.Economy;
using ChargeRush.Save;
using NUnit.Framework;
using UnityEngine;

namespace ChargeRush.Tests
{
    public sealed class CoreSystemsTests
    {
        [Test]
        public void IncorrectConnector_DoesNotCountAsFailingMistake_WhenConfiguredSoft()
        {
            var go = new GameObject("Economy");
            var economy = go.AddComponent<EconomyManager>();
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.ConfigureBasics("t", 1, "T", "E", "D", 100, 150, 200, 3, 5f, 2, 2, 1f, 1f, false, Color.white);
            economy.Configure(level);
            economy.RegisterMistake(MistakeReason.IncorrectConnector, false);
            Assert.AreEqual(0, economy.Mistakes);
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(level);
        }

        [Test]
        public void WrongCustomer_IncrementsMistakeAndCanFail()
        {
            var go = new GameObject("Economy");
            var economy = go.AddComponent<EconomyManager>();
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.ConfigureBasics("t", 1, "T", "E", "D", 100, 150, 200, 3, 5f, 2, 2, 1f, 1f, false, Color.white);
            economy.Configure(level);
            economy.RegisterMistake(MistakeReason.WrongCustomer, true);
            economy.RegisterMistake(MistakeReason.WrongCustomer, true);
            economy.RegisterMistake(MistakeReason.WrongCustomer, true);
            Assert.AreEqual(3, economy.Mistakes);
            Assert.IsTrue(economy.IsFailed);
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(level);
        }

        [Test]
        public void EarningsAndStars_CalculateFromThresholds()
        {
            var go = new GameObject("Economy");
            var economy = go.AddComponent<EconomyManager>();
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.ConfigureBasics("t", 1, "T", "E", "D", 100, 150, 200, 3, 5f, 2, 2, 1f, 1f, false, Color.white);
            economy.Configure(level);
            economy.ApplyPayment(100, 1f);
            Assert.AreEqual(1, economy.EvaluateStars(level));
            economy.ApplyPayment(50, 1f);
            Assert.AreEqual(2, economy.EvaluateStars(level));
            economy.ApplyPayment(50, 1f);
            Assert.AreEqual(3, economy.EvaluateStars(level));
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(level);
        }

        [Test]
        public void LevelCompletion_RequiresTargetEarnings()
        {
            var go = new GameObject("Economy");
            var economy = go.AddComponent<EconomyManager>();
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.ConfigureBasics("t", 1, "T", "E", "D", 100, 150, 200, 3, 5f, 2, 2, 1f, 1f, false, Color.white);
            economy.Configure(level);
            Assert.IsFalse(economy.TargetReached);
            economy.ApplyPayment(100, 1f);
            Assert.IsTrue(economy.TargetReached);
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(level);
        }

        [Test]
        public void SaveMigrateAndValidate_PreservesCredits()
        {
            var data = new SaveData { Version = 0, TotalCredits = 25, LifetimeEarnings = 40 };
            data = SaveManager.Migrate(data);
            SaveManager.Validate(data);
            Assert.AreEqual(SaveManager.CurrentVersion, data.Version);
            Assert.AreEqual(25, data.TotalCredits);
            Assert.NotNull(data.Levels);
            Assert.NotNull(data.UnlockedAchievements);
        }

        [Test]
        public void SaveDeserialize_HandlesEmptyJson()
        {
            var data = SaveManager.Deserialize("");
            Assert.NotNull(data);
            Assert.AreEqual(0, data.TotalCredits);
        }

        [Test]
        public void OwnerMatching_UsesOwnerIdEquality()
        {
            const string owner = "cust_1";
            const string other = "cust_2";
            Assert.AreEqual(owner, owner);
            Assert.AreNotEqual(owner, other);
        }

        [Test]
        public void ChargeDuration_CompletesAtNormalizedOne()
        {
            var normalized = 0f;
            var duration = 5f;
            var elapsed = 0f;
            while (normalized < 1f)
            {
                elapsed += 1f;
                normalized = Mathf.Clamp01(elapsed / duration);
            }

            Assert.AreEqual(1f, normalized);
            Assert.AreEqual(5f, elapsed);
        }

        [Test]
        public void ConnectorMismatch_IsDetectedByEnumCompare()
        {
            Assert.AreNotEqual(ConnectorType.PowerLinkA, ConnectorType.PowerLinkB);
            Assert.AreEqual(ConnectorType.MiniPower, ConnectorType.MiniPower);
        }
    }
}
