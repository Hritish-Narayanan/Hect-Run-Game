using System.Collections.Generic;
using UnityEngine;

namespace SubwaySurfers.Audio
{
    /// <summary>
    /// Fully procedural audio: every SFX is synthesized at boot (no audio files),
    /// plus a tiny generative bass/arpeggio music loop. Volume/mute persist.
    /// </summary>
    public sealed class AudioSystem : MonoBehaviour
    {
        private AudioSource sfx, music;
        private AudioClip jump, land, whoosh, coin, crash, powerup, shieldBreak, roll;

        private float volume = 1f;
        private bool muted;

        private void Awake()
        {
            Core.Game.Register(this);

            sfx = gameObject.AddComponent<AudioSource>();
            sfx.playOnAwake = false;
            sfx.spatialBlend = 0f;

            music = gameObject.AddComponent<AudioSource>();
            music.playOnAwake = false;
            music.loop = true;
            music.volume = 0.35f;

            BuildClips();
            LoadPrefs();
            music.clip = BuildMusicLoop();
            music.Play();
        }

        private void LoadPrefs()
        {
            var save = Core.Game.Get<Core.SaveSystem>();
            volume = save != null ? save.GetFloat(Core.GameConfig.KeyVolume, 1f) : 1f;
            muted = save != null && save.GetInt(Core.GameConfig.KeyMuted, 0) == 1;
            ApplyVolume();
        }

        public float Volume => volume;
        public bool Muted => muted;

        public void SetVolume(float v)
        {
            volume = Mathf.Clamp01(v);
            Core.Game.Get<Core.SaveSystem>()?.SetFloat(Core.GameConfig.KeyVolume, volume);
            ApplyVolume();
        }

        public void SetMuted(bool m)
        {
            muted = m;
            Core.Game.Get<Core.SaveSystem>()?.SetInt(Core.GameConfig.KeyMuted, m ? 1 : 0);
            ApplyVolume();
        }

        private void ApplyVolume()
        {
            float v = muted ? 0f : volume;
            AudioListener.volume = 1f;
            sfx.volume = v;
            music.volume = v * 0.35f;
        }

        // ---- SFX synth ----

        private void BuildClips()
        {
            jump = Tone(520f, 880f, 0.18f, "sine", 0.4f);
            land = NoiseBurst(0.12f, 800f, 0.3f);
            whoosh = NoiseSweep(0.14f, 4000f, 500f, 0.25f);
            coin = Arpeggio(new[] { 1318f, 1760f }, 0.07f, "square", 0.3f);
            crash = NoiseBurst(0.5f, 300f, 0.8f);
            powerup = Arpeggio(new[] { 523f, 659f, 784f, 1046f }, 0.06f, "triangle", 0.4f);
            shieldBreak = NoiseSweep(0.3f, 3000f, 200f, 0.5f);
            roll = NoiseSweep(0.2f, 900f, 250f, 0.3f);
        }

        public void PlayJump() => sfx.PlayOneShot(jump);
        public void PlayLand() => sfx.PlayOneShot(land);
        public void PlayWhoosh() => sfx.PlayOneShot(whoosh);
        public void PlayCoin() => sfx.PlayOneShot(coin);
        public void PlayCrash() => sfx.PlayOneShot(crash);
        public void PlayPowerup() => sfx.PlayOneShot(powerup);
        public void PlayShieldBreak() => sfx.PlayOneShot(shieldBreak);
        public void PlayRoll() => sfx.PlayOneShot(roll);

        // ---- Minimal synth helpers (mono 44.1kHz) ----

        private static AudioClip Tone(float f0, float f1, float dur, string wave, float gain)
        {
            int n = Mathf.CeilToInt(44100 * dur);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / 44100f;
                float env = 1f - t / dur;
                float f = Mathf.Lerp(f0, f1, t / dur);
                float ph = 2f * Mathf.PI * f * t;
                float s = wave == "square" ? Mathf.Sign(Mathf.Sin(ph)) : Mathf.Sin(ph);
                data[i] = s * env * gain;
            }
            return Make(data);
        }

