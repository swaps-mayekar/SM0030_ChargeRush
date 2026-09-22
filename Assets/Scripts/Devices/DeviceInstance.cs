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
        private Vector3 prefabScale;
        private Vector3 scaleBeforeDrag;

        // Docked phones should sit in the cradle, not cover the whole pod.
        private const float DockedWorldScaleFactor = 0.4f;
        private static readonly Vector3 DockedLocalOffset = new Vector3(0f, 0.08f, 0f);

        private void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            cachedCollider = GetComponent<Collider2D>();
            worldCamera = Camera.main;
            prefabScale = transform.localScale;
            scaleBeforeDrag = prefabScale;
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
            // Free any occupied port before clearing device state. Otherwise an
            // abandoned mid-charge phone leaves the dock permanently blocked.
            DetachFromPort();
            State = DeviceState.Inactive;
            Data = null;
            OwnerId = null;
            InstanceId = null;
            IsFullyCharged = false;
            ChargeNormalized = 0f;
            dragging = false;
            ApplyHeldPresentation();
            if (progressFill != null)
            {
                progressFill.localScale = new Vector3(0f, 1f, 1f);
                progressFill.gameObject.SetActive(false);
            }

            if (connectorBadge != null)
            {
                connectorBadge.gameObject.SetActive(false);
            }
        }

        public void SetHome(Transform parent, Vector3 worldPosition)
        {
            HomeParent = parent;
            HomePosition = worldPosition;
            transform.SetParent(parent, true);
            transform.position = worldPosition;
        }

        /// <summary>Shrink and seat the device in a charging cradle.</summary>
        public void ApplyDockedPresentation()
        {
            var parentLossy = transform.parent != null ? transform.parent.lossyScale.x : 1f;
            var desiredWorld = prefabScale.x * DockedWorldScaleFactor;
            transform.localScale = Vector3.one * (desiredWorld / Mathf.Max(0.01f, parentLossy));
            transform.localPosition = DockedLocalOffset;
            HomePosition = transform.position;
            if (spriteRenderer != null)
            {
                var order = OccupiedPort != null ? OccupiedPort.DrawOrder + 1 : 4;
                spriteRenderer.sortingOrder = order;
            }
        }

        /// <summary>Restore normal handheld / counter scale after leaving a dock.</summary>
        public void ApplyHeldPresentation()
        {
            transform.localScale = prefabScale;
            if (spriteRenderer != null)
            {
                spriteRenderer.sortingOrder = 8;
            }
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
            // Always lift to held size — docked scale must not carry into the drag.
            scaleBeforeDrag = prefabScale;
            ApplyHeldPresentation();
            transform.localScale = prefabScale * 1.08f;
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
            transform.localScale = scaleBeforeDrag;
        }

        public void ReturnHome()
        {
            if (HomeParent != null)
            {
                transform.SetParent(HomeParent, true);
            }

            transform.position = HomePosition;
            // If home is a dock socket, re-apply cradle scale; otherwise held size.
            if (OccupiedPort != null && HomeParent == OccupiedPort.SocketAnchor)
            {
                ApplyDockedPresentation();
            }
            else
            {
                ApplyHeldPresentation();
            }
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

        /// <summary>
        /// Clears both sides of the device/port link so docks cannot stay occupied
        /// after the device is returned, abandoned, or pooled.
        /// </summary>
        public void DetachFromPort()
        {
            var port = OccupiedPort;
            if (port == null)
            {
                return;
            }

            if (port.OccupiedDevice == this)
            {
                port.RemoveDevice();
                return;
            }

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

            // Keep production art readable; apply a light tint only.
            spriteRenderer.color = Color.Lerp(Color.white, Data.TintColor, 0.18f);
            if (State == DeviceState.FullyCharged && Data.FullyChargedSprite != null)
            {
                spriteRenderer.sprite = Data.FullyChargedSprite;
                spriteRenderer.color = Color.white;
            }
            else if (State == DeviceState.Charging && Data.ChargingSprite != null)
            {
                spriteRenderer.sprite = Data.ChargingSprite;
            }
            else if (Data.IdleSprite != null)
            {
                spriteRenderer.sprite = Data.IdleSprite;
            }

            if (cachedCollider is BoxCollider2D box && spriteRenderer.sprite != null)
            {
                box.size = spriteRenderer.sprite.bounds.size * 0.85f;
            }

            if (progressFill != null)
            {
                progressFill.gameObject.SetActive(State == DeviceState.Charging || State == DeviceState.FullyCharged);
            }

            if (connectorBadge != null)
            {
                connectorBadge.gameObject.SetActive(State != DeviceState.Completed && State != DeviceState.Inactive);
                var badge = ResolveConnectorBadge(Data.RequiredConnector);
                if (badge != null)
                {
                    connectorBadge.sprite = badge;
                }

                // Keep badge art colors; enlarge slightly so matching is readable.
                connectorBadge.color = Color.white;
                connectorBadge.transform.localScale = Vector3.one * 0.4f;
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

        public static Sprite ResolveConnectorBadge(ConnectorType type)
        {
            string path;
            switch (type)
            {
                case ConnectorType.PowerLinkA: path = "Art/Connectors/badge_powerlink_a"; break;
                case ConnectorType.PowerLinkB: path = "Art/Connectors/badge_powerlink_b"; break;
                case ConnectorType.MiniPower: path = "Art/Connectors/badge_minipower"; break;
                case ConnectorType.ProPower: path = "Art/Connectors/badge_propower"; break;
                default: path = "Art/Connectors/badge_universal"; break;
            }

            return Resources.Load<Sprite>(path);
        }
    }
}
