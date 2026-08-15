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
        }

        private void Update()
        {
            t += Time.deltaTime * 3f;
            var p = transform.localPosition;
            p.y = baseY + Mathf.Sin(t) * 0.25f;
            transform.localPosition = p;
            transform.Rotate(Vector3.up, 120f * Time.deltaTime, Space.World);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            Core.Game.Get<Core.PowerupSystem>()?.Grant(type);
            Core.Game.Get<Effects.EffectsSystem>()?.PowerupBurst(transform.position, Core.GameConfig.PowerupColors[(int)type]);
            gameObject.SetActive(false);
        }
    }
}
