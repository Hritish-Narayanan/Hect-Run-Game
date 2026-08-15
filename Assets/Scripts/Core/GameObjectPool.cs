using System.Collections.Generic;
using UnityEngine;

namespace SubwaySurfers.Core
{
    /// <summary>
    /// Root-level GameObject pool. Unlike the old ObjectPooler this one actually
    /// survives restarts, auto-expands, and parents pooled objects so the scene
    /// hierarchy stays clean.
    /// </summary>
    public sealed class GameObjectPool
    {
        private readonly GameObject prefab;
        private readonly Transform parent;
        private readonly Queue<GameObject> available = new Queue<GameObject>();
        private readonly HashSet<GameObject> inUse = new HashSet<GameObject>();

        public GameObjectPool(GameObject prefab, int prewarm, Transform parent)
        {
            this.prefab = prefab;
            this.parent = parent;
            for (int i = 0; i < prewarm; i++)
                Release(CreateNew());
        }

        private GameObject CreateNew()
        {
            var go = Object.Instantiate(prefab, parent);
            go.SetActive(false);
            return go;
        }

        public GameObject Get()
        {
            GameObject go = available.Count > 0 ? available.Dequeue() : CreateNew();
            inUse.Add(go);
            go.SetActive(true);
            return go;
        }

        public void Release(GameObject go)
        {
            if (go == null) return;
            inUse.Remove(go);
            go.SetActive(false);
            go.transform.SetParent(parent, false);
            if (!available.Contains(go))
                available.Enqueue(go);
        }

        public void ReleaseAll()
        {
            foreach (var go in inUse)
            {
                if (go == null) continue;
                go.SetActive(false);
                go.transform.SetParent(parent, false);
                available.Enqueue(go);
            }
            inUse.Clear();
        }
    }
}
