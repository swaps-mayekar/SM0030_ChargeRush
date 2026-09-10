using UnityEngine;

namespace ChargeRush.Data
{
    /// <summary>Data definition for a chargeable device type.</summary>
    [CreateAssetMenu(fileName = "DeviceData", menuName = "ChargeRush/Device Data")]
    public sealed class DeviceData : ScriptableObject
    {
        [SerializeField] private string deviceId = "device_basic_phone";
        [SerializeField] private string displayName = "Basic Smartphone";
        [SerializeField] private DeviceCategory category = DeviceCategory.BasicSmartphone;
        [SerializeField] private ConnectorType requiredConnector = ConnectorType.PowerLinkA;
        [SerializeField] private int batteryCapacity = 100;
        [SerializeField] private float chargingDurationSeconds = 6f;
        [SerializeField] private int servicePrice = 25;
        [SerializeField] private Color tintColor = Color.white;
        [SerializeField] private Sprite idleSprite;
        [SerializeField] private Sprite chargingSprite;
        [SerializeField] private Sprite fullyChargedSprite;

        public string DeviceId => deviceId;
        public string DisplayName => displayName;
        public DeviceCategory Category => category;
        public ConnectorType RequiredConnector => requiredConnector;
        public int BatteryCapacity => batteryCapacity;
        public float ChargingDurationSeconds => chargingDurationSeconds;
        public int ServicePrice => servicePrice;
        public Color TintColor => tintColor;
        public Sprite IdleSprite => idleSprite;
        public Sprite ChargingSprite => chargingSprite;
        public Sprite FullyChargedSprite => fullyChargedSprite;

        public void Configure(
            string id,
            string name,
            DeviceCategory deviceCategory,
            ConnectorType connector,
            int capacity,
            float duration,
            int price,
            Color tint)
        {
            deviceId = id;
            displayName = name;
            category = deviceCategory;
            requiredConnector = connector;
            batteryCapacity = capacity;
            chargingDurationSeconds = duration;
            servicePrice = price;
            tintColor = tint;
        }

        public void AssignSprites(Sprite idle, Sprite charging, Sprite charged)
        {
            idleSprite = idle;
            chargingSprite = charging;
            fullyChargedSprite = charged;
        }
    }
}
