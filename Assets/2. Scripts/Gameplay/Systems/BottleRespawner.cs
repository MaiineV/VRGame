using System.Collections.Generic;
using Data.Enums;
using Gameplay.Interactions;
using Services;
using Services.Night;
using Services.Progression;
using UnityEngine;
using Utilities;

namespace Gameplay.Systems
{
    /// <summary>
    /// Resets the bar's OWNED bottles to their origin between nights. On start it records each scene
    /// bottle's origin (prefab + world pose + parent + ingredient). When a night ends it destroys and
    /// recreates only the bottles whose ingredient the player OWNS — restoring thrown/moved/broken
    /// owned bottles to a clean state. Bottles that are still for sale in the shop are LEFT UNTOUCHED:
    /// recreating a locked bottle was making it read as unlocked, so we simply don't respawn those.
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
            public IngredientId Ingredient;
            public int InstanceId;   // per-bottle ownership id; re-applied to the recreated bottle
            public bool Free;        // UnlockCost <= 0: owned from the start, always respawns
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
                _origins.Add(new Origin
                {
                    Prefab = b.SourcePrefab,
                    Position = t.position,
                    Rotation = t.rotation,
                    Parent = t.parent,
                    Ingredient = IngredientOf(b),
                    InstanceId = b.InstanceId,
                    Free = b.SO == null || b.SO.UnlockCost <= 0,
                });
            }

            MyLogger.LogInfo($"[BottleRespawner] Tracking {_origins.Count} bottle origin(s).");
        }

        private void RespawnAll()
        {
            // Only touch OWNED bottles. For-sale (locked) bottles are left exactly where they are so
            // destroying/recreating them can't flip their lock state.
            ServiceLocator.TryGet<IProgressionService>(out var progression);

            // Destroy only the live bottles the player OWNS (free, or this specific instance bought).
            var live = Object.FindObjectsByType<Bottle>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < live.Length; i++)
            {
                var b = live[i];
                if (b == null) continue;
                bool free = b.SO == null || b.SO.UnlockCost <= 0;
                if (IsOwned(progression, b.InstanceId, free)) Destroy(b.gameObject);
            }

            // Recreate only the owned origins.
            int recreated = 0;
            for (int i = 0; i < _origins.Count; i++)
            {
                if (!IsOwned(progression, _origins[i].InstanceId, _origins[i].Free)) continue;
                Spawn(_origins[i]);
                recreated++;
            }

            MyLogger.LogInfo($"[BottleRespawner] Respawned {recreated} owned bottle(s) at origin (for-sale bottles left untouched).");
        }

        // A free bottle (or one with no progression service) is treated as owned (default-usable), so the
        // reset still works in setups without the shop economy. Otherwise ownership is per physical instance.
        private static bool IsOwned(IProgressionService progression, int instanceId, bool free)
        {
            if (free) return true;
            return progression == null || progression.IsBottleInstanceOwned(instanceId);
        }

        private static IngredientId IngredientOf(Bottle b) =>
            b != null && b.SO != null && b.SO.Ingredient != null ? b.SO.Ingredient.Id : IngredientId.None;

        private static void Spawn(Origin o)
        {
            if (o.Prefab == null) return;
            var go = Object.Instantiate(o.Prefab, o.Position, o.Rotation, o.Parent);
            // The prefab carries instance id 0; re-stamp the scene-assigned id so the recreated bottle
            // keeps its per-instance ownership identity across nights.
            var b = go.GetComponent<Bottle>();
            if (b != null) b.SetInstanceId(o.InstanceId);
        }
    }
}
