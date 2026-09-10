using UnityEngine;

namespace ChargeRush.Data
{
    /// <summary>Data-driven achievement definition.</summary>
    [CreateAssetMenu(fileName = "AchievementData", menuName = "ChargeRush/Achievement Data")]
    public sealed class AchievementData : ScriptableObject
    {
        [SerializeField] private string achievementId = "first_charge";
        [SerializeField] private string displayName = "First Charge";
        [TextArea] [SerializeField] private string description = "Complete your first customer transaction.";
        [SerializeField] private int targetValue = 1;
        [SerializeField] private string progressKey = "customers_served";

        public string AchievementId => achievementId;
        public string DisplayName => displayName;
        public string Description => description;
        public int TargetValue => targetValue;
        public string ProgressKey => progressKey;

        public void Configure(string id, string name, string desc, int target, string key)
        {
            achievementId = id;
            displayName = name;
            description = desc;
            targetValue = target;
            progressKey = key;
        }
    }
}
