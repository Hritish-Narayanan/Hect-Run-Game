using UnityEngine;

namespace SubwaySurfers.Presentation
{
    /// <summary>
    /// Chase cam with speed-reactive FOV, lane-change anticipation, landing
    /// dips, and crash shake. Attaches to the main camera.
    /// </summary>
    public sealed class CameraController : MonoBehaviour
    {
        private Camera cam;
        private Vector3 offset = new Vector3(0f, 6.5f, -11f);
        private float baseFov = 62f;
        private float fovKick;
        private float shake;
        private Vector3 velocity;

        private void Awake()
        {
            Core.Game.Register(this);
            cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }
            cam.fieldOfView = baseFov;
            transform.position = new Vector3(0f, 6.5f, -11f);
        }

        public void SnapToTarget()
        {
            var t = Core.Game.I != null ? Core.Game.I.PlayerTransform : null;
            if (t == null) return;
            transform.position = t.position + offset;
            velocity = Vector3.zero;
        }

        private void LateUpdate()
        {
            var t = Core.Game.I != null ? Core.Game.I.PlayerTransform : null;
            if (t == null || cam == null) return;

            float dt = Time.deltaTime;

            // FOV kick with speed
            float speedNorm = Mathf.InverseLerp(Core.GameConfig.StartSpeed, Core.GameConfig.MaxSpeed, Core.Game.I.Speed);
            float targetFov = baseFov + speedNorm * 12f + fovKick;
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFov, dt * 5f);
            fovKick = Mathf.Lerp(fovKick, 0f, dt * 4f);

            // Follow with lane anticipation
            Vector3 desired = t.position + offset;
            desired.x = t.position.x * 0.35f;
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, 0.12f);
            transform.rotation = Quaternion.Euler(20f, 0f, 0f);

            // Crash shake
            if (shake > 0f)
            {
                shake = Mathf.Max(0f, shake - dt * 2f);
                transform.position += Random.insideUnitSphere * shake * 0.35f;
            }
        }

        public void JumpKick() => fovKick = 4f;
        public void CrashShake() => shake = 1.2f;
    }
}
