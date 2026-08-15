using UnityEngine;

namespace SubwaySurfers.Track
{
    /// <summary>
    /// Magnet attraction + coin spin. Coins are triggers on the Player layer.
    /// </summary>
    [RequireComponent(typeof(SphereCollider))]
    public sealed class Coin : MonoBehaviour
    {
        private float spinSpeed = 160f;
        private bool magnetised;
        private bool collected;
        private const float PickupRadius = 1.35f;

        private void OnEnable()
        {
            magnetised = false;
            collected = false;
        }

        private void Update()
        {
            transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);

            var power = Core.Game.Get<Core.PowerupSystem>();
            var player = Core.Game.I != null ? Core.Game.I.PlayerTransform : null;
            if (player == null || Core.Game.I == null || !Core.Game.I.Playing) return;

            float d = Vector3.Distance(transform.position, player.position);
            // Distance fallback keeps collection reliable on devices where a
            // pooled trigger misses a physics callback during activation.
            if (d <= PickupRadius)
            {
                Collect();
                return;
            }

            if (power == null || !power.Magnet) return;
            if (d < Core.GameConfig.MagnetRadius) magnetised = true;
            if (magnetised)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position, player.position, Core.GameConfig.MagnetSpeed * Time.deltaTime);
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
            Core.Game.Get<Core.ScoreSystem>()?.AddCoin();
            Core.Game.Get<Effects.EffectsSystem>()?.CoinPickup(transform.position);
            Core.Game.Get<Audio.AudioSystem>()?.PlayCoin();
            gameObject.SetActive(false);
        }
    }
}
