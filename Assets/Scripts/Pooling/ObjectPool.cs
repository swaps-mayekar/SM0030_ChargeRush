using System.Collections.Generic;
using UnityEngine;

namespace ChargeRush.Pooling
{
    /// <summary>Simple generic object pool for MonoBehaviour actors.</summary>
    public sealed class ObjectPool<T> where T : Component
    {
        private readonly T prefab;
        private readonly Transform parent;
        private readonly Stack<T> available = new Stack<T>();
        private readonly HashSet<T> active = new HashSet<T>();

        public ObjectPool(T prefabInstance, Transform poolParent, int prewarmCount)
        {
            if (prefabInstance == null)
            {
                throw new System.ArgumentNullException(
                    nameof(prefabInstance),
                    $"ObjectPool<{typeof(T).Name}> requires a non-null prefab.");
            }

            prefab = prefabInstance;
            parent = poolParent;
            for (var i = 0; i < prewarmCount; i++)
            {
                available.Push(CreateInstance());
            }
        }

        public int ActiveCount => active.Count;

        public T Get()
        {
            var instance = available.Count > 0 ? available.Pop() : CreateInstance();
            instance.gameObject.SetActive(true);
            active.Add(instance);
            return instance;
        }

        public void Release(T instance)
        {
            if (instance == null || !active.Remove(instance))
            {
                return;
            }

            instance.gameObject.SetActive(false);
            instance.transform.SetParent(parent, false);
            available.Push(instance);
        }

        public void ReleaseAll()
        {
            if (active.Count == 0)
            {
                return;
            }

            var snapshot = new List<T>(active);
            for (var i = 0; i < snapshot.Count; i++)
            {
                Release(snapshot[i]);
            }
        }

        private T CreateInstance()
        {
            var instance = Object.Instantiate(prefab, parent);
            instance.gameObject.SetActive(false);
            return instance;
        }
    }
}
