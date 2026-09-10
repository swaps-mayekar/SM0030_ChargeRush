using ChargeRush.Core;
using ChargeRush.Data;
using ChargeRush.Interfaces;
using UnityEngine;

namespace ChargeRush.Devices
{
    /// <summary>Runtime device actor supporting drag, charge, and owner matching.</summary>
    public sealed class DeviceInstance : MonoBehaviour, IDraggable, IChargeable
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Transform progressFill;
        [SerializeField] private SpriteRenderer connectorBadge;

        public string InstanceId { get; private set; }
        public string OwnerId { get; private set; }
        public DeviceData Data { get; private set; }
        public DeviceState State { get; private set; } = DeviceState.Inactive;
        public bool IsFullyCharged { get; private set; }
        public float ChargeNormalized { get; private set; }
        public bool CanDrag => State == DeviceState.AtCounter || State == DeviceState.FullyCharged || State == DeviceState.WithCustomer;
        public Vector3 HomePosition { get; private set; }
        public Transform HomeParent { get; private set; }
        public Charging.ChargingPort OccupiedPort { get; private set; }

        private float chargeDuration;
        private float chargeElapsed;
        private bool dragging;
        private Camera worldCamera;
        private Collider2D cachedCollider;

        private void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            cachedCollider = GetComponent<Collider2D>();
            worldCamera = Camera.main;
        }

        public void Initialize(DeviceData data, string ownerId, string instanceId)
        {
            Data = data;
            OwnerId = ownerId;
            InstanceId = instanceId;
            IsFullyCharged = false;
            ChargeNormalized = 0f;
            chargeElapsed = 0f;
            OccupiedPort = null;
            State = DeviceState.WithCustomer;
            ApplyVisual();
        }

        public void ResetForPool()
        {
            State = DeviceState.Inactive;
            Data = null;
            OwnerId = null;
            InstanceId = null;
            IsFullyCharged = false;
            ChargeNormalized = 0f;
            OccupiedPort = null;
            dragging = false;
            transform.localScale = Vector3.one;
            if (progressFill != null)
            {
                progressFill.localScale = new Vector3(0f, 1f, 1f);
            }
        }

        public void SetHome(Transform parent, Vector3 worldPosition)
        {
            HomeParent = parent;
            HomePosition = worldPosition;
            transform.SetParent(parent, true);
            transform.position = worldPosition;
        }

        public void SetState(DeviceState state)
        {
            State = state;
            ApplyVisual();
        }

        public void OnDragBegin()
        {
            if (!CanDrag)
            {
                return;
            }

            dragging = true;
            HomePosition = transform.position;
            HomeParent = transform.parent;
            transform.SetParent(null, true);
            transform.localScale = Vector3.one * 1.08f;
            GameEvents.RaiseDevicePickedUp(this);
        }

        public void OnDrag(Vector2 screenPosition)
        {
            if (!dragging)
            {
                return;
            }

            if (worldCamera == null)
            {
                worldCamera = Camera.main;
            }

            if (worldCamera == null)
            {
                return;
            }

            var world = worldCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, Mathf.Abs(worldCamera.transform.position.z)));
            world.z = 0f;
            transform.position = world;
        }

        public void OnDragEnd(Vector2 screenPosition)
        {
            if (!dragging)
            {
                return;
            }

            dragging = false;
            transform.localScale = Vector3.one;
        }

        public void ReturnHome()
        {
            if (HomeParent != null)
            {
                transform.SetParent(HomeParent, true);
            }

            transform.position = HomePosition;
        }

        public void BeginCharge(float durationSeconds)
        {
            chargeDuration = Mathf.Max(0.1f, durationSeconds);
            chargeElapsed = 0f;
            IsFullyCharged = false;
            ChargeNormalized = 0f;
            SetState(DeviceState.Charging);
            GameEvents.RaiseChargingStarted(this);
        }

        public void TickCharge(float deltaTime)
        {
            if (State != DeviceState.Charging)
            {
                return;
            }

            chargeElapsed += deltaTime;
            ChargeNormalized = Mathf.Clamp01(chargeElapsed / chargeDuration);
            if (progressFill != null)
            {
                progressFill.localScale = new Vector3(ChargeNormalized, 1f, 1f);
            }

            if (ChargeNormalized >= 1f)
            {
                CompleteCharge();
            }
        }

        public void CompleteCharge()
        {
            IsFullyCharged = true;
            ChargeNormalized = 1f;
            SetState(DeviceState.FullyCharged);
            GameEvents.RaiseChargingCompleted(this);
        }

        public void AssignPort(Charging.ChargingPort port)
        {
            OccupiedPort = port;
        }

        public void ClearPort()
        {
            OccupiedPort = null;
        }

        public bool ContainsScreenPoint(Vector2 screenPosition)
        {
            if (cachedCollider == null || worldCamera == null)
            {
                worldCamera = Camera.main;
                if (worldCamera == null || cachedCollider == null)
                {
                    return false;
                }
            }

            var world = worldCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, Mathf.Abs(worldCamera.transform.position.z)));
            return cachedCollider.OverlapPoint(world);
        }

        private void ApplyVisual()
        {
            if (spriteRenderer == null || Data == null)
            {
                return;
            }

            spriteRenderer.color = Data.TintColor;
            if (State == DeviceState.Charging && Data.ChargingSprite != null)
            {
                spriteRenderer.sprite = Data.ChargingSprite;
            }
            else if (State == DeviceState.FullyCharged && Data.FullyChargedSprite != null)
            {
                spriteRenderer.sprite = Data.FullyChargedSprite;
            }
            else if (Data.IdleSprite != null)
            {
                spriteRenderer.sprite = Data.IdleSprite;
            }

            if (connectorBadge != null)
            {
                connectorBadge.color = ConnectorColor(Data.RequiredConnector);
            }
        }

        public static Color ConnectorColor(ConnectorType type)
        {
            switch (type)
            {
                case ConnectorType.PowerLinkA: return new Color(0.2f, 0.7f, 1f);
                case ConnectorType.PowerLinkB: return new Color(0.3f, 0.9f, 0.4f);
                case ConnectorType.MiniPower: return new Color(1f, 0.75f, 0.2f);
                case ConnectorType.ProPower: return new Color(0.85f, 0.35f, 0.95f);
                default: return new Color(0.9f, 0.9f, 0.9f);
            }
        }
    }
}
