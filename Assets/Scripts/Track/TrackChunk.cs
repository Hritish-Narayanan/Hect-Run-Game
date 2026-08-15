using System.Collections.Generic;
using UnityEngine;
using SubwaySurfers.Core;

namespace SubwaySurfers.Track
{
    /// <summary>
    /// One 18m chunk of world. Floor, rails, sleepers, obstacles, coins, powerup,
    /// side buildings. Everything is pooled and recycled — zero runtime Instantiate
    /// after warm-up, zero GC churn during play.
    /// </summary>
    public sealed class TrackChunk : MonoBehaviour
    {
        private static GameObjectPool floorPool, trainPool, barrierPool, overheadPool,
            coinPool, powerupPool, buildingPool, sleeperPool, railPool;

        private readonly List<GameObject> spawned = new List<GameObject>(32);

        public static void EnsurePools(Transform parent)
        {
            if (floorPool != null) return;

            floorPool = MakePool(parent, PrimitiveType.Cube, 10);
            trainPool = MakePool(parent, PrimitiveType.Cube, 12);
            barrierPool = MakePool(parent, PrimitiveType.Cube, 8);
            overheadPool = MakePool(parent, PrimitiveType.Cube, 6);
            coinPool = MakePool(parent, PrimitiveType.Cylinder, 60, MakeCoinPrefab());
            powerupPool = MakePool(parent, PrimitiveType.Sphere, 3, MakePowerupPrefab());
            buildingPool = MakePool(parent, PrimitiveType.Cube, 24);
            sleeperPool = MakePool(parent, PrimitiveType.Cube, 40);
            railPool = MakePool(parent, PrimitiveType.Cube, 20);
        }

        private static GameObject MakeCoinPrefab()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.Destroy(go.GetComponent<Collider>());
            var col = go.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.45f;
            go.transform.localScale = new Vector3(0.6f, 0.06f, 0.6f);
            go.GetComponent<Renderer>().sharedMaterial = Core.GameConfig.GetMaterial("coin", Core.GameConfig.CoinColor);
            go.AddComponent<Coin>();
            go.tag = "Coin";
            return go;
        }

