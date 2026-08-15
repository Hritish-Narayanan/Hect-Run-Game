using UnityEngine;

namespace SubwaySurfers.Core
{
    /// <summary>
    /// Single-writer persistence. Flushes PlayerPrefs on pause/quit so the
    /// old "best score lost on kill" bug can't happen. Auto-migrates the old
    /// "runner_bestscore" key from the previous version.
    /// </summary>
    public sealed class SaveSystem : MonoBehaviour
    {
        private int best;
        private int totalCoins;
        private bool dirty;

        public int Best => best;
        public int TotalCoins => totalCoins;

        private void Awake()
        {
            Core.Game.Register(this);

            // Migrate legacy best score
            if (!PlayerPrefs.HasKey(GameConfig.KeyBest) && PlayerPrefs.HasKey("runner_bestscore"))
            {
                PlayerPrefs.SetInt(GameConfig.KeyBest, PlayerPrefs.GetInt("runner_bestscore"));
                PlayerPrefs.DeleteKey("runner_bestscore");
                dirty = true;
            }

            best = PlayerPrefs.GetInt(GameConfig.KeyBest, 0);
            totalCoins = PlayerPrefs.GetInt(GameConfig.KeyCoins, 0);
        }

        public void RecordCoin()
        {
            totalCoins++;
            PlayerPrefs.SetInt(GameConfig.KeyCoins, totalCoins);
            dirty = true;
            Flush();
        }

        public void RecordRun(int score, int coinsEarned)
        {
            if (score > best)
            {
                best = score;
                PlayerPrefs.SetInt(GameConfig.KeyBest, best);
                dirty = true;
            }
            if (dirty) Flush();
        }

        public float GetFloat(string key, float fallback) => PlayerPrefs.GetFloat(key, fallback);
        public int GetInt(string key, int fallback) => PlayerPrefs.GetInt(key, fallback);

        public void SetFloat(string key, float v) { PlayerPrefs.SetFloat(key, v); dirty = true; }
        public void SetInt(string key, int v) { PlayerPrefs.SetInt(key, v); dirty = true; }

        public void Flush()
        {
            PlayerPrefs.Save();
            dirty = false;
        }

        private void OnApplicationPause(bool paused) { if (paused && dirty) Flush(); }
        private void OnApplicationQuit() { if (dirty) Flush(); }
    }
}
