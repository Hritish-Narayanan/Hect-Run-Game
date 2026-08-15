using UnityEngine;

namespace SubwaySurfers.Core
{
    public enum PowerupType { Magnet, Shield, DoubleScore }

    /// <summary>
    /// Timed powerups with UI-readable remaining time. Magnet/2x are timed;
    /// Shield is a one-hit absorb.
    /// </summary>
    public sealed class PowerupSystem : MonoBehaviour
    {
        public bool Magnet { get; private set; }
        public bool Shield { get; private set; }
        public bool DoubleScore { get; private set; }

        public float MagnetTimeLeft { get; private set; }
        public float DoubleTimeLeft { get; private set; }

        public const float MagnetDuration = 9f;
        public const float DoubleDuration = 9f;

        private void Awake() => Game.Register(this);

        public void ResetRun()
        {
            Magnet = Shield = DoubleScore = false;
            MagnetTimeLeft = DoubleTimeLeft = 0f;
        }

        private void Update()
        {
            if (Game.I == null || !Game.I.Playing) return;

            if (Magnet)
            {
                MagnetTimeLeft -= Time.deltaTime;
                if (MagnetTimeLeft <= 0f) Magnet = false;
            }
            if (DoubleScore)
            {
                DoubleTimeLeft -= Time.deltaTime;
                if (DoubleTimeLeft <= 0f) DoubleScore = false;
            }
        }

        public void Grant(PowerupType type)
        {
            switch (type)
            {
                case PowerupType.Magnet:
                    Magnet = true;
                    MagnetTimeLeft = MagnetDuration;
                    break;
                case PowerupType.DoubleScore:
                    DoubleScore = true;
                    DoubleTimeLeft = DoubleDuration;
                    break;
                case PowerupType.Shield:
                    Shield = true;
                    break;
            }
            Game.Get<Audio.AudioSystem>()?.PlayPowerup();
        }

        /// <summary>Absorbs one hit if shielded. Returns true if absorbed.</summary>
        public bool TryAbsorbHit()
        {
            if (!Shield) return false;
            Shield = false;
            return true;
        }
    }
}
