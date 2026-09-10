using ChargeRush.Core;
using ChargeRush.Data;
using ChargeRush.Progression;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;

namespace ChargeRush.Tests
{
    public sealed class PlayModeSmokeTests
    {
        [UnityTest]
        public IEnumerator CareerRank_AdvancesWithCompletedLevels()
        {
            Assert.AreEqual(CareerRank.Unemployed, CareerProgression.EvaluateRank(0));
            Assert.AreEqual(CareerRank.LocalChargingHelper, CareerProgression.EvaluateRank(1));
            Assert.AreEqual(CareerRank.MajorEventService, CareerProgression.EvaluateRank(10));
            yield return null;
        }

        [UnityTest]
        public IEnumerator SceneLoaderConstants_AreDefined()
        {
            Assert.AreEqual("Boot", SceneLoader.BootScene);
            Assert.AreEqual("Gameplay", SceneLoader.GameplayScene);
            yield return null;
        }

        [Test]
        public void LevelStarEvaluation_SupportsExcellentService()
        {
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.ConfigureBasics("t", 1, "T", "E", "D", 100, 150, 200, 3, 5f, 2, 2, 1f, 1f, false, Color.white);
            Assert.AreEqual(3, level.EvaluateStars(220, 0, false));
            Assert.AreEqual(0, level.EvaluateStars(50, 0, false));
            Object.DestroyImmediate(level);
        }
    }
}
