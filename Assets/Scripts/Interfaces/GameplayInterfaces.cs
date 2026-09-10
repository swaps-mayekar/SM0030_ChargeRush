namespace ChargeRush.Interfaces
{
    /// <summary>Marker for scene objects that can receive pointer interaction.</summary>
    public interface IInteractable
    {
        bool CanInteract { get; }
        void OnInteract();
    }

    /// <summary>Contract for objects that support drag-and-drop.</summary>
    public interface IDraggable
    {
        bool CanDrag { get; }
        void OnDragBegin();
        void OnDrag(UnityEngine.Vector2 screenPosition);
        void OnDragEnd(UnityEngine.Vector2 screenPosition);
    }

    /// <summary>Contract for devices that can receive charge.</summary>
    public interface IChargeable
    {
        bool IsFullyCharged { get; }
        float ChargeNormalized { get; }
        void BeginCharge(float durationSeconds);
        void TickCharge(float deltaTime);
        void CompleteCharge();
    }

    /// <summary>Contract for customer service interactions.</summary>
    public interface ICustomerService
    {
        string OwnerId { get; }
        bool AcceptDevice(ChargeRush.Devices.DeviceInstance device);
        void NotifyPayment(int amount);
    }
}
