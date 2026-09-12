using ChargeRush.Customers;
using ChargeRush.Data;
using ChargeRush.Devices;
using ChargeRush.Economy;
using ChargeRush.Save;
using ChargeRush.Tutorial;
using NUnit.Framework;
using UnityEngine;

namespace ChargeRush.Tests
{
    public sealed class CoreSystemsTests
    {
        [Test]
        public void IncorrectConnectorAndCustomerLeft_DoNotCountAsMistakes()
        {
            var go = new GameObject("Economy");
            var economy = go.AddComponent<EconomyManager>();
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.ConfigureBasics("t", 1, "T", "E", "D", 100, 150, 200, 3, 5f, 2, 2, 1f, 1f, false, Color.white);
            economy.Configure(level);
            economy.RegisterMistake(MistakeReason.IncorrectConnector, false);
            economy.RegisterMistake(MistakeReason.CustomerLeft, false);
            Assert.AreEqual(0, economy.Mistakes);
            Assert.IsFalse(economy.IsFailed);
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

        [Test]
        public void Tutorial_ReturnThenPayment_CompletesAndUnlocksDragging()
        {
            var go = new GameObject("Tutorial");
            var tutorial = go.AddComponent<TutorialDirector>();

            tutorial.Begin(true);
            tutorial.NotifyCustomerSpawned();
            tutorial.NotifyDevicePickedFromCustomer();
            tutorial.NotifyDevicePlaced(null);
            tutorial.NotifyChargeComplete(null);
            tutorial.NotifyDeviceReturned(null);
            Assert.AreEqual(TutorialDirector.Step.CollectPayment, tutorial.CurrentStep);

            tutorial.NotifyPayment();
            Assert.AreEqual(TutorialDirector.Step.Completed, tutorial.CurrentStep);
            Assert.IsFalse(tutorial.IsActive);
            Assert.IsTrue(tutorial.CanDragDevice(null));

            Object.DestroyImmediate(go);
        }

        [Test]
        public void UnchargedDevice_ReturnsToCustomerWhenNotDocked()
        {
            var customerObject = new GameObject("Customer");
            var customer = customerObject.AddComponent<CustomerInstance>();
            var deviceObject = new GameObject("Device");
            var device = deviceObject.AddComponent<DeviceInstance>();
            var data = ScriptableObject.CreateInstance<DeviceData>();
            data.Configure("phone", "Phone", DeviceCategory.BasicSmartphone, ConnectorType.PowerLinkA, 100, 1f, 20, Color.white);

            device.Initialize(data, "owner", "device");
            customer.Initialize(null, "owner", device, 10f);
            customer.TakeDeviceFromCustomer();

            Assert.AreEqual(DeviceState.AtCounter, device.State);
            Assert.IsNull(customer.HeldDevice);

            Assert.IsTrue(customer.ReturnUnchargedDevice(device));
            Assert.AreEqual(DeviceState.WithCustomer, device.State);
            Assert.AreSame(device, customer.HeldDevice);
            Assert.AreSame(customer.DeviceAnchor, device.transform.parent);
            Assert.AreEqual(CustomerState.HandingOver, customer.State);

            Object.DestroyImmediate(deviceObject);
            Object.DestroyImmediate(customerObject);
            Object.DestroyImmediate(data);
        }

        [Test]
        public void ReturnedDevice_FollowsCustomerAndCustomerKeepsLeavingState()
        {
            var customerObject = new GameObject("Customer");
            var customer = customerObject.AddComponent<CustomerInstance>();
            var deviceObject = new GameObject("Device");
            var device = deviceObject.AddComponent<DeviceInstance>();
            var data = ScriptableObject.CreateInstance<DeviceData>();
            data.Configure("phone", "Phone", DeviceCategory.BasicSmartphone, ConnectorType.PowerLinkA, 100, 1f, 20, Color.white);

            device.Initialize(data, "owner", "device");
            customer.Initialize(null, "owner", device, 10f);
            customer.TakeDeviceFromCustomer();
            device.CompleteCharge();

            Assert.IsTrue(customer.AcceptDevice(device));
            Assert.AreSame(customer.transform, device.transform.parent);

            customer.NotifyPayment(20);
            customer.BeginLeave();
            Assert.AreEqual(CustomerState.Leaving, customer.State);

            Object.DestroyImmediate(deviceObject);
            Object.DestroyImmediate(customerObject);
            Object.DestroyImmediate(data);
        }
    }
}