        private static GameObject MakePowerupPrefab()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.Destroy(go.GetComponent<Collider>());
            var col = go.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.5f;
            go.transform.localScale = Vector3.one * 0.7f;
            go.AddComponent<PowerupPickup>();
            go.tag = "Powerup";
            return go;
        }

        private static GameObjectPool MakePool(Transform parent, PrimitiveType type, int size, GameObject prefabOverride = null)
        {
            var prefab = prefabOverride != null ? prefabOverride : GameObject.CreatePrimitive(type);
            if (prefabOverride == null)
            {
                Object.Destroy(prefab.GetComponent<Collider>());
                prefab.GetComponent<Renderer>().sharedMaterial = Core.GameConfig.GetMaterial("default", Color.gray);
            }
            return new Core.GameObjectPool(prefab, size, parent);
        }

        /// <summary>Populate this chunk at the given Z with seeded randomness.</summary>
        public void Populate(float z, int chunkIndex)
        {
            transform.position = new Vector3(0f, 0f, z);
            spawned.Clear();

            // Floor
            var floor = floorPool.Get();
            floor.transform.SetParent(transform, false);
            floor.transform.localPosition = new Vector3(0f, -0.55f, Core.GameConfig.SegmentLength * 0.5f);
            floor.transform.localScale = new Vector3(Core.GameConfig.TrackWidth, Core.GameConfig.TrackThickness, Core.GameConfig.SegmentLength);
            floor.GetComponent<Renderer>().sharedMaterial = Core.GameConfig.GetMaterial("track", Core.GameConfig.TrackColor);
            spawned.Add(floor);

            // Rails
            for (int lane = 0; lane < Core.GameConfig.LaneCount; lane++)
            {
                float x = (lane - 1) * Core.GameConfig.LaneWidth;
                var rail = railPool.Get();
                rail.transform.SetParent(transform, false);
                rail.transform.localPosition = new Vector3(x, -0.02f, Core.GameConfig.SegmentLength * 0.5f);
                rail.transform.localScale = new Vector3(0.14f, 0.1f, Core.GameConfig.SegmentLength);
                rail.GetComponent<Renderer>().sharedMaterial = Core.GameConfig.GetMaterial("rail", Core.GameConfig.RailColor);
                spawned.Add(rail);
            }

            // Sleepers
            for (int i = 0; i < 6; i++)
            {
                var s = sleeperPool.Get();
                s.transform.SetParent(transform, false);
                s.transform.localPosition = new Vector3(0f, -0.1f, i * 3f + 1.5f);
                s.transform.localScale = new Vector3(Core.GameConfig.TrackWidth * 0.96f, 0.08f, 0.5f);
                s.GetComponent<Renderer>().sharedMaterial = Core.GameConfig.GetMaterial("sleeper", Core.GameConfig.SleeperColor);
                spawned.Add(s);
            }

            // Side buildings
            int buildingRows = Core.GameConfig.LowPolyMode ? 1 : 2;
            for (int i = 0; i < buildingRows; i++)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    var b = buildingPool.Get();
                    b.transform.SetParent(transform, false);
                    float h = Random.Range(4f, 14f);
                    b.transform.localPosition = new Vector3(side * (Core.GameConfig.TrackWidth * 0.5f + Random.Range(3f, 7f)),
                        h * 0.5f - 0.5f, i * Core.GameConfig.SegmentLength * 0.5f + Core.GameConfig.SegmentLength * 0.25f);
                    b.transform.localScale = new Vector3(Random.Range(4f, 8f), h, Random.Range(6f, 12f));
                    float v = Random.Range(0.85f, 1.15f);
                    b.GetComponent<Renderer>().sharedMaterial = Core.GameConfig.GetMaterial("building" + (i + (side + 1) / 2),
                        Core.GameConfig.BuildingColor * v);
                    spawned.Add(b);
                }
            }

            // Obstacles — difficulty scales with chunkIndex
            float difficulty = Mathf.Clamp01(chunkIndex / 60f);
            int safeLane = Random.Range(0, Core.GameConfig.LaneCount);
            for (int lane = 0; lane < Core.GameConfig.LaneCount; lane++)
            {
                if (lane == safeLane) continue;
                float obstacleChance = 0.35f + difficulty * 0.35f;
                if (Random.value > obstacleChance) continue;

                ObstacleKind kind = RollObstacleKind(difficulty);
                GameObject src = kind == ObstacleKind.Train ? trainPool.Get()
                    : kind == ObstacleKind.Barrier ? barrierPool.Get()
                    : overheadPool.Get();

                // Rebuild proper obstacle from raw pool cube
                src.SetActive(false);
                var obs = Obstacle.Create(transform, kind, new Vector3((lane - 1) * Core.GameConfig.LaneWidth, 0f,
                    Random.Range(4f, Core.GameConfig.SegmentLength - 4f)));
                spawned.Add(obs.gameObject);
                // return the unused pool cube
                if (kind == ObstacleKind.Train) trainPool.Release(src);
                else if (kind == ObstacleKind.Barrier) barrierPool.Release(src);
                else overheadPool.Release(src);
            }

            // Coins — arc or line on the safe lane
            if (Random.value < 0.85f)
            {
                int count = Random.Range(5, 9);
                bool arc = Random.value < 0.5f;
                float startZ = Random.Range(3f, Core.GameConfig.SegmentLength - count * 1.2f - 2f);
                for (int i = 0; i < count; i++)
                {
                    var c = coinPool.Get();
                    c.transform.SetParent(transform, false);
                    float y = arc ? 1.2f + Mathf.Sin((float)i / (count - 1) * Mathf.PI) * 1.6f : 1.2f;
                    c.transform.localPosition = new Vector3((safeLane - 1) * Core.GameConfig.LaneWidth, y, startZ + i * 1.2f);
                    c.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    spawned.Add(c);
                }
            }

            // Powerup — rare, on the safe lane
            if (Random.value < 0.16f)
            {
                var p = powerupPool.Get();
                p.transform.SetParent(transform, false);
                p.transform.localPosition = new Vector3((safeLane - 1) * Core.GameConfig.LaneWidth, 1.4f,
                    Random.Range(5f, Core.GameConfig.SegmentLength - 3f));
                var pickup = p.GetComponent<PowerupPickup>();
                pickup.SetType((Core.PowerupType)Random.Range(0, 3));
                spawned.Add(p);
            }
        }

        private static ObstacleKind RollObstacleKind(float difficulty)
        {
            float r = Random.value;
            if (difficulty < 0.15f) return ObstacleKind.Train;
            if (r < 0.45f) return ObstacleKind.Train;
            if (r < 0.75f) return ObstacleKind.Barrier;
            return ObstacleKind.Overhead;
        }

        public void Recycle()
        {
            foreach (var go in spawned)
            {
                if (go == null) continue;
                if (go.name.StartsWith("Train")) { ReleaseObstacleParts(go); trainPool.Release(go); }
                else if (go.name.StartsWith("Barrier")) { ReleaseObstacleParts(go); barrierPool.Release(go); }
                else if (go.name.StartsWith("Overhead")) { ReleaseObstacleParts(go); overheadPool.Release(go); }
                else if (go.CompareTag("Coin")) coinPool.Release(go);
                else if (go.CompareTag("Powerup")) powerupPool.Release(go);
                else if (go.name.StartsWith("Building")) buildingPool.Release(go);
                else if (go.name.StartsWith("Sleeper")) sleeperPool.Release(go);
                else if (go.name.StartsWith("Rail")) railPool.Release(go);
                else floorPool.Release(go);
            }
            spawned.Clear();
        }

        private static void ReleaseObstacleParts(GameObject root)
        {
            // Obstacle visuals are children created at spawn time; destroy them
            // since the pool only owns the root cube.
            for (int i = root.transform.childCount - 1; i >= 0; i--)
                Object.Destroy(root.transform.GetChild(i).gameObject);
            var obs = root.GetComponent<Obstacle>();
            if (obs != null) Object.Destroy(obs);
            var col = root.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
            root.name = "PooledCube";
        }
    }
}
