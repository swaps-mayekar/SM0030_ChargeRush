using ChargeRush.Core;
using ChargeRush.Customers;
using ChargeRush.Devices;
using UnityEngine;

namespace ChargeRush.Tutorial
{
    /// <summary>Progressive interactive tutorial used by Level 1.</summary>
    public sealed class TutorialDirector : MonoBehaviour
    {
        public enum Step
        {
            Disabled = 0,
            CustomerArrives = 1,
            TakeDevice = 2,
            ConnectPort = 3,
            WaitCharge = 4,
            ReturnDevice = 5,
            CollectPayment = 6,
            Completed = 7
        }

        [SerializeField] private bool enabledForLevel;
        [SerializeField] private GameObject highlightArrow;

        public Step CurrentStep { get; private set; } = Step.Disabled;
        public bool IsActive => enabledForLevel && CurrentStep != Step.Disabled && CurrentStep != Step.Completed;

        public void Begin(bool enable)
        {
            enabledForLevel = enable;
            CurrentStep = enable ? Step.CustomerArrives : Step.Disabled;
            UpdatePrompt();
        }

        public void NotifyCustomerSpawned()
        {
            if (CurrentStep == Step.CustomerArrives)
            {
                Advance(Step.TakeDevice);
            }
        }

        public void NotifyDevicePlaced(DeviceInstance device)
        {
            if (CurrentStep == Step.ConnectPort || CurrentStep == Step.TakeDevice)
            {
                Advance(Step.WaitCharge);
            }
        }

        public void NotifyChargeComplete(DeviceInstance device)
        {
            if (CurrentStep == Step.WaitCharge)
            {
                Advance(Step.ReturnDevice);
            }
        }

        public void NotifyDeviceReturned(DeviceInstance device)
        {
            if (CurrentStep == Step.ReturnDevice)
            {
                Advance(Step.CollectPayment);
            }
        }

        public void NotifyPayment()
        {
            if (CurrentStep == Step.CollectPayment)
            {
                Advance(Step.Completed);
            }
        }

        public bool CanDragDevice(DeviceInstance device)
        {
            if (!IsActive)
            {
                return true;
            }

            return CurrentStep == Step.TakeDevice || CurrentStep == Step.ConnectPort || CurrentStep == Step.ReturnDevice;
        }

        public bool CanPlaceOnPort(DeviceInstance device, Charging.ChargingPort port)
        {
            if (!IsActive)
            {
                return true;
            }

            return CurrentStep == Step.TakeDevice || CurrentStep == Step.ConnectPort;
        }

        public bool CanReturnToCustomer(DeviceInstance device, CustomerInstance customer)
        {
            if (!IsActive)
            {
                return true;
            }

            return CurrentStep == Step.ReturnDevice;
        }

        private void Advance(Step step)
        {
            CurrentStep = step;
            UpdatePrompt();
            if (step == Step.Completed)
            {
                enabledForLevel = false;
                if (highlightArrow != null)
                {
                    highlightArrow.SetActive(false);
                }
            }
        }

        private void UpdatePrompt()
        {
            string message;
            switch (CurrentStep)
            {
                case Step.CustomerArrives:
                    message = "Someone needs a charge!";
                    break;
                case Step.TakeDevice:
                    message = "Take the device.";
                    break;
                case Step.ConnectPort:
                    message = "Connect it to the matching power port.";
                    break;
                case Step.WaitCharge:
                    message = "Wait until it is fully charged.";
                    break;
                case Step.ReturnDevice:
                    message = "Return it to its owner.";
                    break;
                case Step.CollectPayment:
                    message = "Collect your payment.";
                    break;
                default:
                    message = string.Empty;
                    break;
            }

            GameEvents.RaiseTutorialStepChanged(message);
            if (highlightArrow != null)
            {
                highlightArrow.SetActive(IsActive && !string.IsNullOrEmpty(message));
            }

            if (CurrentStep == Step.TakeDevice)
            {
                // After first take prompt, allow connect guidance next.
                // ConnectPort is entered once the player starts interacting.
            }
        }

        private void Update()
        {
            if (CurrentStep == Step.TakeDevice)
            {
                // Soft transition: once device leaves customer, coach connect.
            }
        }

        public void NotifyDevicePickedFromCustomer()
        {
            if (CurrentStep == Step.TakeDevice)
            {
                Advance(Step.ConnectPort);
            }
        }
    }
}
