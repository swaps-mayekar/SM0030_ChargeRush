using System.Collections.Generic;
using ChargeRush.Customers;
using UnityEngine;

namespace ChargeRush.Customers
{
    /// <summary>Reusable visible-slot and overflow queue manager.</summary>
    public sealed class CustomerQueue : MonoBehaviour
    {
        // Stand behind the counter (higher Y), packed toward the left so the
        // row stays on-screen on narrower iPad landscape frustums.
        private const float SlotStartX = -3.1f;
        private const float SlotSpacingX = 1.85f;
        private const float SlotLocalY = -0.35f;
        private const float OverflowLocalX = -6.2f;
        private const float OverflowSpacingX = 0.5f;

        [SerializeField] private Transform slotRoot;
        [SerializeField] private Transform overflowRoot;

        private readonly List<Transform> visibleSlots = new List<Transform>();
        private readonly List<CustomerInstance> customers = new List<CustomerInstance>();
        private int visibleCapacity = 2;

        public int Count => customers.Count;
        public int VisibleCapacity => visibleCapacity;
        public IReadOnlyList<CustomerInstance> Customers => customers;

        public void Configure(int capacity, int maxSlotsToCreate = 6)
        {
            visibleCapacity = Mathf.Max(1, capacity);
            EnsureSlots(Mathf.Max(visibleCapacity, maxSlotsToCreate));
            Relayout();
        }

        public bool TryEnqueue(CustomerInstance customer)
        {
            if (customer == null)
            {
                return false;
            }

            customers.Add(customer);
            Relayout();
            return true;
        }

        public bool Remove(CustomerInstance customer)
        {
            var removed = customers.Remove(customer);
            if (removed)
            {
                Relayout();
            }

            return removed;
        }

        public CustomerInstance FindAtScreen(Vector2 screenPosition, Camera camera)
        {
            for (var i = 0; i < customers.Count; i++)
            {
                var customer = customers[i];
                if (customer != null && !customer.IsInOverflow && customer.ContainsScreenPoint(screenPosition, camera))
                {
                    return customer;
                }
            }

            return null;
        }

        public void Relayout()
        {
            for (var i = 0; i < customers.Count; i++)
            {
                var customer = customers[i];
                if (customer == null)
                {
                    continue;
                }

                if (i < visibleCapacity && i < visibleSlots.Count)
                {
                    var slot = visibleSlots[i];
                    customer.AssignSlot(i, slot.position, false);
                }
                else
                {
                    var overflowIndex = i - visibleCapacity;
                    var pos = overflowRoot != null
                        ? overflowRoot.position + Vector3.left * overflowIndex * OverflowSpacingX
                        : transform.position + Vector3.left * (2.5f + overflowIndex * OverflowSpacingX);
                    customer.AssignSlot(i, pos, true);
                }
            }
        }

        private void EnsureSlots(int count)
        {
            if (slotRoot == null)
            {
                var go = new GameObject("Slots");
                go.transform.SetParent(transform, false);
                slotRoot = go.transform;
            }

            if (overflowRoot == null)
            {
                var go = new GameObject("Overflow");
                go.transform.SetParent(transform, false);
                overflowRoot = go.transform;
            }

            overflowRoot.localPosition = new Vector3(OverflowLocalX, SlotLocalY, 0f);

            while (visibleSlots.Count < count)
            {
                var slot = new GameObject($"Slot_{visibleSlots.Count}");
                slot.transform.SetParent(slotRoot, false);
                visibleSlots.Add(slot.transform);
            }

            for (var i = 0; i < visibleSlots.Count; i++)
            {
                visibleSlots[i].localPosition = new Vector3(SlotStartX + i * SlotSpacingX, SlotLocalY, 0f);
            }
        }
    }
}
