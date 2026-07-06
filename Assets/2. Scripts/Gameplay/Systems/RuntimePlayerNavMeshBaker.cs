using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Utilities;

namespace Gameplay.Systems
{
    /// <summary>
    /// Bakes a second NavMesh at runtime, sized for the VR player instead of the 0.6-scaled customer
    /// NPCs. <see cref="RuntimeNavMeshBaker"/> already bakes under the default ("Humanoid", agentTypeID 0)
    /// agent type tuned to the small NPCs; reusing that mesh for player teleport would let the player land
    /// in gaps only an NPC-sized capsule fits through (right up against counters/shelves). This bakes under
    /// its own agent type instead, consumed by the SDK's Teleport Building Block (NavMesh variant, area
    /// "Walkable") as the player's teleport surface. Put this on an empty GameObject next to "Navigation".
    /// </summary>
    public sealed class RuntimePlayerNavMeshBaker : MonoBehaviour
    {
        [Tooltip("Player capsule radius used to erode the walkable area from walls/props/shelves.")]
        [SerializeField] private float _agentRadius = 0.35f;
        [SerializeField] private float _agentHeight = 1.75f;
        [Tooltip("Max step height the player can teleport up onto.")]
        [SerializeField] private float _agentClimb = 0.4f;
        [Tooltip("Max walkable slope in degrees.")]
        [SerializeField] private float _agentSlope = 45f;
        [Tooltip("World-space box (centred on this object) scanned for geometry and covered by the mesh. " +
                 "Matches RuntimeNavMeshBaker's bounds so both meshes cover the same floor area.")]
        [SerializeField] private Vector3 _bounds = new Vector3(40f, 12f, 40f);
        [Tooltip("Layers whose colliders count as walkable ground / obstacles.")]
        [SerializeField] private LayerMask _layers = ~0;

        private int _agentTypeID = -1;
        private NavMeshDataInstance _instance;
        private NavMeshData _data;
        private readonly List<NavMeshBuildSource> _sources = new();
        private readonly List<NavMeshBuildMarkup> _markups = new();

        /// <summary>agentTypeID of the player-scoped settings this baker created, for wiring into the
        /// installed Teleport NavMesh block's surface (its agent index/type must match this one).</summary>
        public int AgentTypeID => _agentTypeID;

        // Must run in Awake (before RuntimeNavMeshBaker's Start-driven bake and before the Teleport block's
        // NavMeshSurface resolves its agent index): Unity guarantees all Awake()s run before any Start().
        void Awake() => CreatePlayerAgentType();

        void Start() => Bake();

        // Idempotent across repeated Play sessions in the same Editor process: NavMesh.CreateSettings()
        // would otherwise append a fresh entry every time Play is entered (no domain reload in between),
        // shifting the agent index the installed Teleport NavMeshSurface(s) were pointed at. Reuse an
        // existing entry with our exact tuning instead of creating a new one when one is already there.
        private void CreatePlayerAgentType()
        {
            int count = NavMesh.GetSettingsCount();
            for (int i = 0; i < count; i++)
            {
                var existing = NavMesh.GetSettingsByIndex(i);
                if (Mathf.Approximately(existing.agentRadius, _agentRadius) &&
                    Mathf.Approximately(existing.agentHeight, _agentHeight))
                {
                    _agentTypeID = existing.agentTypeID;
                    return;
                }
            }

            NavMesh.CreateSettings();
            var settings = NavMesh.GetSettingsByIndex(NavMesh.GetSettingsCount() - 1);
            settings.agentRadius = _agentRadius;
            settings.agentHeight = _agentHeight;
            settings.agentClimb = _agentClimb;
            settings.agentSlope = _agentSlope;
            _agentTypeID = settings.agentTypeID;
        }

        /// <summary>Rebuild the player navmesh from the current scene geometry. Safe to call again.</summary>
        public void Bake()
        {
            if (_agentTypeID == -1) CreatePlayerAgentType();
            var settings = NavMesh.GetSettingsByID(_agentTypeID);

            var worldBounds = new Bounds(transform.position, _bounds);
            _sources.Clear();
            NavMeshBuilder.CollectSources(worldBounds, _layers, NavMeshCollectGeometry.PhysicsColliders, 0, _markups, _sources);

            var localBounds = new Bounds(Vector3.zero, _bounds);

            if (_data == null)
            {
                _data = new NavMeshData(settings.agentTypeID);
                _instance = NavMesh.AddNavMeshData(_data, transform.position, Quaternion.identity);
            }

            var op = NavMeshBuilder.UpdateNavMeshDataAsync(_data, settings, _sources, localBounds);
            op.completed += _ => MyLogger.LogInfo(
                $"[RuntimePlayerNavMeshBaker] Baked player navmesh from {_sources.Count} source(s) " +
                $"(radius={_agentRadius}, agentTypeID={_agentTypeID}, valid={_instance.valid}).");
        }

        void OnDestroy()
        {
            if (_instance.valid) NavMesh.RemoveNavMeshData(_instance);
        }
    }
}
