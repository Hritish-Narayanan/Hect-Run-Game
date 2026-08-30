using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SubwaySurfers.UI
{
    /// <summary>
    /// All UI, built in code but organised: HUD, animated panels, settings,
    /// mobile controls. Panels scale-in, buttons have hover/press states,
    /// powerup timers show as chips.
    /// </summary>
    public sealed class UISystem : MonoBehaviour
    {
        private Canvas canvas;
        private Text scoreText, bestText, coinText, totalCoinText, comboText, gameOverScore, gameOverBest;
        private RectTransform hud;
        private GameObject startPanel, pausePanel, gameOverPanel, settingsPanel, mobileControls;
        private Dropdown resDropdown, qualityDropdown;
        private Toggle fullscreenToggle, muteToggle;
        private Slider volumeSlider;
        private RectTransform magnetChip, doubleChip;
        private Text magnetTime, doubleTime;
        private Vector2 lastScreenSize;

        private readonly Dictionary<GameObject, Coroutine> panelAnims = new Dictionary<GameObject, Coroutine>();

        private void Awake()
        {
            Core.Game.Register(this);
            Build();
        }

        private void Build()
        {
            EnsureEventSystem();

            var co = new GameObject("UI Canvas");
            co.transform.SetParent(transform, false);
            canvas = co.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = co.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = Screen.width > Screen.height ? 1f : 0f;
            lastScreenSize = new Vector2(Screen.width, Screen.height);
            co.AddComponent<GraphicRaycaster>();

            BuildHUD();
            BuildStart();
            BuildPause();
            BuildGameOver();
            BuildSettings();
            BuildMobile();

            var input = Core.Game.Get<Core.InputReader>();
            if (input != null)
            {
                input.OnAnyStart += OnAnyStart;
                input.OnPause += OnPauseKey;
            }
        }

        private static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
                DontDestroyOnLoad(es);
            }
        }

        // ---------------- HUD ----------------

        private void BuildHUD()
        {
            var hudGo = new GameObject("HUD", typeof(RectTransform));
            hudGo.transform.SetParent(canvas.transform, false);
            hud = hudGo.GetComponent<RectTransform>();
            hud.anchorMin = Vector2.zero;
            hud.anchorMax = Vector2.one;
            hud.offsetMin = hud.offsetMax = Vector2.zero;

            scoreText = MakeText(hud, "0", 44, TextAnchor.UpperLeft, new Vector2(30f, -24f));
            bestText = MakeText(hud, "Best 0", 20, TextAnchor.UpperLeft, new Vector2(32f, -78f));
            coinText = MakeText(hud, "◎ 0", 26, TextAnchor.UpperRight, new Vector2(-110f, -30f));
            totalCoinText = MakeText(hud, "Total ◎ 0", 17, TextAnchor.UpperRight, new Vector2(-110f, -66f));
            comboText = MakeText(hud, "", 24, TextAnchor.UpperRight, new Vector2(-110f, -102f));

            var pauseBtn = MakeButton(hud, "II", new Vector2(56f, 56f), () => Core.Game.I?.Pause());
            var rt = pauseBtn.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-40f, -40f);

            magnetChip = MakeChip(hud, "MAGNET", new Vector2(-240f, -34f), new Color(0.2f, 0.85f, 1f), out magnetTime);
            doubleChip = MakeChip(hud, "2X SCORE", new Vector2(-240f, -70f), new Color(1f, 0.45f, 0.9f), out doubleTime);
        }

        private RectTransform MakeChip(RectTransform parent, string label, Vector2 pos, Color color, out Text timeText)
        {
            var chip = new GameObject(label + "Chip", typeof(RectTransform), typeof(Image));
            chip.transform.SetParent(parent, false);
            var rt = chip.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(130f, 30f);
            chip.GetComponent<Image>().color = new Color(color.r, color.g, color.b, 0.25f);

            var txt = MakeText(rt, label, 15, TextAnchor.MiddleLeft, Vector2.zero);
            txt.rectTransform.anchorMin = Vector2.zero;
            txt.rectTransform.anchorMax = Vector2.one;
            txt.rectTransform.offsetMin = new Vector2(10f, 0f);
            txt.rectTransform.offsetMax = new Vector2(-40f, 0f);
            txt.color = color;

            timeText = MakeText(rt, "9s", 15, TextAnchor.MiddleRight, Vector2.zero);
            timeText.rectTransform.anchorMin = Vector2.zero;
            timeText.rectTransform.anchorMax = Vector2.one;
            timeText.rectTransform.offsetMin = new Vector2(70f, 0f);
            timeText.rectTransform.offsetMax = new Vector2(-8f, 0f);
            timeText.color = color;

            chip.SetActive(false);
            return rt;
        }

        // ---------------- Panels ----------------

        private void BuildStart()
        {
            startPanel = MakePanel("StartPanel", new Vector2(520f, 320f), new Color(0.05f, 0.05f, 0.07f, 0.9f));
            MakeText(startPanel.transform as RectTransform, "SUBWAY SURFERS", 46, TextAnchor.MiddleCenter, new Vector2(0f, 90f));
            MakeText(startPanel.transform as RectTransform, "ENTER / TAP TO RUN", 20, TextAnchor.MiddleCenter, new Vector2(0f, 30f));
            MakeText(startPanel.transform as RectTransform, "A/D or Arrows — lanes\nSPACE / W — jump    S — roll", 17, TextAnchor.MiddleCenter, new Vector2(0f, -40f));
            MakeButton(startPanel.transform as RectTransform, "START RUN", new Vector2(200f, 48f), () => Core.Game.I?.StartRun())
                .GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -110f);
        }

        private void BuildPause()
        {
            pausePanel = MakePanel("PausePanel", new Vector2(440f, 480f), new Color(0.07f, 0.08f, 0.11f, 0.95f));
            MakeText(pausePanel.transform as RectTransform, "PAUSED", 38, TextAnchor.MiddleCenter, new Vector2(0f, 180f));
            MakeButton(pausePanel.transform as RectTransform, "RESUME", new Vector2(200f, 46f), () => Core.Game.I?.Resume())
                .GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 110f);
            MakeButton(pausePanel.transform as RectTransform, "RESTART", new Vector2(200f, 46f), () => Core.Game.I?.StartRun())
                .GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 50f);
            MakeButton(pausePanel.transform as RectTransform, "SETTINGS", new Vector2(200f, 46f), ShowSettings)
                .GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -10f);
            MakeButton(pausePanel.transform as RectTransform, "QUIT", new Vector2(200f, 46f), () => Core.Game.I?.QuitGame())
                .GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -70f);
            pausePanel.SetActive(false);
        }

        private void BuildGameOver()
        {
            gameOverPanel = MakePanel("GameOverPanel", new Vector2(480f, 320f), new Color(0.16f, 0.05f, 0.06f, 0.95f));
            MakeText(gameOverPanel.transform as RectTransform, "WIPED OUT", 42, TextAnchor.MiddleCenter, new Vector2(0f, 100f));
            gameOverScore = MakeText(gameOverPanel.transform as RectTransform, "0", 56, TextAnchor.MiddleCenter, new Vector2(0f, 20f));
            gameOverBest = MakeText(gameOverPanel.transform as RectTransform, "Best 0", 22, TextAnchor.MiddleCenter, new Vector2(0f, -50f));
            MakeButton(gameOverPanel.transform as RectTransform, "RUN AGAIN (R)", new Vector2(220f, 48f), () => Core.Game.I?.StartRun())
                .GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -120f);
            gameOverPanel.SetActive(false);
        }

        private void BuildSettings()
        {
            settingsPanel = MakePanel("SettingsPanel", new Vector2(560f, 480f), new Color(0.06f, 0.08f, 0.10f, 0.98f));
            MakeText(settingsPanel.transform as RectTransform, "SETTINGS", 34, TextAnchor.MiddleCenter, new Vector2(0f, 190f));

            MakeText(settingsPanel.transform as RectTransform, "Refresh Rate", 18, TextAnchor.MiddleLeft, new Vector2(-240f, 130f));
            resDropdown = MakeDropdown(settingsPanel.transform as RectTransform, new Vector2(90f, 130f));

            MakeText(settingsPanel.transform as RectTransform, "Fullscreen", 18, TextAnchor.MiddleLeft, new Vector2(-240f, 80f));
            fullscreenToggle = MakeToggle(settingsPanel.transform as RectTransform, new Vector2(90f, 80f));

            MakeText(settingsPanel.transform as RectTransform, "Quality", 18, TextAnchor.MiddleLeft, new Vector2(-240f, 30f));
            qualityDropdown = MakeDropdown(settingsPanel.transform as RectTransform, new Vector2(90f, 30f));

            MakeText(settingsPanel.transform as RectTransform, "Volume", 18, TextAnchor.MiddleLeft, new Vector2(-240f, -20f));
            volumeSlider = MakeSlider(settingsPanel.transform as RectTransform, new Vector2(90f, -20f));

            MakeText(settingsPanel.transform as RectTransform, "Mute", 18, TextAnchor.MiddleLeft, new Vector2(-240f, -70f));
            muteToggle = MakeToggle(settingsPanel.transform as RectTransform, new Vector2(90f, -70f));

            MakeButton(settingsPanel.transform as RectTransform, "APPLY", new Vector2(170f, 46f), ApplySettings)
                .GetComponent<RectTransform>().anchoredPosition = new Vector2(-100f, -160f);
            MakeButton(settingsPanel.transform as RectTransform, "BACK", new Vector2(170f, 46f), HideSettings)
                .GetComponent<RectTransform>().anchoredPosition = new Vector2(100f, -160f);
            settingsPanel.SetActive(false);
        }

        private void BuildMobile()
        {
            mobileControls = new GameObject("MobileControls", typeof(RectTransform));
            mobileControls.transform.SetParent(canvas.transform, false);
            var mr = mobileControls.GetComponent<RectTransform>();
            mr.anchorMin = Vector2.zero;
            mr.anchorMax = Vector2.one;
            mr.offsetMin = mr.offsetMax = Vector2.zero;

            var left = MakeButton(mr, "<", new Vector2(110f, 90f), () => Core.Game.Get<Player.PlayerController>()?.MoveLane(-1));
            var right = MakeButton(mr, ">", new Vector2(110f, 90f), () => Core.Game.Get<Player.PlayerController>()?.MoveLane(1));
            var jump = MakeButton(mr, "JUMP", new Vector2(150f, 90f), () => Core.Game.Get<Player.PlayerController>()?.Jump());
            var roll = MakeButton(mr, "ROLL", new Vector2(150f, 90f), () => Core.Game.Get<Player.PlayerController>()?.Roll());

            PlaceMobile(left, new Vector2(0.5f, 0f), new Vector2(-220f, 70f));
            PlaceMobile(right, new Vector2(0.5f, 0f), new Vector2(220f, 70f));
            PlaceMobile(jump, new Vector2(0.5f, 0f), new Vector2(-70f, 70f));
            PlaceMobile(roll, new Vector2(0.5f, 0f), new Vector2(70f, 70f));

            mobileControls.SetActive(Application.isMobilePlatform || Input.touchSupported);
            ApplySafeArea();
        }

        private static void PlaceMobile(GameObject go, Vector2 anchor, Vector2 pos)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
        }

        private void ApplySafeArea()
        {
            Rect safe = Screen.safeArea;
            Vector2 min = safe.position;
            Vector2 max = safe.position + safe.size;
            var screen = new Vector2(Screen.width, Screen.height);
            if (screen.x <= 0f || screen.y <= 0f) return;

            hud.offsetMin = min;
            hud.offsetMax = max - screen;

            var mobileRect = mobileControls.GetComponent<RectTransform>();
            mobileRect.offsetMin = min;
            mobileRect.offsetMax = max - screen;
        }

        private void UpdateResponsiveLayout()
        {
            var size = new Vector2(Screen.width, Screen.height);
            if (size == lastScreenSize) return;
            lastScreenSize = size;
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.referenceResolution = size.x > size.y ? new Vector2(1920f, 1080f) : new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = size.x > size.y ? 1f : 0f;
            ApplySafeArea();
        }

        // ---------------- State handling ----------------

        public void OnStateChanged(Core.GameState state)
        {
            switch (state)
            {
                case Core.GameState.Menu:
                    ShowPanel(startPanel);
                    HidePanel(pausePanel);
                    HidePanel(gameOverPanel);
                    hud.gameObject.SetActive(false);
                    if (mobileControls != null) mobileControls.SetActive(false);
                    break;
                case Core.GameState.Playing:
                    HidePanel(startPanel);
                    HidePanel(pausePanel);
                    HidePanel(gameOverPanel);
                    HidePanel(settingsPanel);
                    hud.gameObject.SetActive(true);
                    if (mobileControls != null && (Application.isMobilePlatform || Input.touchSupported))
                        mobileControls.SetActive(true);
                    break;
                case Core.GameState.Paused:
                    ShowPanel(pausePanel);
                    break;
                case Core.GameState.GameOver:
                    ShowGameOver();
                    if (mobileControls != null) mobileControls.SetActive(false);
                    break;
            }
        }

        private void ShowGameOver()
        {
            var score = Core.Game.Get<Core.ScoreSystem>();
            if (score != null)
            {
                gameOverScore.text = score.Score.ToString("N0");
                gameOverBest.text = score.NewBest ? "★ NEW BEST ★" : $"Best {score.BestAtStart:N0}";
            }
            ShowPanel(gameOverPanel);
        }

        private void OnAnyStart()
        {
            if (Core.Game.I == null) return;
            if (Core.Game.I.State == Core.GameState.Menu) Core.Game.I.StartRun();
            else if (Core.Game.I.State == Core.GameState.GameOver) Core.Game.I.StartRun();
        }

        private void OnPauseKey()
        {
            if (Core.Game.I == null) return;
            if (Core.Game.I.State == Core.GameState.Playing) Core.Game.I.Pause();
            else if (Core.Game.I.State == Core.GameState.Paused) Core.Game.I.Resume();
        }

        private void Update()
        {
            UpdateResponsiveLayout();
            if (Core.Game.I == null || !Core.Game.I.Playing) return;

            var score = Core.Game.Get<Core.ScoreSystem>();
            if (score != null)
            {
                scoreText.text = score.Score.ToString("N0");
                bestText.text = $"Best {Core.Game.Get<Core.SaveSystem>()?.Best ?? 0:N0}";
                coinText.text = $"◎ {score.Coins}";
                totalCoinText.text = $"Total ◎ {Core.Game.Get<Core.SaveSystem>()?.TotalCoins ?? score.Coins}";
                comboText.text = score.Combo > 1 ? $"x{score.Combo} combo" : "";
            }

            var power = Core.Game.Get<Core.PowerupSystem>();
            if (power != null)
            {
                magnetChip.gameObject.SetActive(power.Magnet);
                doubleChip.gameObject.SetActive(power.DoubleScore);
                if (power.Magnet) magnetTime.text = Mathf.CeilToInt(power.MagnetTimeLeft) + "s";
                if (power.DoubleScore) doubleTime.text = Mathf.CeilToInt(power.DoubleTimeLeft) + "s";
            }
        }

        // ---------------- Settings ----------------

        private void ShowSettings()
        {
            PopulateSettings();
            pausePanel.SetActive(false);
            ShowPanel(settingsPanel);
        }

        private void HideSettings()
        {
            HidePanel(settingsPanel);
            if (Core.Game.I != null && Core.Game.I.State == Core.GameState.Paused)
                ShowPanel(pausePanel);
        }

        private void PopulateSettings()
        {
            resDropdown.ClearOptions();
            var options = new List<string>();
            bool mobile = Application.isMobilePlatform || Input.touchSupported;
            int selectedIndex = 0;
 
            if (mobile)
            {
                var mobileFrameRates = new List<int> { 30, 60, 90, 120, -1 };
                foreach (int fps in mobileFrameRates)
                {
                    if (fps == -1) options.Add("Unlocked");
                    else options.Add(fps + " FPS");
                }
 
                int currentFPS = PlayerPrefs.GetInt(Core.GameConfig.KeyFPS, -1);
                int index = mobileFrameRates.IndexOf(currentFPS);
                if (index < 0) index = mobileFrameRates.IndexOf(-1);
 
                selectedIndex = Mathf.Max(0, index);
            }
            else
            {
                foreach (var resolution in Screen.resolutions)
                    options.Add($"{resolution.width} x {resolution.height}");
                int current = 0;
                for (int i = 0; i < Screen.resolutions.Length; i++)
                    if (Screen.resolutions[i].width == Screen.currentResolution.width && Screen.resolutions[i].height == Screen.currentResolution.height) current = i;
                selectedIndex = current;
            }
            resDropdown.AddOptions(options);
            resDropdown.value = selectedIndex; // Set value AFTER adding options to prevent clamping!
            resDropdown.RefreshShownValue();

            qualityDropdown.ClearOptions();
            if (mobile)
            {
                qualityDropdown.AddOptions(new List<string> { "Mobile" });
                qualityDropdown.value = 0;
            }
            else
            {
                qualityDropdown.AddOptions(new List<string>(QualitySettings.names));
                qualityDropdown.value = QualitySettings.GetQualityLevel();
            }
            qualityDropdown.RefreshShownValue();

            fullscreenToggle.isOn = Screen.fullScreen;
            var audio = Core.Game.Get<Audio.AudioSystem>();
            volumeSlider.value = audio != null ? audio.Volume : 1f;
            muteToggle.isOn = audio != null && audio.Muted;
        }

        private void ApplySettings()
        {
            bool mobile = Application.isMobilePlatform || Input.touchSupported;
            var save = Core.Game.Get<Core.SaveSystem>();
            if (mobile)
            {
                var mobileFrameRates = new List<int> { 30, 60, 90, 120, -1 };
                if (resDropdown.value >= 0 && resDropdown.value < mobileFrameRates.Count)
                {
                    int selectedFPS = mobileFrameRates[resDropdown.value];
                    
                    // Force display mode to maximum supported refresh rate (e.g. 120Hz)
                    Core.Game.UnlockAndroidRefreshRate();
                    int maxHz = Core.Game.GetMaxSupportedRefreshRate();
                    var refresh = new RefreshRate { numerator = (uint)maxHz, denominator = 1 };
                    Screen.SetResolution(Screen.width, Screen.height, Screen.fullScreenMode, refresh);
                    
                    QualitySettings.vSyncCount = 1; // Always keep VSync enabled on mobile
                    Application.targetFrameRate = selectedFPS == -1 ? -1 : Mathf.Min(selectedFPS, maxHz);
                    
                    if (save != null) save.SetInt(Core.GameConfig.KeyFPS, selectedFPS);
                    else PlayerPrefs.SetInt(Core.GameConfig.KeyFPS, selectedFPS);
                }
                Core.GameConfig.SetQualityMode(true);
            }
            else
            {
                if (resDropdown.value >= 0 && resDropdown.value < Screen.resolutions.Length)
                {
                    var r = Screen.resolutions[resDropdown.value];
                    Screen.SetResolution(r.width, r.height, fullscreenToggle.isOn);
                }
                Core.GameConfig.SetQualityMode(false);
                QualitySettings.SetQualityLevel(qualityDropdown.value, true);
            }
            var audio = Core.Game.Get<Audio.AudioSystem>();
            if (audio != null)
            {
                audio.SetVolume(volumeSlider.value);
                audio.SetMuted(muteToggle.isOn);
            }

            if (save != null)
            {
                save.SetInt(Core.GameConfig.KeyQuality, qualityDropdown.value);
                save.SetInt(Core.GameConfig.KeyFullscreen, fullscreenToggle.isOn ? 1 : 0);
                save.Flush();
            }
        }

        // ---------------- Panel animation ----------------

        private void ShowPanel(GameObject panel)
        {
            if (panel == null || panel.activeSelf) return;
            panel.SetActive(true);
            if (panelAnims.TryGetValue(panel, out var c) && c != null) StopCoroutine(c);
            panelAnims[panel] = StartCoroutine(ScaleIn(panel.transform as RectTransform));
        }

        private void HidePanel(GameObject panel)
        {
            if (panel == null) return;
            panel.SetActive(false);
        }

        private static System.Collections.IEnumerator ScaleIn(RectTransform rt)
        {
            float t = 0;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime * 6f;
                float s = Mathf.SmoothStep(0.7f, 1f, t);
                rt.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            rt.localScale = Vector3.one;
        }

        // ---------------- Widget factories ----------------

        private GameObject MakePanel(string name, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(canvas.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            go.GetComponent<Image>().color = color;
            return go;
        }

        private Text MakeText(RectTransform parent, string content, int size, TextAnchor anchor, Vector2 pos)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(480f, size + 14f);
            var t = go.GetComponent<Text>();
            t.text = content;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (t.font == null) t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            t.fontSize = size;
            t.alignment = anchor;
            t.color = Color.white;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        private GameObject MakeButton(RectTransform parent, string label, Vector2 size, UnityEngine.Events.UnityAction action)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            var img = go.GetComponent<Image>();
            img.color = new Color(0.18f, 0.24f, 0.32f);
            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.25f, 0.33f, 0.44f);
            colors.pressedColor = new Color(0.12f, 0.16f, 0.22f);
            colors.fadeDuration = 0.06f;
            btn.colors = colors;
            btn.onClick.AddListener(action);
            var txt = MakeText(rt, label, 19, TextAnchor.MiddleCenter, Vector2.zero);
            txt.rectTransform.anchorMin = Vector2.zero;
            txt.rectTransform.anchorMax = Vector2.one;
            txt.rectTransform.offsetMin = txt.rectTransform.offsetMax = Vector2.zero;
            return go;
        }

        private Dropdown MakeDropdown(RectTransform parent, Vector2 pos)
        {
            var go = new GameObject("Dropdown", typeof(RectTransform), typeof(Image), typeof(Dropdown));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(300f, 34f);
            go.GetComponent<Image>().color = new Color(0.18f, 0.24f, 0.32f);
            var dd = go.GetComponent<Dropdown>();
            var caption = MakeText(rt, "", 16, TextAnchor.MiddleLeft, Vector2.zero);
            caption.rectTransform.anchorMin = Vector2.zero;
            caption.rectTransform.anchorMax = Vector2.one;
            caption.rectTransform.offsetMin = new Vector2(12f, 0f);
            caption.rectTransform.offsetMax = new Vector2(-30f, 0f);
            dd.captionText = caption;
            dd.options = new List<Dropdown.OptionData>();

            var template = new GameObject("Template", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            template.transform.SetParent(rt, false);
            var trt = template.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0f, 0f);
            trt.anchorMax = new Vector2(1f, 0f);
            trt.pivot = new Vector2(0.5f, 1f);
            trt.sizeDelta = new Vector2(0f, 150f);
            template.GetComponent<Image>().color = new Color(0.10f, 0.13f, 0.18f);
            dd.template = trt;

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(trt, false);
            var crt = content.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0f, 1f);
            crt.anchorMax = new Vector2(1f, 1f);
            crt.pivot = new Vector2(0.5f, 1f);
            template.GetComponent<ScrollRect>().content = crt;

            var item = new GameObject("Item", typeof(RectTransform), typeof(Toggle));
            item.transform.SetParent(crt, false);
            var irt = item.GetComponent<RectTransform>();
            irt.anchorMin = new Vector2(0f, 0.5f);
            irt.anchorMax = new Vector2(1f, 0.5f);
            irt.sizeDelta = new Vector2(0f, 30f);
            var itemText = MakeText(irt, "", 16, TextAnchor.MiddleLeft, Vector2.zero);
            itemText.rectTransform.anchorMin = Vector2.zero;
            itemText.rectTransform.anchorMax = Vector2.one;
            itemText.rectTransform.offsetMin = new Vector2(14f, 0f);
            dd.itemText = itemText;
            return dd;
        }

        private Toggle MakeToggle(RectTransform parent, Vector2 pos)
        {
            var go = new GameObject("Toggle", typeof(RectTransform), typeof(Toggle));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(30f, 30f);
            var t = go.GetComponent<Toggle>();
            var bg = new GameObject("Bg", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(rt, false);
            bg.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f);
            var brt = bg.GetComponent<RectTransform>();
            brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one;
            brt.offsetMin = brt.offsetMax = Vector2.zero;
            t.targetGraphic = bg.GetComponent<Image>();
            var check = new GameObject("Check", typeof(RectTransform), typeof(Image));
            check.transform.SetParent(bg.transform, false);
            check.GetComponent<Image>().color = new Color(0.3f, 0.8f, 1f);
            var crt = check.GetComponent<RectTransform>();
            crt.anchorMin = Vector2.zero; crt.anchorMax = Vector2.one;
            crt.offsetMin = crt.offsetMax = new Vector2(4f, 4f);
            t.graphic = check.GetComponent<Image>();
            return t;
        }

        private Slider MakeSlider(RectTransform parent, Vector2 pos)
        {
            var go = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(300f, 20f);
            var s = go.GetComponent<Slider>();
            var bg = new GameObject("Bg", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(rt, false);
            bg.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f);
            var brt = bg.GetComponent<RectTransform>();
            brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one;
            brt.offsetMin = brt.offsetMax = Vector2.zero;
            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(rt, false);
            fill.GetComponent<Image>().color = new Color(0.2f, 0.6f, 1f);
            var frt = fill.GetComponent<RectTransform>();
            frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one;
            frt.offsetMin = frt.offsetMax = Vector2.zero;
            s.fillRect = frt;
            s.targetGraphic = fill.GetComponent<Image>();
            s.minValue = 0f; s.maxValue = 1f;
            return s;
        }
    }
}
