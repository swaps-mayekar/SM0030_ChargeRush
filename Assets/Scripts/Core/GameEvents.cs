using System;
using ChargeRush.Data;
using ChargeRush.Devices;

namespace ChargeRush.Core
{
    /// <summary>Static typed events used to keep systems loosely coupled.</summary>
    public static class GameEvents
    {
        public static event Action<int> EarningsChanged;
        public static event Action<int, int> MistakesChanged;
        public static event Action<int> StarsEarned;
        public static event Action<SessionState> SessionStateChanged;
        public static event Action<string> CustomerArrived;
        public static event Action<string> CustomerLeftAngry;
        public static event Action<DeviceInstance> DevicePickedUp;
        public static event Action<DeviceInstance> DevicePlacedCorrectly;
        public static event Action<DeviceInstance> IncorrectConnector;
        public static event Action<DeviceInstance> ChargingStarted;
        public static event Action<DeviceInstance> ChargingCompleted;
        public static event Action<DeviceInstance> DeviceReturned;
        public static event Action<int> PaymentReceived;
        public static event Action<string> AchievementUnlocked;
        public static event Action LevelCompleted;
        public static event Action<MistakeReason> LevelFailed;
        public static event Action<string> TutorialStepChanged;
        public static event Action SaveCompleted;

        public static void RaiseEarningsChanged(int value) => EarningsChanged?.Invoke(value);
        public static void RaiseMistakesChanged(int current, int max) => MistakesChanged?.Invoke(current, max);
        public static void RaiseStarsEarned(int stars) => StarsEarned?.Invoke(stars);
        public static void RaiseSessionStateChanged(SessionState state) => SessionStateChanged?.Invoke(state);
        public static void RaiseCustomerArrived(string id) => CustomerArrived?.Invoke(id);
        public static void RaiseCustomerLeftAngry(string id) => CustomerLeftAngry?.Invoke(id);
        public static void RaiseDevicePickedUp(DeviceInstance device) => DevicePickedUp?.Invoke(device);
        public static void RaiseDevicePlacedCorrectly(DeviceInstance device) => DevicePlacedCorrectly?.Invoke(device);
        public static void RaiseIncorrectConnector(DeviceInstance device) => IncorrectConnector?.Invoke(device);
        public static void RaiseChargingStarted(DeviceInstance device) => ChargingStarted?.Invoke(device);
        public static void RaiseChargingCompleted(DeviceInstance device) => ChargingCompleted?.Invoke(device);
        public static void RaiseDeviceReturned(DeviceInstance device) => DeviceReturned?.Invoke(device);
        public static void RaisePaymentReceived(int amount) => PaymentReceived?.Invoke(amount);
        public static void RaiseAchievementUnlocked(string id) => AchievementUnlocked?.Invoke(id);
        public static void RaiseLevelCompleted() => LevelCompleted?.Invoke();
        public static void RaiseLevelFailed(MistakeReason reason) => LevelFailed?.Invoke(reason);
        public static void RaiseTutorialStepChanged(string step) => TutorialStepChanged?.Invoke(step);
        public static void RaiseSaveCompleted() => SaveCompleted?.Invoke();

        public static void ClearAll()
        {
            EarningsChanged = null;
            MistakesChanged = null;
            StarsEarned = null;
            SessionStateChanged = null;
            CustomerArrived = null;
            CustomerLeftAngry = null;
            DevicePickedUp = null;
            DevicePlacedCorrectly = null;
            IncorrectConnector = null;
            ChargingStarted = null;
            ChargingCompleted = null;
            DeviceReturned = null;
            PaymentReceived = null;
            AchievementUnlocked = null;
            LevelCompleted = null;
            LevelFailed = null;
            TutorialStepChanged = null;
            SaveCompleted = null;
        }
    }
}
