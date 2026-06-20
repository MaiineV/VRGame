using System.Collections.Generic;
using Gameplay.Interactions;
using Services;
using Services.Night;
using UnityEngine;
using Utilities;

namespace Gameplay.Systems
{
    /// <summary>
    /// Owns the bar's bottles across nights. On start it records each scene bottle's origin (prefab +
    /// world pose + parent). When a night ends (or is aborted) it destroys every bottle — including
    /// broken ones, which <see cref="Breakable"/> only deactivates — and recreates the whole set fresh
    /// at their origins. Whatever bottles you place in the scene (e.g. a backup set) are picked up
    /// automatically; this component doesn't create duplicates itself.
    ///
    /// Bottles aren't pooled (they're scene-placed), so each bottle prefab must reference itself via
    /// <see cref="Bottle.SourcePrefab"/> for recreation to work.
    /// </summary>
    public sealed class BottleRespawner : MonoBehaviour
    {
        private struct Origin
        {
            public GameObject Prefab;
            public Vector3 Position;
            public Quaternion Rotation;
            public Transform Parent;
        }

        private readonly List<Origin> _origins = new();
        private INightService _night;
        private bool _initialized;

        void Start()
        {
            CaptureOrigins();

            if (ServiceLocator.TryGet<INightService>(out _night))
                _night.NightEnded += RespawnAll;
            else
                MyLogger.LogWarning("[BottleRespawner] INightService not found; bottles won't respawn on night end.");
        }

        void OnDestroy()
        {
            if (_night != null) _night.NightEnded -= RespawnAll;
        }

        private void CaptureOrigins()
        {
            if (_initialized) return;
            _initialized = true;

            var bottles = Object.FindObjectsByType<Bottle>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < bottles.Length; i++)
            {
                var b = bottles[i];
                if (b == null || b.SourcePrefab == null)
                {
                    if (b != null) MyLogger.LogWarning($"[BottleRespawner] '{b.name}' has no SourcePrefab; skipping (won't respawn).");
                    continue;
                }

                var t = b.transform;
                _origins.Add(new Origin { Prefab = b.SourcePrefab, Position = t.position, Rotation = t.rotation, Parent = t.parent });
            }

            MyLogger.LogInfo($"[BottleRespawner] Tracking {_origins.Count} bottle origin(s).");
        }

        private void RespawnAll()
        {
            // Destroy every current bottle (active or broken/inactive) before recreating the full set.
            var live = Object.FindObjectsByType<Bottle>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < live.Length; i++)
                if (live[i] != null) Destroy(live[i].gameObject);

            for (int i = 0; i < _origins.Count; i++)
                Spawn(_origins[i]);

            MyLogger.LogInfo($"[BottleRespawner] Respawned {_origins.Count} bottle(s) at origin.");
        }

        private static void Spawn(Origin o)
        {
            if (o.Prefab == null) return;
            Object.Instantiate(o.Prefab, o.Position, o.Rotation, o.Parent);
        }
    }
}
