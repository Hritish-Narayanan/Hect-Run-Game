using UnityEngine;

namespace SubwaySurfers.Core
{
    /// <summary>
    /// Score = distance + coins, with a coin combo multiplier that decays if
    /// you stop collecting. Owns the run's numbers; commits to SaveSystem on
    /// game over.
    /// </summary>
    public sealed class ScoreSystem : MonoBehaviour
    {
        public int Score { get; private set; }
        public int Coins { get; private set; }
        public int Combo { get; private set; } = 1;
        public bool NewBest { get; private set; }

        private float comboTimer;
        private int bestAtStart;
        public int BestAtStart => bestAtStart;
        private int startZ;

        private void Awake() => Game.Register(this);

        private void OnEnable()
        {
            if (Game.Get<SaveSystem>() != null)
                bestAtStart = Game.Get<SaveSystem>().Best;
        }

        public void ResetRun()
        {
            Score = 0;
            Coins = 0;
            Combo = 1;
            NewBest = false;
            comboTimer = 0f;
            bestAtStart = Game.Get<SaveSystem>() != null ? Game.Get<SaveSystem>().Best : 0;
            var pt = Game.I != null ? Game.I.PlayerTransform : null;
            startZ = pt != null ? Mathf.FloorToInt(pt.position.z) : 0;
        }

        private void Update()
        {
            if (Game.I == null || !Game.I.Playing) return;

            var pt = Game.I.PlayerTransform;
            if (pt != null)
            {
                int dist = Mathf.Max(0, Mathf.FloorToInt(pt.position.z) - startZ);
                Score = Mathf.Max(Score, dist);
            }

            if (Combo > 1)
            {
                comboTimer -= Time.deltaTime;
                if (comboTimer <= 0f) Combo = 1;
            }
        }

        /// <summary>Coins feed score through the combo multiplier.</summary>
        public void AddCoin()
        {
            int multiplier = Combo * (Game.Get<PowerupSystem>() != null && Game.Get<PowerupSystem>().DoubleScore ? 2 : 1);
            Coins += 1;
            Game.Get<SaveSystem>()?.RecordCoin();
            Score += GameConfig.CoinScore * multiplier;
            if (Coins % 50 == 0)
                Game.I?.SpeedUpFromCoins();

            Combo = Mathf.Min(GameConfig.MaxCombo, Combo + 1);
            comboTimer = GameConfig.ComboWindow;
        }

        public void AddScore(int flat) => Score += flat;

        public void CommitRun()
        {
            NewBest = Score > bestAtStart;
            Game.Get<SaveSystem>()?.RecordRun(Score, Coins);
        }
    }
}
