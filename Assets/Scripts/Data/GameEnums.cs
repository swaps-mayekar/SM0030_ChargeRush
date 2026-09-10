namespace ChargeRush.Data
{
    /// <summary>Fictional connector families used by ChargeRush devices.</summary>
    public enum ConnectorType
    {
        PowerLinkA = 0,
        PowerLinkB = 1,
        MiniPower = 2,
        ProPower = 3,
        UniversalPower = 4
    }

    /// <summary>High-level device category for progression and achievements.</summary>
    public enum DeviceCategory
    {
        BasicSmartphone = 0,
        AdvancedSmartphone = 1,
        Tablet = 2,
        PowerBank = 3,
        DigitalCamera = 4,
        HandheldGameDevice = 5,
        NotebookComputer = 6
    }

    /// <summary>Customer archetype used for patience and payment modifiers.</summary>
    public enum CustomerType
    {
        Regular = 0,
        Impatient = 1,
        Generous = 2,
        Patient = 3,
        Group = 4,
        Vip = 5
    }

    public enum CustomerState
    {
        Inactive = 0,
        Approaching = 1,
        Waiting = 2,
        HandingOver = 3,
        DeviceAtCounter = 4,
        WaitingForDevice = 5,
        ReceivingDevice = 6,
        Paying = 7,
        Leaving = 8,
        AngryLeaving = 9
    }

    public enum DeviceState
    {
        Inactive = 0,
        WithCustomer = 1,
        AtCounter = 2,
        Charging = 3,
        FullyCharged = 4,
        ReturningToCustomer = 5,
        Completed = 6
    }

    public enum GameMode
    {
        Story = 0,
        Challenge = 1,
        Endless = 2
    }

    public enum SessionState
    {
        Boot = 0,
        Menu = 1,
        Loading = 2,
        Playing = 3,
        Paused = 4,
        Completing = 5,
        Completed = 6,
        Failed = 7
    }

    public enum CareerRank
    {
        Unemployed = 0,
        LocalChargingHelper = 1,
        SmallChargingService = 2,
        EventChargingService = 3,
        ProfessionalChargingBooth = 4,
        PremiumChargingService = 5,
        MajorEventService = 6
    }

    public enum UpgradeType
    {
        ChargingCapacity = 0,
        ChargingSpeed = 1,
        CounterSize = 2,
        QueueCapacity = 3,
        ServiceQuality = 4,
        ProfessionalEquipment = 5
    }

    public enum ChallengeObjectiveType
    {
        EarnCredits = 0,
        ServeCustomers = 1,
        ZeroMistakes = 2,
        FinishWithinTime = 3
    }

    public enum MistakeReason
    {
        WrongCustomer = 0,
        IncorrectConnector = 1,
        CustomerLeft = 2
    }

    public enum PatienceMood
    {
        Happy = 0,
        Waiting = 1,
        BecomingImpatient = 2,
        VeryImpatient = 3,
        Leaves = 4
    }

    public enum AudioCue
    {
        CustomerArrival = 0,
        DevicePickup = 1,
        DevicePlacedCorrectly = 2,
        IncorrectConnector = 3,
        ChargingStarted = 4,
        ChargingComplete = 5,
        DeviceReturned = 6,
        Payment = 7,
        AchievementUnlocked = 8,
        LevelComplete = 9,
        LevelFailed = 10,
        ButtonClick = 11
    }
}
