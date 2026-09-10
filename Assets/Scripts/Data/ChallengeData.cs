using UnityEngine;

namespace ChargeRush.Data
{
    /// <summary>Challenge mode objective definition.</summary>
    [CreateAssetMenu(fileName = "ChallengeData", menuName = "ChargeRush/Challenge Data")]
    public sealed class ChallengeData : ScriptableObject
    {
        [SerializeField] private string challengeId = "challenge_earn";
        [SerializeField] private string displayName = "Credit Rush";
        [SerializeField] private string description = "Earn the target credits.";
        [SerializeField] private ChallengeObjectiveType objectiveType = ChallengeObjectiveType.EarnCredits;
        [SerializeField] private LevelData linkedLevel;
        [SerializeField] private int targetValue = 400;
        [SerializeField] private float timeLimitSeconds;

        public string ChallengeId => challengeId;
        public string DisplayName => displayName;
        public string Description => description;
        public ChallengeObjectiveType ObjectiveType => objectiveType;
        public LevelData LinkedLevel => linkedLevel;
        public int TargetValue => targetValue;
        public float TimeLimitSeconds => timeLimitSeconds;

        public void Configure(
            string id,
            string name,
            string desc,
            ChallengeObjectiveType type,
            LevelData level,
            int target,
            float timeLimit)
        {
            challengeId = id;
            displayName = name;
            description = desc;
            objectiveType = type;
            linkedLevel = level;
            targetValue = target;
            timeLimitSeconds = timeLimit;
        }
    }
}
