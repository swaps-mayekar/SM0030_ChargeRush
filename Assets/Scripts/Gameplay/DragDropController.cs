using ChargeRush.Charging;
using ChargeRush.Core;
using ChargeRush.Customers;
using ChargeRush.Data;
using ChargeRush.Devices;
using ChargeRush.Economy;
using ChargeRush.Input;
using ChargeRush.Tutorial;
using UnityEngine;

namespace ChargeRush.Gameplay
{
    /// <summary>Handles pointer drag-and-drop between customers, counter, and ports.</summary>
    public sealed class DragDropController : MonoBehaviour
    {
        [SerializeField] private Camera gameplayCamera;
        [SerializeField] private ChargingStation chargingStation;
        [SerializeField] private CustomerQueue customerQueue;
        [SerializeField] private Transform counterDropZone;
        [SerializeField] private EconomyManager economy;
        [SerializeField] private TutorialDirector tutorial;

        private DeviceInstance draggedDevice;
        private InputManager input;

        public void Configure(
            Camera cam,
            ChargingStation station,
            CustomerQueue queue,
            Transform counter,
            EconomyManager economyManager,
            TutorialDirector tutorialDirector)
        {
            gameplayCamera = cam;
            chargingStation = station;
            customerQueue = queue;
            counterDropZone = counter;
            economy = economyManager;
            tutorial = tutorialDirector;
        }

        private void OnEnable()
        {
            input = InputManager.Instance;
            if (input != null)
            {
                input.PointerPressed += OnPressed;
                input.PointerMoved += OnMoved;
                input.PointerReleased += OnReleased;
            }
        }

        private void OnDisable()
        {
            if (input != null)
            {
                input.PointerPressed -= OnPressed;
                input.PointerMoved -= OnMoved;
                input.PointerReleased -= OnReleased;
            }
        }

        private void OnPressed(Vector2 screenPosition)
        {
            if (gameplayCamera == null)
            {
                gameplayCamera = Camera.main;
            }

            draggedDevice = FindDevice(screenPosition);
            if (draggedDevice == null || !draggedDevice.CanDrag)
            {
                draggedDevice = null;
                return;
            }

            if (tutorial != null && !tutorial.CanDragDevice(draggedDevice))
            {
                draggedDevice = null;
                return;
            }

            // Take from customer if still held.
            if (draggedDevice.State == DeviceState.WithCustomer)
            {
                var owner = FindOwner(draggedDevice.OwnerId);
                if (owner != null)
                {
                    owner.TakeDeviceFromCustomer();
                }

                if (counterDropZone != null)
                {
                    draggedDevice.SetHome(counterDropZone, counterDropZone.position + Vector3.up * 0.4f);
                }

                if (tutorial != null)
                {
                    tutorial.NotifyDevicePickedFromCustomer();
                }
            }
            else if (draggedDevice.OccupiedPort != null && draggedDevice.State == DeviceState.FullyCharged)
            {
                draggedDevice.OccupiedPort.RemoveDevice();
            }

            draggedDevice.OnDragBegin();
        }

        private void OnMoved(Vector2 screenPosition)
        {
            if (draggedDevice != null)
            {
                draggedDevice.OnDrag(screenPosition);
            }
        }

        private void OnReleased(Vector2 screenPosition)
        {
            if (draggedDevice == null)
            {
                return;
            }

            var device = draggedDevice;
            device.OnDragEnd(screenPosition);
            draggedDevice = null;

            if (device.State == DeviceState.FullyCharged || device.IsFullyCharged)
            {
                TryReturnToCustomer(device, screenPosition);
                return;
            }

            TryPlaceOnPort(device, screenPosition);
        }

        private void TryPlaceOnPort(DeviceInstance device, Vector2 screenPosition)
        {
            var port = chargingStation != null ? chargingStation.FindPortAtScreen(screenPosition, gameplayCamera) : null;
            if (port == null)
            {
                device.ReturnHome();
                return;
            }

            if (tutorial != null && !tutorial.CanPlaceOnPort(device, port))
            {
                device.ReturnHome();
                return;
            }

            if (!port.CanAccept(device))
            {
                device.ReturnHome();
                GameEvents.RaiseIncorrectConnector(device);
                if (economy != null)
                {
                    economy.RegisterMistake(MistakeReason.IncorrectConnector, false);
                }

                return;
            }

            var duration = device.Data.ChargingDurationSeconds;
            if (LevelSession.Instance != null)
            {
                duration = LevelSession.Instance.GetAdjustedChargeDuration(duration);
            }

            port.TryPlace(device, duration);
            if (tutorial != null)
            {
                tutorial.NotifyDevicePlaced(device);
            }
        }

        private void TryReturnToCustomer(DeviceInstance device, Vector2 screenPosition)
        {
            var customer = customerQueue != null ? customerQueue.FindAtScreen(screenPosition, gameplayCamera) : null;
            if (customer == null)
            {
                device.ReturnHome();
                return;
            }

            if (tutorial != null && !tutorial.CanReturnToCustomer(device, customer))
            {
                device.ReturnHome();
                return;
            }

            if (customer.OwnerId != device.OwnerId)
            {
                device.ReturnHome();
                if (economy != null)
                {
                    economy.RegisterMistake(MistakeReason.WrongCustomer, true);
                }

                return;
            }

            if (!customer.AcceptDevice(device))
            {
                device.ReturnHome();
                return;
            }

            GameEvents.RaiseDeviceReturned(device);
            var payment = economy != null
                ? economy.ApplyPayment(device.Data.ServicePrice, customer.Data.PaymentMultiplier)
                : device.Data.ServicePrice;
            device.SetState(DeviceState.Completed);
            customer.BeginLeave();
            if (tutorial != null)
            {
                tutorial.NotifyDeviceReturned(device);
            }

            // Emit payment after the tutorial enters CollectPayment so the same
            // transaction can complete that step instead of leaving input locked.
            customer.NotifyPayment(payment);
        }

        private DeviceInstance FindDevice(Vector2 screenPosition)
        {
            var devices = FindObjectsByType<DeviceInstance>(FindObjectsSortMode.None);
            for (var i = 0; i < devices.Length; i++)
            {
                if (devices[i] != null && devices[i].gameObject.activeInHierarchy && devices[i].ContainsScreenPoint(screenPosition))
                {
                    return devices[i];
                }
            }

            return null;
        }

        private CustomerInstance FindOwner(string ownerId)
        {
            if (customerQueue == null)
            {
                return null;
            }

            for (var i = 0; i < customerQueue.Customers.Count; i++)
            {
                if (customerQueue.Customers[i] != null && customerQueue.Customers[i].OwnerId == ownerId)
                {
                    return customerQueue.Customers[i];
                }
            }

            return null;
        }
    }
}
