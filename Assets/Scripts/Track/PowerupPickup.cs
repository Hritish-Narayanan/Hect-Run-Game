using UnityEngine;

namespace SubwaySurfers.Track
{
    /// <summary>
    /// Bobbing powerup pickup. Trigger on Player layer; grants via PowerupSystem.
    /// </summary>
    [RequireComponent(typeof(SphereCollider))]
    public sealed class PowerupPickup : MonoBehaviour
    {
        private Core.PowerupType type;
        private bool collected;
        private float baseY;
        private float t;

        public void SetType(Core.PowerupType type)
        {
            this.type = type;
            GetComponent<Renderer>().sharedMaterial =
                Core.GameConfig.GetMaterial("power" + (int)type, Core.GameConfig.PowerupColors[(int)type]);
        }

        private void OnEnable()
        {
            baseY = transform.localPosition.y;
            t = Random.value * 6.28f;
            collected = false;
        }

        private void Update()
        {
            t += Time.deltaTime * 3f;
            var p = transform.localPosition;
            p.y = baseY + Mathf.Sin(t) * 0.25f;
            transform.localPosition = p;
            transform.Rotate(Vector3.up, 120f * Time.deltaTime, Space.World);

            var player = Core.Game.I != null ? Core.Game.I.PlayerTransform : null;
            if (player == null || Core.Game.I == null || !Core.Game.I.Playing) return;

            float d = Vector3.Distance(transform.position, player.position);
            if (d <= 1.4f)
            {
                Collect();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (collected || !other.CompareTag("Player")) return;
            Collect();
        }

        private void Collect()
        {
            if (collected) return;
            collected = true;
            Core.Game.Get<Core.PowerupSystem>()?.Grant(type);
            Core.Game.Get<Effects.EffectsSystem>()?.PowerupBurst(transform.position, Core.GameConfig.PowerupColors[(int)type]);
            gameObject.SetActive(false);
        }
    }
}