        private static AudioClip Arpeggio(float[] freqs, float step, string wave, float gain)
        {
            float dur = freqs.Length * step + 0.15f;
            int n = Mathf.CeilToInt(44100 * dur);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / 44100f;
                int idx = Mathf.Min(freqs.Length - 1, Mathf.FloorToInt(t / step));
                float local = t - idx * step;
                float env = Mathf.Clamp01(1f - local / (step + 0.15f));
                float ph = 2f * Mathf.PI * freqs[idx] * local;
                float s = wave == "square" ? Mathf.Sign(Mathf.Sin(ph)) * 0.5f : Mathf.Sin(ph);
                data[i] = s * env * gain;
            }
            return Make(data);
        }

        private static AudioClip NoiseBurst(float dur, float cutoff, float gain)
        {
            int n = Mathf.CeilToInt(44100 * dur);
            var data = new float[n];
            var rng = new System.Random();
            float last = 0f;
            float rc = 1f / (2f * Mathf.PI * cutoff);
            float dt = 1f / 44100f;
            float a = dt / (rc + dt);
            for (int i = 0; i < n; i++)
            {
                float t = i * dt;
                float env = 1f - t / dur;
                float white = (float)(rng.NextDouble() * 2.0 - 1.0);
                last += a * (white - last); // one-pole lowpass
                data[i] = last * env * gain * 3f;
            }
            return Make(data);
        }

        private static AudioClip NoiseSweep(float dur, float f0, float f1, float gain)
        {
            int n = Mathf.CeilToInt(44100 * dur);
            var data = new float[n];
            var rng = new System.Random();
            float last = 0f;
            float dt = 1f / 44100f;
            for (int i = 0; i < n; i++)
            {
                float t = i * dt;
                float env = 1f - t / dur;
                float cutoff = Mathf.Lerp(f0, f1, t / dur);
                float rc = 1f / (2f * Mathf.PI * cutoff);
                float a = dt / (rc + dt);
                float white = (float)(rng.NextDouble() * 2.0 - 1.0);
                last += a * (white - last);
                data[i] = last * env * gain * 3f;
            }
            return Make(data);
        }

        private static AudioClip Make(float[] data)
        {
            var clip = AudioClip.Create("sfx", data.Length, 1, 44100, false);
            clip.SetData(data, 0);
            return clip;
        }

        // ---- Generative music: 2-bar loop, bass + arpeggio + hats ----

        private static AudioClip BuildMusicLoop()
        {
            const int bpm = 128;
            const int bars = 2;
            const int stepsPerBar = 8;
            float stepDur = 60f / bpm / 2f; // 8th notes
            float dur = bars * stepsPerBar * stepDur;
            int n = Mathf.CeilToInt(44100 * dur);
            var data = new float[n];

            // Chord roots: Am F C G over 2 bars
            float[] bassNotes = { 55f, 55f, 43.65f, 43.65f, 65.41f, 65.41f, 49f, 49f }; // A1 A1 F1 F1 C2 C2 G1 G1
            float[] arpNotes = { 220f, 261.63f, 329.63f, 440f, 523.25f, 659.25f, 880f, 1046.5f };
            var rng = new System.Random(42);

            for (int i = 0; i < n; i++)
            {
                float t = i / 44100f;
                int step = Mathf.FloorToInt(t / stepDur) % (bars * stepsPerBar);
                float local = t - Mathf.FloorToInt(t / stepDur) * stepDur;

                float sample = 0f;

                // Bass on every step
                {
                    float f = bassNotes[step % bassNotes.Length];
                    float env = Mathf.Clamp01(1f - local / stepDur);
                    sample += Mathf.Sin(2f * Mathf.PI * f * local) * env * 0.35f;
                    sample += Mathf.Sin(2f * Mathf.PI * f * 2f * local) * env * 0.10f; // octave
                }

                // Arp on off-beats
                if (step % 2 == 1)
                {
                    float f = arpNotes[(step * 3 + step / 4) % arpNotes.Length];
                    float env = Mathf.Clamp01(1f - local / (stepDur * 0.8f));
                    sample += Mathf.Sin(2f * Mathf.PI * f * local) * env * 0.08f;
                }

                // Hi-hat every step
                {
                    float env = Mathf.Clamp01(1f - local / 0.03f);
                    float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                    sample += noise * env * (step % 2 == 0 ? 0.04f : 0.02f);
                }

                data[i] = Mathf.Clamp(sample, -0.9f, 0.9f);
            }
            return Make(data);
        }
    }
}
