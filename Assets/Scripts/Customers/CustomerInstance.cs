using ChargeRush.Core;
using ChargeRush.Data;
using ChargeRush.Devices;
using ChargeRush.Interfaces;
using UnityEngine;

namespace ChargeRush.Customers
{
    /// <summary>Runtime customer actor with patience and service states.</summary>
    public sealed class CustomerInstance : MonoBehaviour, ICustomerService
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Transform patienceFill;
        [SerializeField] private Transform deviceAnchor;
        [SerializeField] private SpriteRenderer moodBadge;

        public string OwnerId { get; private set; }
        public CustomerData Data { get; private set; }
        public CustomerState State { get; private set; } = CustomerState.Inactive;
        public DeviceInstance HeldDevice { get; private set; }
        public DeviceInstance ServicedDevice { get; private set; }
        public float PatienceNormalized { get; private set; } = 1f;
        public PatienceMood Mood { get; private set; } = PatienceMood.Happy;
        public int SlotIndex { get; private set; } = -1;
        public bool IsInOverflow { get; private set; }

        private float maxPatience;
        private float remainingPatience;
        private bool patiencePaused;
        private Vector3 targetPosition;
        private float moveSpeed = 4.5f;

        private void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }
        }

        public Transform DeviceAnchor => deviceAnchor != null ? deviceAnchor : transform;

        public void Initialize(CustomerData data, string ownerId, DeviceInstance device, float patienceSeconds)
        {
            Data = data;
            OwnerId = ownerId;
            HeldDevice = device;
            ServicedDevice = null;
            maxPatience = Mathf.Max(5f, patienceSeconds);
            remainingPatience = maxPatience;
            PatienceNormalized = 1f;
            Mood = PatienceMood.Happy;
            patiencePaused = false;
            SlotIndex = -1;
            IsInOverflow = false;
            State = CustomerState.Approaching;
            ApplyVisual();

            if (device != null)
            {
                device.SetHome(DeviceAnchor, DeviceAnchor.position);
                device.SetState(DeviceState.WithCustomer);
            }
        }

        public void ResetForPool()
        {
            State = CustomerState.Inactive;
            Data = null;
            OwnerId = null;
            HeldDevice = null;
            ServicedDevice = null;
            SlotIndex = -1;
            IsInOverflow = false;
            patiencePaused = false;
            Mood = PatienceMood.Happy;
            PatienceNormalized = 1f;
        }

        public void AssignSlot(int index, Vector3 worldPosition, bool overflow)
        {
            SlotIndex = index;
            IsInOverflow = overflow;
            targetPosition = worldPosition;
            if (State == CustomerState.Approaching || State == CustomerState.Waiting || State == CustomerState.DeviceAtCounter || State == CustomerState.WaitingForDevice)
            {
                // Keep current service state while relocating.
            }
        }

        public void SetState(CustomerState state)
        {
            State = state;
            if (state == CustomerState.HandingOver || state == CustomerState.WaitingForDevice || state == CustomerState.ReceivingDevice || state == CustomerState.Paying)
            {
                patiencePaused = true;
            }
            else if (state == CustomerState.Waiting || state == CustomerState.DeviceAtCounter)
            {
                patiencePaused = false;
            }
        }

        public void Tick(float deltaTime)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * deltaTime);

            if (State == CustomerState.Approaching && Vector3.Distance(transform.position, targetPosition) < 0.05f)
            {
                SetState(IsInOverflow ? CustomerState.Waiting : CustomerState.Waiting);
                if (!IsInOverflow && HeldDevice != null)
                {
                    SetState(CustomerState.HandingOver);
                }
            }

            if (!patiencePaused &&
                (State == CustomerState.Waiting || State == CustomerState.DeviceAtCounter || State == CustomerState.WaitingForDevice || State == CustomerState.HandingOver))
            {
                remainingPatience -= deltaTime;
                PatienceNormalized = Mathf.Clamp01(remainingPatience / maxPatience);
                UpdateMood();
                if (remainingPatience <= 0f)
                {
                    BeginAngryLeave();
                }
            }

            if (patienceFill != null)
            {
                patienceFill.localScale = new Vector3(PatienceNormalized, 1f, 1f);
            }
        }

        public DeviceInstance TakeDeviceFromCustomer()
        {
            if (HeldDevice == null)
            {
                return null;
            }

            var device = HeldDevice;
            HeldDevice = null;
            ServicedDevice = device;
            device.SetState(DeviceState.AtCounter);
            SetState(CustomerState.DeviceAtCounter);
            return device;
        }

        public bool AcceptDevice(DeviceInstance device)
        {
            if (device == null || device.OwnerId != OwnerId || !device.IsFullyCharged)
            {
                return false;
            }

            ServicedDevice = device;
            device.SetState(DeviceState.ReturningToCustomer);
            SetState(CustomerState.ReceivingDevice);
            return true;
        }

        public void NotifyPayment(int amount)
        {
            SetState(CustomerState.Paying);
            GameEvents.RaisePaymentReceived(amount);
        }

        public void BeginLeave()
        {
            SetState(CustomerState.Leaving);
            targetPosition = transform.position + Vector3.right * 8f;
        }

        public void BeginAngryLeave()
        {
            if (State == CustomerState.AngryLeaving || State == CustomerState.Leaving)
            {
                return;
            }

            SetState(CustomerState.AngryLeaving);
            Mood = PatienceMood.Leaves;
            targetPosition = transform.position + Vector3.left * 8f;
            GameEvents.RaiseCustomerLeftAngry(OwnerId);
        }

        public bool HasLeftScreen()
        {
            return (State == CustomerState.Leaving || State == CustomerState.AngryLeaving) &&
                   Vector3.Distance(transform.position, targetPosition) < 0.1f;
        }

        public bool ContainsScreenPoint(Vector2 screenPosition, Camera camera)
        {
            var collider = GetComponent<Collider2D>();
            if (collider == null || camera == null)
            {
                return false;
            }

            var world = camera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, Mathf.Abs(camera.transform.position.z)));
            return collider.OverlapPoint(world);
        }

        private void UpdateMood()
        {
            if (PatienceNormalized > 0.7f)
            {
                Mood = PatienceMood.Happy;
            }
            else if (PatienceNormalized > 0.45f)
            {
                Mood = PatienceMood.Waiting;
            }
            else if (PatienceNormalized > 0.2f)
            {
                Mood = PatienceMood.BecomingImpatient;
            }
            else
            {
                Mood = PatienceMood.VeryImpatient;
            }

            if (moodBadge != null)
            {
                moodBadge.color = MoodColor(Mood);
            }
        }

        private void ApplyVisual()
        {
            if (spriteRenderer == null || Data == null)
            {
                return;
            }

            spriteRenderer.color = Color.white;
            if (Data.PortraitSprite != null)
            {
                spriteRenderer.sprite = Data.PortraitSprite;
            }
        }

        private static Color MoodColor(PatienceMood mood)
        {
            switch (mood)
            {
                case PatienceMood.Happy: return new Color(0.35f, 0.85f, 0.4f);
                case PatienceMood.Waiting: return new Color(0.95f, 0.85f, 0.25f);
                case PatienceMood.BecomingImpatient: return new Color(1f, 0.55f, 0.2f);
                case PatienceMood.VeryImpatient: return new Color(0.95f, 0.25f, 0.2f);
                default: return Color.red;
            }
        }
    }
}
