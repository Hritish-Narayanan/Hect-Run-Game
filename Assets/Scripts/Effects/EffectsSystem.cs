using UnityEngine;

namespace SubwaySurfers.Effects
{
    /// <summary>
    /// Pooled one-shot particle bursts (coins, crash debris, powerup sparkles,
    /// shield pop) — all procedural, no texture assets.
    /// </summary>
    public sealed class EffectsSystem : MonoBehaviour
    {
        private ParticleSystem coinFx, crashFx, powerupFx, shieldFx;

        private void Awake()
        {
            Core.Game.Register(this);
            coinFx = MakeBurst("CoinFX", Core.GameConfig.CoinColor, 14, 0.35f, 6f, 0.10f);
            crashFx = MakeBurst("CrashFX", new Color(1f, 0.4f, 0.2f), 26, 0.6f, 9f, 0.16f);
            powerupFx = MakeBurst("PowerupFX", Color.cyan, 18, 0.45f, 7f, 0.12f);
            shieldFx = MakeBurst("ShieldFX", new Color(0.4f, 1f, 0.6f), 20, 0.5f, 8f, 0.13f);
        }

        private ParticleSystem MakeBurst(string name, Color color, int count, float life, float speed, float size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.startLifetime = life;
            main.startSpeed = speed;
            main.startSize = size;
            main.startColor = color;
            main.maxParticles = 64;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0.4f;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.3f;

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            Shader shader = null;
            var dummyMat = Resources.Load<Material>("URPParticlesUnlitDummy");
            if (dummyMat != null && dummyMat.shader != null)
            {
                shader = dummyMat.shader;
            }
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Hidden/Internal-Colored");
            renderer.material = new Material(shader);
            renderer.material.color = color;

            return ps;
        }

        public void CoinPickup(Vector3 pos) => Play(coinFx, pos);
        public void CrashBurst(Vector3 pos) => Play(crashFx, pos);
        public void PowerupBurst(Vector3 pos, Color c)
        {
            var main = powerupFx.main;
            main.startColor = c;
            Play(powerupFx, pos);
        }
        public void ShieldPop(Vector3 pos) => Play(shieldFx, pos);
        public void CrashShake() => Core.Game.Get<Presentation.CameraController>()?.CrashShake();

        public void ResetRun()
        {
            coinFx?.Clear(); crashFx?.Clear(); powerupFx?.Clear(); shieldFx?.Clear();
        }

        private static void Play(ParticleSystem ps, Vector3 pos)
        {
            if (ps == null) return;
            ps.transform.position = pos;
            ps.Play();
        }
    }
}
