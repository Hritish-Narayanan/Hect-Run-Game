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
        public const float JumpForce = 6.8f;
        public const float FallMultiplier = 2.0f;
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
        public const string KeyFPS = "surfer_fps";

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
                case "shield":
                    var shieldMat = MakeMat(color);
                    shieldMat.SetFloat("_Surface", 1f); // 1.0 is Transparent in URP Lit
                    shieldMat.SetFloat("_Blend", 0f); // 0.0 is Alpha blend
                    shieldMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    shieldMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    shieldMat.SetInt("_ZWrite", 0); // Disable depth write
                    shieldMat.DisableKeyword("_ALPHATEST_ON");
                    shieldMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    shieldMat.EnableKeyword("_BLENDMODE_ALPHA");
                    shieldMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                    shieldMat.EnableKeyword("_EMISSION");
                    shieldMat.SetColor("_EmissionColor", color * 0.6f); // Glowing edge/emission
                    return shieldMat;
                default:
                    return MakeMat(color);
            }
        }

        private static Shader defaultShader;
        private static Shader DefaultShader
        {
            get
            {
                if (defaultShader == null)
                {
                    // 1. Try loading from the dummy material in Resources.
                    // This is the most reliable way in URP builds on mobile because the material (and its shader)
                    // are guaranteed to be included in the build via the Resources directory.
                    var dummyMat = Resources.Load<Material>("URPLitDummy");
                    if (dummyMat != null && dummyMat.shader != null)
                    {
                        defaultShader = dummyMat.shader;
                    }
                }
                if (defaultShader == null || defaultShader.name == "Standard")
                {
                    // 2. Try the primitive cube route
                    var temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    var renderer = temp.GetComponent<Renderer>();
                    if (renderer != null && renderer.sharedMaterial != null)
                    {
                        var s = renderer.sharedMaterial.shader;
                        if (s != null && s.name != "Standard")
                        {
                            defaultShader = s;
                        }
                    }
                    Object.DestroyImmediate(temp);
                }
                if (defaultShader == null || defaultShader.name == "Standard")
                {
                    defaultShader = Shader.Find("Universal Render Pipeline/Lit");
                }
                if (defaultShader == null)
                {
                    defaultShader = Shader.Find("Standard");
                }
                return defaultShader;
            }
        }

        private static Material MakeMat(Color c)
        {
            var shader = DefaultShader;
            if (shader == null)
            {
                // Absolute fallback just in case
                return new Material(Shader.Find("Hidden/Internal-Colored")) { color = c };
            }
            return new Material(shader) { color = c };
        }
    }
}
