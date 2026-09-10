using UnityEngine;

namespace ChargeRush.Data
{
    /// <summary>Data definition for a customer archetype.</summary>
    [CreateAssetMenu(fileName = "CustomerData", menuName = "ChargeRush/Customer Data")]
    public sealed class CustomerData : ScriptableObject
    {
        [SerializeField] private string customerId = "customer_regular";
        [SerializeField] private string displayName = "Regular Customer";
        [SerializeField] private CustomerType customerType = CustomerType.Regular;
        [SerializeField] private float basePatienceSeconds = 45f;
        [SerializeField] private float paymentMultiplier = 1f;
        [SerializeField] private int maxDevices = 1;
        [SerializeField] private Color tintColor = Color.white;
        [SerializeField] private Sprite portraitSprite;

        public string CustomerId => customerId;
        public string DisplayName => displayName;
        public CustomerType CustomerType => customerType;
        public float BasePatienceSeconds => basePatienceSeconds;
        public float PaymentMultiplier => paymentMultiplier;
        public int MaxDevices => maxDevices;
        public Color TintColor => tintColor;
        public Sprite PortraitSprite => portraitSprite;

        public void Configure(
            string id,
            string name,
            CustomerType type,
            float patience,
            float multiplier,
            int devices,
            Color tint)
        {
            customerId = id;
            displayName = name;
            customerType = type;
            basePatienceSeconds = patience;
            paymentMultiplier = multiplier;
            maxDevices = devices;
            tintColor = tint;
        }

        public void AssignSprite(Sprite sprite)
        {
            portraitSprite = sprite;
        }
    }
}
