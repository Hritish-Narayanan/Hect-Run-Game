using UnityEngine;

namespace SubwaySurfers.Track
{
    public enum ObstacleKind { Train, Barrier, Overhead }

    /// <summary>
    /// One obstacle instance. Trains are multi-part (body/roof/cabin), barriers
    /// are jumpable, overheads must be rolled under.
    /// </summary>
    public sealed class Obstacle : MonoBehaviour
    {
        public ObstacleKind Kind { get; private set; }

        public static Obstacle Create(Transform parent, ObstacleKind kind, Vector3 localPos)
        {
            GameObject root;
            Obstacle o;

            switch (kind)
            {
                case ObstacleKind.Train:
                    root = BuildTrain();
                    break;
                case ObstacleKind.Barrier:
                    root = BuildBarrier();
                    break;
                default:
                    root = BuildOverhead();
                    break;
            }

            root.name = kind.ToString();
            root.layer = LayerMask.NameToLayer("Obstacle") >= 0 ? LayerMask.NameToLayer("Obstacle") : 0;
            root.tag = "Obstacle";
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localPos;
            o = root.AddComponent<Obstacle>();
            o.Kind = kind;

            var box = root.GetComponent<BoxCollider>() ?? root.AddComponent<BoxCollider>();
            box.isTrigger = true;
            return o;
        }

        private static GameObject BuildTrain()
        {
            var root = new GameObject("Train");
            float length = Random.Range(5f, 8.5f);

            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0f, 1.1f, 0f);
            body.transform.localScale = new Vector3(1.9f, 2.2f, length);
            body.GetComponent<Renderer>().sharedMaterial = Core.GameConfig.GetMaterial("train", Core.GameConfig.TrainBodyColor);
            Object.Destroy(body.GetComponent<Collider>());

            var roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roof.transform.SetParent(root.transform, false);
            roof.transform.localPosition = new Vector3(0f, 2.3f, 0f);
            roof.transform.localScale = new Vector3(1.7f, 0.25f, length * 0.96f);
            roof.GetComponent<Renderer>().sharedMaterial = Core.GameConfig.GetMaterial("trainRoof", Core.GameConfig.TrainRoofColor);
            Object.Destroy(roof.GetComponent<Collider>());

            var cabin = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cabin.transform.SetParent(root.transform, false);
            cabin.transform.localPosition = new Vector3(0f, 1.5f, length * 0.5f - 0.4f);
            cabin.transform.localScale = new Vector3(1.8f, 1.6f, 0.8f);
            cabin.GetComponent<Renderer>().sharedMaterial = Core.GameConfig.GetMaterial("trainRoof", Core.GameConfig.TrainRoofColor);
            Object.Destroy(cabin.GetComponent<Collider>());

            var box = root.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 1.1f, 0f);
            box.size = new Vector3(1.9f, 2.2f, length);
            return root;
        }

        private static GameObject BuildBarrier()
        {
            var root = new GameObject("Barrier");

            var bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bar.transform.SetParent(root.transform, false);
            bar.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            bar.transform.localScale = new Vector3(2.1f, 1.1f, 0.3f);
            bar.GetComponent<Renderer>().sharedMaterial = Core.GameConfig.GetMaterial("barrier", Core.GameConfig.BarrierColor);
            Object.Destroy(bar.GetComponent<Collider>());

            var stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stripe.transform.SetParent(root.transform, false);
            stripe.transform.localPosition = new Vector3(0f, 0.55f, 0.16f);
            stripe.transform.localScale = new Vector3(2.12f, 0.25f, 0.02f);
            stripe.GetComponent<Renderer>().sharedMaterial = Core.GameConfig.GetMaterial("barrierStripe", Color.white);
            Object.Destroy(stripe.GetComponent<Collider>());

            var box = root.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 0.55f, 0f);
            box.size = new Vector3(2.1f, 1.1f, 0.3f);
            return root;
        }

        private static GameObject BuildOverhead()
        {
            var root = new GameObject("Overhead");

            var beam = GameObject.CreatePrimitive(PrimitiveType.Cube);
            beam.transform.SetParent(root.transform, false);
            beam.transform.localPosition = new Vector3(0f, 1.7f, 0f);
            beam.transform.localScale = new Vector3(2.1f, 1.6f, 0.4f);
            beam.GetComponent<Renderer>().sharedMaterial = Core.GameConfig.GetMaterial("overhead", Core.GameConfig.OverheadColor);
            Object.Destroy(beam.GetComponent<Collider>());

            var box = root.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 1.7f, 0f);
            box.size = new Vector3(2.1f, 1.6f, 0.4f);
            return root;
        }
    }
}
