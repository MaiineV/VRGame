using System.Collections.Generic;
using Data.Enums;
using UnityEngine;

namespace Services.Vfx
{
    /// <summary>
    /// Pooled, code-built particle bursts. Mirrors AudioService: a DontDestroyOnLoad host owns a
    /// small ring of pre-configured ParticleSystems per VfxId, so PlayBurst never instantiates at
    /// runtime — it just repositions a free system, tints it, and Emit()s. ParticleSystems are
    /// configured entirely in code (no prefab authoring). One shared Sprites/Default material keeps
    /// it cheap and avoids depending on render-pipeline-specific shaders.
    /// </summary>
    public sealed class VfxService : IVfxService
    {
        private const int PoolPerEffect = 6;

        private struct EffectConfig
        {
            public float lifetime;
            public float speed;
            public float size;
            public float gravity;
            public int count;
            public ParticleSystemShapeType shape;
            public float coneAngle;
            public float radius;
            public bool emitUpward;
        }

        private sealed class EffectPool
        {
            public ParticleSystem[] Systems;
            public int Cursor;
            public int DefaultCount;
        }

        private Transform _root;
        private Material _material;
        private readonly Dictionary<VfxId, EffectPool> _pools = new();

        public void Initialize()
        {
            var host = new GameObject("[VfxService]");
            Object.DontDestroyOnLoad(host);
            _root = host.transform;

            // Sprites/Default is an always-included built-in (editor + player, URP-compatible),
            // vertex-colored and alpha-blended — safe to resolve at runtime.
            var shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            _material = new Material(shader) { name = "VfxParticle" };
            // Soft radial dot so particles read as round droplets/sparkles instead of hard squares.
            _material.mainTexture = BuildSoftParticleTexture();

            Build(VfxId.Splash, new EffectConfig
            {
                lifetime = 0.3f, speed = 0.4f, size = 0.015f, gravity = 1f, count = 5,
                shape = ParticleSystemShapeType.Cone, coneAngle = 25f, radius = 0.01f, emitUpward = true
            });
            Build(VfxId.Shatter, new EffectConfig
            {
                lifetime = 0.5f, speed = 1.2f, size = 0.015f, gravity = 1f, count = 12,
                shape = ParticleSystemShapeType.Sphere, radius = 0.03f
            });
            Build(VfxId.ServeSuccess, new EffectConfig
            {
                lifetime = 0.6f, speed = 0.8f, size = 0.02f, gravity = 0f, count = 14,
                shape = ParticleSystemShapeType.Hemisphere, radius = 0.06f
            });
            Build(VfxId.Coins, new EffectConfig
            {
                lifetime = 0.7f, speed = 1.2f, size = 0.025f, gravity = 2f, count = 8,
                shape = ParticleSystemShapeType.Cone, coneAngle = 20f, radius = 0.02f, emitUpward = true
            });
            // Rejected-serve cue: a brief red puff that drifts down (no upward sparkle) so it reads as a
            // miss at a glance — gives the failure a VISUAL signal, not just audio/haptic.
            Build(VfxId.ServeFail, new EffectConfig
            {
                lifetime = 0.5f, speed = 0.5f, size = 0.02f, gravity = 1f, count = 12,
                shape = ParticleSystemShapeType.Hemisphere, radius = 0.06f
            });
        }

        public void PlayBurst(VfxId id, Vector3 position, Color tint, int count = 0)
        {
            if (_root == null || !_pools.TryGetValue(id, out var pool)) return;

            var ps = pool.Systems[pool.Cursor];
            pool.Cursor = (pool.Cursor + 1) % pool.Systems.Length;

            ps.transform.position = position;
            var main = ps.main;
            main.startColor = tint;
            ps.Emit(count > 0 ? count : pool.DefaultCount);
        }

        private void Build(VfxId id, EffectConfig cfg)
        {
            var systems = new ParticleSystem[PoolPerEffect];
            for (int i = 0; i < PoolPerEffect; i++)
            {
                var go = new GameObject($"vfx_{id}_{i}");
                go.transform.SetParent(_root, false);

                var ps = go.AddComponent<ParticleSystem>();
                // AddComponent spins up a default emitting system — stop & clear before configuring.
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

                var main = ps.main;
                main.duration = 1f;
                // Loop so the system stays perpetually "playing" — with emission disabled it emits
                // nothing on its own, but manual Emit() particles still simulate and render. A
                // non-looping system would auto-stop after `duration` and later Emit()s would be dead.
                main.loop = true;
                main.playOnAwake = false;
                main.startLifetime = cfg.lifetime;
                main.startSpeed = cfg.speed;
                main.startSize = cfg.size;
                main.gravityModifier = cfg.gravity;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.maxParticles = 64;

                var emission = ps.emission;
                emission.enabled = false; // manual Emit() only

                var shape = ps.shape;
                shape.enabled = true;
                shape.shapeType = cfg.shape;
                shape.radius = cfg.radius;
                if (cfg.shape == ParticleSystemShapeType.Cone)
                    shape.angle = cfg.coneAngle;
                // Cone/Hemisphere emit along local +Z; rotate so the burst fires upward (+Y).
                if (cfg.emitUpward)
                    shape.rotation = new Vector3(-90f, 0f, 0f);

                var renderer = ps.GetComponent<ParticleSystemRenderer>();
                renderer.renderMode = ParticleSystemRenderMode.Billboard;
                renderer.material = _material;

                // Run the sim loop so manual Emit() particles age and render; emission stays off.
                ps.Play();

                systems[i] = ps;
            }

            _pools[id] = new EffectPool { Systems = systems, Cursor = 0, DefaultCount = cfg.count };
        }

        // White dot with a soft radial alpha falloff, built in code (no texture asset needed).
        private static Texture2D BuildSoftParticleTexture()
        {
            const int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "VfxSoftDot",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            float center = (size - 1) * 0.5f;
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) / center; // 0 = center, 1 = edge
                    float a = Mathf.Clamp01(1f - d);
                    a *= a; // smoother edge
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            }

            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }
    }
}
