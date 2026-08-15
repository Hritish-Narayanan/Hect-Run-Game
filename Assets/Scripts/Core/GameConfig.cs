using UnityEngine;

namespace SubwaySurfers.Core
{
    /// <summary>
    /// All gameplay tuning lives here. One place to balance the entire game.
    /// </summary>
    public static class GameConfig
    {
        // Lanes
        public const int LaneCount = 3;
        public const float LaneWidth = 2.6f;

        // Track
        public const float SegmentLength = 18f;
        public const float TrackWidth = 12f;
        public const float TrackThickness = 0.9f;
        public const float SpawnDistance = 150f;
        public const float DespawnDistance = 34f;

        // Movement / speed
        public const float StartSpeed = 11f;
        public const float MaxSpeed = 34f;
        public const float SpeedRamp = 0.12f;
        public const float LaneChangeSharpness = 13f;
        public const float LaneLeanDegrees = 14f;

        // Jump / gravity / roll
        public const float JumpForce = 8.2f;
        public const float FallMultiplier = 2.6f;
        public const float RollDuration = 0.65f;
        public const float RollHeight = 0.6f;
        public const float PlayerHeight = 2f;

        // Scoring
        public const float ComboWindow = 3f;
        public const int MaxCombo = 8;
        public const int CoinScore = 10;

        // Magnet
        public const float MagnetRadius = 6f;
        public const float MagnetSpeed = 14f;

        // PlayerPrefs keys
        public const string KeyBest = "surfer_best";
        public const string KeyCoins = "surfer_coins";
        public const string KeyQuality = "surfer_quality";
        public const string KeyFullscreen = "surfer_fullscreen";
        public const string KeyVolume = "surfer_volume";
        public const string KeyMuted = "surfer_muted";

        // Palette
        public static readonly Color SkyColor = new Color(0.53f, 0.76f, 0.95f);
        public static readonly Color FogColor = new Color(0.65f, 0.81f, 0.93f);
        public static readonly Color TrackColor = new Color(0.20f, 0.22f, 0.25f);
        public static readonly Color SleeperColor = new Color(0.32f, 0.27f, 0.22f);
        public static readonly Color RailColor = new Color(0.55f, 0.56f, 0.58f);
        public static readonly Color PlayerColor = new Color(0.98f, 0.75f, 0.24f);
        public static readonly Color TrainBodyColor = new Color(0.80f, 0.16f, 0.12f);
        public static readonly Color TrainRoofColor = new Color(0.62f, 0.10f, 0.08f);
        public static readonly Color BarrierColor = new Color(0.95f, 0.55f, 0.10f);
        public static readonly Color OverheadColor = new Color(0.25f, 0.28f, 0.34f);
        public static readonly Color CoinColor = new Color(1.00f, 0.84f, 0.10f);
        public static readonly Color BuildingColor = new Color(0.52f, 0.56f, 0.62f);
        public static readonly Color[] PowerupColors =
        {
            new Color(0.20f, 0.85f, 1.00f), // Magnet  - cyan
            new Color(0.35f, 1.00f, 0.45f), // Shield  - green
            new Color(1.00f, 0.45f, 0.90f), // 2x      - pink
        };

        public static bool LowPolyMode { get; private set; }

        public static void SetQualityMode(bool lowPoly)
        {
            LowPolyMode = lowPoly;
            QualitySettings.SetQualityLevel(lowPoly ? 0 : Mathf.Min(2, QualitySettings.names.Length - 1), true);
        }

        // Palette-driven material cache so primitives share materials (fewer draw calls, no leaks).
        private static Material coinMat, sleeperMat, railMat, trackMat;

        public static Material GetMaterial(string key, Color color)
        {
            switch (key)
            {
                case "track":
                    if (trackMat == null) trackMat = MakeMat(color);
                    return trackMat;
                case "sleeper":
                    if (sleeperMat == null) sleeperMat = MakeMat(color);
                    return sleeperMat;
                case "rail":
                    if (railMat == null) railMat = MakeMat(color);
                    return railMat;
                case "coin":
                    if (coinMat == null)
                    {
                        coinMat = MakeMat(color);
                        coinMat.EnableKeyword("_EMISSION");
                        coinMat.SetColor("_EmissionColor", color * 0.9f);
                    }
                    return coinMat;
                default:
                    return MakeMat(color);
            }
        }

        private static Material MakeMat(Color c)
        {
            return new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = c };
        }
    }
}
