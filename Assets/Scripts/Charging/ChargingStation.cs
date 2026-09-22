using System.Collections.Generic;
using ChargeRush.Data;
using UnityEngine;

namespace ChargeRush.Charging
{
    /// <summary>Station that owns multiple charging ports.</summary>
    public sealed class ChargingStation : MonoBehaviour
    {
        // Tuned against counter_final (isometric wood top). Row sits on the
        // mid of the slab and drops toward +X to match the top plane.
        private const float PortRowLocalY = 0.2f;
        private const float PortRowSlope = -0.14f;
        private const float PortRowMaxWidth = 3.6f;
        private const float PortMaxSpacing = 1.15f;

        [SerializeField] private List<ChargingPort> ports = new List<ChargingPort>();

        public IReadOnlyList<ChargingPort> Ports => ports;

        public void SetPorts(List<ChargingPort> configuredPorts)
        {
            ports = configuredPorts ?? new List<ChargingPort>();
        }

        public void EnsurePortCount(int count, ChargingPort prefab, Transform parent, IList<ConnectorType> connectors, Sprite portSprite)
        {
            while (ports.Count < count)
            {
                var port = Instantiate(prefab, parent);
                ports.Add(port);
            }

            var scale = ScaleForCount(count);
            var spacing = count <= 1 ? 0f : Mathf.Min(PortRowMaxWidth / (count - 1), PortMaxSpacing);
            var startX = count <= 1 ? 0f : -spacing * (count - 1) * 0.5f;

            for (var i = 0; i < ports.Count; i++)
            {
                var active = i < count;
                ports[i].gameObject.SetActive(active);
                if (!active)
                {
                    continue;
                }

                var connector = connectors != null && connectors.Count > 0
                    ? connectors[i % connectors.Count]
                    : ConnectorType.PowerLinkA;
                ports[i].Configure(connector, portSprite);
                ports[i].transform.localScale = Vector3.one * scale;
                var x = startX + i * spacing;
                var y = PortRowLocalY + x * PortRowSlope;
                ports[i].transform.localPosition = new Vector3(x, y, 0f);
                // Lower on screen (front / further +X) draws above neighbors behind.
                ports[i].SetDrawOrder(4 + i);
            }
        }

        private static float ScaleForCount(int count)
        {
            if (count <= 2)
            {
                return 0.7f;
            }

            if (count == 3)
            {
                return 0.62f;
            }

            if (count == 4)
            {
                return 0.55f;
            }

            // 5–6 ports: shrink so the row stays on the countertop.
            return 0.48f;
        }

        public void Tick(float deltaTime)
        {
            for (var i = 0; i < ports.Count; i++)
            {
                if (ports[i] != null && ports[i].gameObject.activeInHierarchy)
                {
                    ports[i].Tick(deltaTime);
                }
            }
        }

        public ChargingPort FindPortAtScreen(Vector2 screenPosition, Camera camera)
        {
            for (var i = 0; i < ports.Count; i++)
            {
                if (ports[i] != null && ports[i].gameObject.activeInHierarchy && ports[i].ContainsScreenPoint(screenPosition, camera))
                {
                    return ports[i];
                }
            }

            return null;
        }

        public int ActivePortCount
        {
            get
            {
                var count = 0;
                for (var i = 0; i < ports.Count; i++)
                {
                    if (ports[i] != null && ports[i].gameObject.activeInHierarchy)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public int OccupiedCount
        {
            get
            {
                var count = 0;
                for (var i = 0; i < ports.Count; i++)
                {
                    if (ports[i] != null && ports[i].IsOccupied)
                    {
                        count++;
                    }
                }

                return count;
            }
        }
    }
}
