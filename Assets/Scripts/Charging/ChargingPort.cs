using ChargeRush.Core;
using ChargeRush.Data;
using ChargeRush.Devices;
using UnityEngine;

namespace ChargeRush.Charging
{
    /// <summary>Single charging port that accepts a matching connector.</summary>
    public sealed class ChargingPort : MonoBehaviour
    {
        [SerializeField] private ConnectorType connectorType = ConnectorType.PowerLinkA;
        [SerializeField] private SpriteRenderer portRenderer;
        [SerializeField] private SpriteRenderer highlightRenderer;
        [SerializeField] private Transform socketAnchor;

        public ConnectorType ConnectorType => connectorType;
        public bool IsOccupied => OccupiedDevice != null;
        public DeviceInstance OccupiedDevice { get; private set; }
        public Transform SocketAnchor => socketAnchor != null ? socketAnchor : transform;

        public void Configure(ConnectorType type, Sprite sprite)
        {
            connectorType = type;
            if (portRenderer == null)
            {
                portRenderer = GetComponent<SpriteRenderer>();
            }

            if (portRenderer != null)
            {
                portRenderer.sprite = sprite;
                portRenderer.color = DeviceInstance.ConnectorColor(type);
            }
        }

        public bool CanAccept(DeviceInstance device)
        {
            return device != null && !IsOccupied && device.Data != null && device.Data.RequiredConnector == connectorType;
        }

        public bool TryPlace(DeviceInstance device, float chargeDuration)
        {
            if (!CanAccept(device))
            {
                return false;
            }

            OccupiedDevice = device;
            device.AssignPort(this);
            device.SetHome(SocketAnchor, SocketAnchor.position);
            device.transform.position = SocketAnchor.position;
            device.BeginCharge(chargeDuration);
            GameEvents.RaiseDevicePlacedCorrectly(device);
            SetHighlight(false);
            return true;
        }

        public DeviceInstance RemoveDevice()
        {
            var device = OccupiedDevice;
            if (device != null)
            {
                device.ClearPort();
            }

            OccupiedDevice = null;
            return device;
        }

        public void Tick(float deltaTime)
        {
            if (OccupiedDevice != null && OccupiedDevice.State == DeviceState.Charging)
            {
                OccupiedDevice.TickCharge(deltaTime);
            }
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

        public void SetHighlight(bool enabled)
        {
            if (highlightRenderer != null)
            {
                highlightRenderer.enabled = enabled;
            }
        }
    }
}
