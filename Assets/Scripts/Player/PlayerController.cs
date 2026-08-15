using UnityEngine;
using SubwaySurfers.Core;

namespace SubwaySurfers.Player
{
    /// <summary>
    /// The surfer. Kinematic Rigidbody + capsule, layer-locked to Player so it
    /// only collides with obstacles. Lanes via critically-damped lerp with lean,
    /// snappy jump with fall multiplier, roll that shrinks the capsule.
    /// </summary>
    [RequireComponent(typeof(CapsuleCollider))]
    public sealed class PlayerController : MonoBehaviour
    {
        private CapsuleCollider col;
        private Rigidbody rb;

        private int lane = 1;
        private float x;
        private float yVel;
        private bool grounded = true;
        private bool rolling;
        private float rollTimer;
        private float leanZ;

        private Coroutine iFrames;

        private void Awake()
        {
            Core.Game.Register(this);
            gameObject.layer = LayerMask.NameToLayer("Player") >= 0 ? LayerMask.NameToLayer("Player") : 0;

            col = GetComponent<CapsuleCollider>();
            rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            x = transform.position.x;

            var input = EnsureInput();
            input.OnLane += MoveLane;
            input.OnJump += Jump;
            input.OnRoll += Roll;
        }

        private static Core.InputReader EnsureInput()
        {
            var reader = Core.Game.Get<Core.InputReader>();
            if (reader == null)
            {
                var go = new GameObject("Input");
                reader = go.AddComponent<Core.InputReader>();
                if (Core.Game.I != null) go.transform.SetParent(Core.Game.I.transform, false);
            }
            return reader;
        }

        private void Update()
        {
            if (Core.Game.I == null || !Core.Game.I.Playing) return;
            float dt = Time.deltaTime;

            // Forward
            Vector3 pos = transform.position;
            pos.z += Core.Game.I.Speed * dt;

            // Lane (smooth, frame-rate independent)
            float targetX = (lane - 1) * Core.GameConfig.LaneWidth;
            x = Mathf.Lerp(x, targetX, 1f - Mathf.Exp(-Core.GameConfig.LaneChangeSharpness * dt));
            float lateral = x - pos.x;
            pos.x = x;

            // Gravity / jump
            if (!grounded)
            {
                float mult = yVel < 0 ? Core.GameConfig.FallMultiplier : 1f;
                yVel += Physics.gravity.y * mult * dt;
                pos.y += yVel * dt;
                if (pos.y <= 1f)
                {
                    pos.y = 1f;
                    grounded = true;
                    yVel = 0f;
                    Core.Game.Get<Audio.AudioSystem>()?.PlayLand();
                }
            }

            // Roll timer
            if (rolling)
            {
                rollTimer -= dt;
                if (rollTimer <= 0f) EndRoll();
            }

            // Lean into lane changes
            float targetLean = Mathf.Clamp(-lateral * 20f, -Core.GameConfig.LaneLeanDegrees, Core.GameConfig.LaneLeanDegrees);
            leanZ = Mathf.Lerp(leanZ, targetLean, dt * 10f);
            transform.rotation = Quaternion.Euler(0f, 0f, leanZ);

            transform.position = pos;
        }

        public void MoveLane(int dir)
        {
            lane = Mathf.Clamp(lane + dir, 0, Core.GameConfig.LaneCount - 1);
            Core.Game.Get<Audio.AudioSystem>()?.PlayWhoosh();
        }

        public void Jump()
        {
            if (!grounded) return;
            if (rolling) EndRoll();
            grounded = false;
            yVel = Core.GameConfig.JumpForce;
            Core.Game.Get<Audio.AudioSystem>()?.PlayJump();

            // Increase speed by 1% on jump
            Core.Game.I?.SpeedUpFromJump();
        }

        public void Roll()
        {
            if (rolling) return;
            if (!grounded) yVel = Mathf.Min(yVel, -12f); // slam down
            rolling = true;
            rollTimer = Core.GameConfig.RollDuration;
            col.height = Core.GameConfig.RollHeight;
            col.center = new Vector3(0f, Core.GameConfig.RollHeight * 0.5f - 0.5f, 0f);
            transform.localScale = new Vector3(1f, 0.55f, 1f);
            Core.Game.Get<Audio.AudioSystem>()?.PlayRoll();
        }

        private void EndRoll()
        {
            rolling = false;
            col.height = 2f;
            col.center = new Vector3(0f, 0.9f, 0f);
            transform.localScale = Vector3.one;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Obstacle"))
            {
                var power = Core.Game.Get<Core.PowerupSystem>();
                if (power != null && power.TryAbsorbHit())
                {
                    Core.Game.Get<Effects.EffectsSystem>()?.ShieldPop(transform.position);
                    Core.Game.Get<Audio.AudioSystem>()?.PlayShieldBreak();
                    return;
                }
                Die();
            }
        }

        private void Die()
        {
            if (iFrames != null) return; // already dying
            iFrames = StartCoroutine(DeathSequence());
        }

        private System.Collections.IEnumerator DeathSequence()
        {
            Core.Game.Get<Effects.EffectsSystem>()?.CrashBurst(transform.position);
            Core.Game.I.Crash();
            // brief tumble
            float t = 0;
            Vector3 start = transform.position;
            while (t < 0.5f)
            {
                t += Time.deltaTime;
                transform.position = start + Vector3.up * (t * 2f) - Vector3.forward * (t * 3f);
                transform.Rotate(Vector3.right, 540f * Time.deltaTime);
                yield return null;
            }
            gameObject.SetActive(false);
        }

        public Vector3 TopCenter => transform.position + Vector3.up * (col.height * 0.5f);
    }
}
