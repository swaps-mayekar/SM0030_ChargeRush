using System.Collections.Generic;
using ChargeRush.Data;
using UnityEngine;

namespace ChargeRush.Charging
{
    /// <summary>Station that owns multiple charging ports.</summary>
    public sealed class ChargingStation : MonoBehaviour
    {
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
                ports[i].transform.localPosition = new Vector3(-2.2f + i * 1.4f, -1.6f, 0f);
            }
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
