using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SubwaySurfers.Core
{
    public enum GameState { Menu, Playing, Paused, GameOver }

    /// <summary>
    /// Central game orchestrator. Owns state machine, speed, subsystems and the
    /// run lifecycle. Subsystems register here via Awake; nothing reaches for
    /// anything else directly.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class Game : MonoBehaviour
    {
        public static Game I { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (I != null) return;
            var root = new GameObject("SubwaySurfers");
            root.AddComponent<Game>();
        }

        // ---- Subsystem registry (central service locator) ----
        private readonly Dictionary<System.Type, Component> systems = new Dictionary<System.Type, Component>();
        public static void Register<T>(T c) where T : Component
        {
            if (I != null) I.systems[typeof(T)] = c;
        }
        public static T Get<T>() where T : Component
        {
            if (I == null) return null;
            I.systems.TryGetValue(typeof(T), out var c);
            return (T)c;
        }

        // ---- Run state ----
        public GameState State { get; private set; } = GameState.Menu;
        public float Speed { get; private set; } = GameConfig.StartSpeed;
        public Transform PlayerTransform => player != null ? player.transform : null;
        public bool Playing => State == GameState.Playing;

        private Player.PlayerController player;

        private void Awake()
        {
            if (I != null && I != this) { Destroy(gameObject); return; }
            I = this;
            DontDestroyOnLoad(gameObject);

            Application.targetFrameRate = GetTargetFrameRate();
            QualitySettings.vSyncCount = 0;

            SetupEnvironment();
            StartCoroutine(Boot());
        }

        private static int GetTargetFrameRate()
        {
            double hz = Screen.currentResolution.refreshRateRatio.value;
            return hz > 1 ? (int)hz : 60;
        }

        /// <summary>
        /// Build order matters: save first (data), audio/effects (no deps), track
        /// (needs pools), player (needs track), camera (needs player), UI last.
        /// </summary>
        private IEnumerator Boot()
        {
            gameObject.AddComponent<SaveSystem>();

            new GameObject("Audio").AddComponent<Audio.AudioSystem>().transform.SetParent(transform, false);
            new GameObject("Effects").AddComponent<Effects.EffectsSystem>().transform.SetParent(transform, false);
            new GameObject("Score").AddComponent<ScoreSystem>().transform.SetParent(transform, false);
            new GameObject("Powerups").AddComponent<PowerupSystem>().transform.SetParent(transform, false);

            var track = new GameObject("Track").AddComponent<Track.TrackSystem>();
            track.transform.SetParent(transform, false);

            yield return null; // let pools initialise

            SpawnPlayer();

            Camera.main.gameObject.AddComponent<Presentation.CameraController>();
            new GameObject("UI").AddComponent<UI.UISystem>().transform.SetParent(transform, false);

            SetState(GameState.Menu);
        }

        private void SetupEnvironment()
        {
            RenderSettings.fog = true;
            RenderSettings.fogColor = GameConfig.FogColor;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 60f;
            RenderSettings.fogEndDistance = 190f;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.72f, 0.80f, 0.90f);
            RenderSettings.ambientEquatorColor = new Color(0.55f, 0.58f, 0.62f);
            RenderSettings.ambientGroundColor = new Color(0.30f, 0.30f, 0.34f);

            var cam = Camera.main;
            if (cam == null)
            {
                var co = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = co.AddComponent<Camera>();
                co.AddComponent<AudioListener>();
            }
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = GameConfig.SkyColor;
            cam.fieldOfView = 62f;

            if (FindAnyObjectByType<Light>() == null)
            {
                var lo = new GameObject("Sun");
                var l = lo.AddComponent<Light>();
                l.type = LightType.Directional;
                l.intensity = 1.5f;
                l.color = new Color(1f, 0.96f, 0.88f);
                lo.transform.rotation = Quaternion.Euler(48f, -25f, 0f);
                l.shadows = LightShadows.Soft;
            }
        }

        private void SpawnPlayer()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "Player";
            go.tag = "Player";
            go.transform.position = new Vector3(0f, 1f, 0f);
            go.GetComponent<Renderer>().material = GameConfig.GetMaterial("player", GameConfig.PlayerColor);
            player = go.AddComponent<Player.PlayerController>();
        }

        private void Update()
        {
            if (State != GameState.Playing) return;
            Speed = Mathf.Min(GameConfig.MaxSpeed, Speed + GameConfig.SpeedRamp * Time.deltaTime);
        }

        public void SpeedUpFromCoins()
        {
            Speed = Mathf.Min(GameConfig.MaxSpeed, Speed + 0.75f);
        }

        // ---- Run lifecycle (called by UI / PlayerController) ----

        public void StartRun()
        {
            if (State == GameState.Playing) return;
            ResetRun();
            SetState(GameState.Playing);
        }

        public void Pause()
        {
            if (State != GameState.Playing) return;
            SetState(GameState.Paused);
            Time.timeScale = 0f;
        }

        public void Resume()
        {
            if (State != GameState.Paused) return;
            Time.timeScale = 1f;
            SetState(GameState.Playing);
        }

        public void Crash()
        {
            if (State != GameState.Playing) return;
            Get<Effects.EffectsSystem>()?.CrashShake();
            Get<Audio.AudioSystem>()?.PlayCrash();
            SetState(GameState.GameOver);
            Time.timeScale = 1f;
            Get<ScoreSystem>()?.CommitRun();
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void ResetRun()
        {
            Time.timeScale = 1f;
            Speed = GameConfig.StartSpeed;

            Get<Track.TrackSystem>()?.ResetTrack();
            Get<ScoreSystem>()?.ResetRun();
            Get<PowerupSystem>()?.ResetRun();
            Get<Effects.EffectsSystem>()?.ResetRun();
            Get<Presentation.CameraController>()?.SnapToTarget();

            if (player != null) Destroy(player.gameObject);
            SpawnPlayer();
        }

        private void SetState(GameState s)
        {
            State = s;
            Get<UI.UISystem>()?.OnStateChanged(s);
        }
    }
}
