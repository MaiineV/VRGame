using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Utilities;

namespace Gameplay.Systems
{
    /// <summary>
    /// Forces the GPU to compile the shader variants the gameplay materials need, ONCE, on the first
    /// frame of the Bar scene — while the atmosphere cover is still holding the screen black (and the
    /// Quest is in its 45s launch boost, so clocks are maxed). Without this, each variant compiles the
    /// first time its material is actually drawn (first customer, first pour, first break), causing the
    /// hitches that — together with the NavMesh bake — produced the ~40 FPS startup reading.
    ///
    /// Why a CommandBuffer instead of instantiating the prefabs: drawing a material directly compiles its
    /// variant for the REAL device graphics API (Vulkan/Mobile URP), which an editor-recorded
    /// ShaderVariantCollection cannot do. And it runs no Awake/Start gameplay logic — instantiating a
    /// Customer prefab cold (no seat, no navmesh) would throw. We only read the prefab assets'
    /// sharedMaterials; nothing is spawned.
    ///
    /// Static scene materials already warm naturally during the cover (the scene renders behind the
    /// black). This component fills the gap: the pooled/dynamic prefabs that aren't on screen yet.
    /// </summary>
    public sealed class ShaderWarmup : MonoBehaviour
    {
        [Tooltip("Dynamic / pooled prefabs whose materials aren't on screen during the load cover " +
                 "(customers, glass, breakables, bottles). Their shared materials are warmed without " +
                 "instantiating the prefab.")]
        [SerializeField] private GameObject[] _prefabs;

        [Tooltip("Any extra materials to warm that aren't reachable from the prefabs above " +
                 "(e.g. a material built/assigned at runtime).")]
        [SerializeField] private Material[] _materials;

        void Start()
        {
            var unique = new HashSet<Material>();
            var buffer = new List<Material>(8);

            if (_prefabs != null)
            {
                foreach (var prefab in _prefabs)
                {
                    if (prefab == null) continue;
                    foreach (var r in prefab.GetComponentsInChildren<Renderer>(true))
                    {
                        if (r == null) continue;
                        r.GetSharedMaterials(buffer);
                        for (int i = 0; i < buffer.Count; i++)
                            if (buffer[i] != null) unique.Add(buffer[i]);
                    }
                }
            }

            if (_materials != null)
                foreach (var m in _materials)
                    if (m != null) unique.Add(m);

            if (unique.Count == 0) return;

            // A single off-screen unit quad is enough to force each (material, pass) variant to compile;
            // the geometry never shows (it's behind the black cover and positioned far away anyway).
            var quad = BuildQuad();
            var cb = new CommandBuffer { name = "ShaderWarmup" };
            var offscreen = Matrix4x4.TRS(new Vector3(0f, -10000f, 0f), Quaternion.identity, Vector3.one);

            int variants = 0;
            foreach (var mat in unique)
            {
                int passes = Mathf.Max(1, mat.passCount);
                for (int p = 0; p < passes; p++)
                {
                    cb.DrawMesh(quad, offscreen, mat, 0, p);
                    variants++;
                }
            }

            Graphics.ExecuteCommandBuffer(cb);
            cb.Release();

            MyLogger.LogInfo($"[ShaderWarmup] Warmed {unique.Count} material(s), {variants} pass variant(s).");
        }

        private static Mesh BuildQuad()
        {
            var mesh = new Mesh { name = "ShaderWarmupQuad" };
            mesh.vertices = new[]
            {
                new Vector3(0f, 0f, 0f), new Vector3(0f, 0.001f, 0f),
                new Vector3(0.001f, 0.001f, 0f), new Vector3(0.001f, 0f, 0f)
            };
            mesh.uv = new[] { Vector2.zero, Vector2.up, Vector2.one, Vector2.right };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            return mesh;
        }
    }
}
