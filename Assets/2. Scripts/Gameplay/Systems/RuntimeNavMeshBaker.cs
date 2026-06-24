using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Utilities;

namespace Gameplay.Systems
{
    /// <summary>
    /// Bakes a NavMesh at runtime from the scene's physics colliders, so NPCs can path around the bar,
    /// shelves and furniture instead of walking through them — with NO editor bake step required (the
    /// Quest build just bakes on Start). Uses the low-level <see cref="NavMeshBuilder"/> API so the agent
    /// radius/height are set in code (the small bar + 0.6-scaled customers need a smaller radius than the
    /// default Humanoid agent type). Put this on an empty GameObject at the world origin.
    /// </summary>
    public sealed class RuntimeNavMeshBaker : MonoBehaviour
    {
        [Tooltip("Agent radius used to erode the walkable area from walls/props. Smaller = NPCs fit through " +
                 "tighter gaps. Match the NavMeshAgent radius on the customer prefabs.")]
        [SerializeField] private float _agentRadius = 0.2f;
        [SerializeField] private float _agentHeight = 1.2f;
        [Tooltip("Max step height the NPC can climb.")]
        [SerializeField] private float _agentClimb = 0.4f;
        [Tooltip("Max walkable slope in degrees.")]
        [SerializeField] private float _agentSlope = 45f;
        [Tooltip("World-space box (centred on this object) scanned for geometry and covered by the mesh.")]
        [SerializeField] private Vector3 _bounds = new Vector3(40f, 12f, 40f);
        [Tooltip("Layers whose colliders count as walkable ground / obstacles.")]
        [SerializeField] private LayerMask _layers = ~0;

        private NavMeshDataInstance _instance;
        private NavMeshData _data;
        private readonly List<NavMeshBuildSource> _sources = new();
        private readonly List<NavMeshBuildMarkup> _markups = new();

        void Start() => Bake();

        /// <summary>
        /// Rebuild the navmesh from the current scene geometry. Safe to call again (re-bakes in place).
        /// The build runs on a worker thread via <see cref="NavMeshBuilder.UpdateNavMeshDataAsync"/> so it
        /// never stalls the main thread in one giant frame — that synchronous spike landed on the very
        /// first frame of the Bar scene (right when the FPS meter starts counting) and was the biggest
        /// contributor to the ~40 FPS startup reading. NPCs only need the mesh at night start, long after
        /// this async bake (a fraction of a second) has completed.
        /// </summary>
        public void Bake()
        {
            var settings = NavMesh.GetSettingsByID(0); // default (Humanoid) agent type as the base
            settings.agentRadius = _agentRadius;
            settings.agentHeight = _agentHeight;
            settings.agentClimb = _agentClimb;
            settings.agentSlope = _agentSlope;

            var worldBounds = new Bounds(transform.position, _bounds);
            _sources.Clear();
            NavMeshBuilder.CollectSources(worldBounds, _layers, NavMeshCollectGeometry.PhysicsColliders, 0, _markups, _sources);

            // UpdateNavMeshDataAsync expects bounds in the local space of (position, rotation).
            var localBounds = new Bounds(Vector3.zero, _bounds);

            // Register an (initially empty) NavMeshData once, then fill it asynchronously. Re-bakes reuse
            // the same data instance so agents already bound to it keep their reference.
            if (_data == null)
            {
                _data = new NavMeshData(settings.agentTypeID);
                _instance = NavMesh.AddNavMeshData(_data, transform.position, Quaternion.identity);
            }

            var op = NavMeshBuilder.UpdateNavMeshDataAsync(_data, settings, _sources, localBounds);
            op.completed += _ => MyLogger.LogInfo(
                $"[RuntimeNavMeshBaker] Baked navmesh from {_sources.Count} source(s) (radius={_agentRadius}, valid={_instance.valid}).");
        }

        void OnDestroy()
        {
            if (_instance.valid) NavMesh.RemoveNavMeshData(_instance);
        }
    }
}
