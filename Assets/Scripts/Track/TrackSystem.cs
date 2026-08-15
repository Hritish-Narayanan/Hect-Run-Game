using System.Collections.Generic;
using UnityEngine;

namespace SubwaySurfers.Track
{
    /// <summary>
    /// Endless track manager. Keeps SpawnDistance of world ahead of the player,
    /// recycles chunks behind. Owns the chunk pool.
    /// </summary>
    public sealed class TrackSystem : MonoBehaviour
    {
        private readonly Queue<TrackChunk> active = new Queue<TrackChunk>();
        private readonly Queue<TrackChunk> inactive = new Queue<TrackChunk>();
        private float nextSpawnZ;
        private int chunkIndex;
        private Transform poolRoot;

        private void Awake()
        {
            Core.Game.Register(this);
            poolRoot = new GameObject("ChunkPool").transform;
            poolRoot.SetParent(transform, false);
            TrackChunk.EnsurePools(poolRoot);
        }

        private void Start() => ResetTrack();

        public void ResetTrack()
        {
            foreach (var c in active) Recycle(c);
            active.Clear();
            nextSpawnZ = -Core.GameConfig.SegmentLength * 2f;
            chunkIndex = 0;
            for (int i = 0; i < 9; i++) SpawnNext();
        }

        private void Update()
        {
            if (Core.Game.I == null || !Core.Game.I.Playing) return;
            var pt = Core.Game.I.PlayerTransform;
            if (pt == null) return;

            while (nextSpawnZ < pt.position.z + Core.GameConfig.SpawnDistance)
                SpawnNext();

            while (active.Count > 0)
            {
                var head = active.Peek();
                if (head == null || head.transform.position.z + Core.GameConfig.SegmentLength
                    < pt.position.z - Core.GameConfig.DespawnDistance)
                {
                    Recycle(active.Dequeue());
                }
                else break;
            }
        }

        private void SpawnNext()
        {
            var chunk = inactive.Count > 0 ? inactive.Dequeue() : CreateChunk();
            chunk.gameObject.SetActive(true);
            chunk.Populate(nextSpawnZ, chunkIndex);
            active.Enqueue(chunk);
            nextSpawnZ += Core.GameConfig.SegmentLength;
            chunkIndex++;
        }

        private TrackChunk CreateChunk()
        {
            var go = new GameObject("Chunk");
            go.transform.SetParent(poolRoot, false);
            return go.AddComponent<TrackChunk>();
        }

        private void Recycle(TrackChunk chunk)
        {
            if (chunk == null) return;
            chunk.Recycle();
            chunk.gameObject.SetActive(false);
            inactive.Enqueue(chunk);
        }
    }
}
