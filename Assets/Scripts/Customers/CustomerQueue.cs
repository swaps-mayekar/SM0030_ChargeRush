using System.Collections.Generic;
using ChargeRush.Customers;
using UnityEngine;

namespace ChargeRush.Customers
{
    /// <summary>Reusable visible-slot and overflow queue manager.</summary>
    public sealed class CustomerQueue : MonoBehaviour
    {
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
                        ? overflowRoot.position + Vector3.left * overflowIndex * 0.55f
                        : transform.position + Vector3.left * (2.5f + overflowIndex * 0.55f);
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
                go.transform.localPosition = new Vector3(-7.5f, -0.1f, 0f);
                overflowRoot = go.transform;
            }

            while (visibleSlots.Count < count)
            {
                var slot = new GameObject($"Slot_{visibleSlots.Count}");
                slot.transform.SetParent(slotRoot, false);
                slot.transform.localPosition = new Vector3(-1.4f + visibleSlots.Count * 2.8f, -0.1f, 0f);
                visibleSlots.Add(slot.transform);
            }
        }
    }
}
