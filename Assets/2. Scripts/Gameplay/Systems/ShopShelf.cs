using System.Collections.Generic;
using Services;
using Services.Night;
using UnityEngine;
using Utilities;

namespace Gameplay.Systems
{
    /// <summary>
    /// Owns the shop's <see cref="ShelfSlot"/>s. On start it gives each slot a stable, unique id and tells
    /// it to spawn its bottle from the slot's <see cref="Data.SO.BottleSO"/>. When a night ends it resets
    /// every slot IN PLACE — bottles are never destroyed or recreated, so per-slot ownership ids, price-tag
    /// bindings and event subscriptions survive intact across nights.
    ///
    /// Replaces the old BottleRespawner (which destroyed and re-instantiated every bottle each night and
    /// tracked ownership by a hand-authored instance id — the source of the "all bought / can't buy /
    /// half the tags" shop bugs).
    /// </summary>
    public sealed class ShopShelf : MonoBehaviour
    {
        [Tooltip("Slots managed by this shelf. Leave empty to auto-collect all ShelfSlot children in " +
                 "hierarchy order (their order sets the stable ownership ids).")]
        [SerializeField] private List<ShelfSlot> _slots = new();

        private INightService _night;

        void Start()
        {
            if (_slots == null || _slots.Count == 0)
                _slots = new List<ShelfSlot>(GetComponentsInChildren<ShelfSlot>(true));

            for (int i = 0; i < _slots.Count; i++)
                if (_slots[i] != null) _slots[i].Configure(StableId(i));

            BindNight();
            MyLogger.LogInfo($"[ShopShelf] Configured {_slots.Count} slot(s).");
        }

        // Keep retrying the night-service bind until it resolves. ShopShelf.Start can run before the
        // services are registered (bottles spawn during scene load, ahead of GameBootstrap finishing) — a
        // one-shot bind in Start would silently miss INightService and the bottles would never reset
        // between nights. Once bound the poll stops doing any work.
        void Update()
        {
            if (_night == null) BindNight();
        }

        private void BindNight()
        {
            if (_night != null) return;
            if (ServiceLocator.TryGet<INightService>(out _night))
                _night.NightEnded += ResetAll;
        }

        void OnDestroy()
        {
            if (_night != null) _night.NightEnded -= ResetAll;
        }

        // Stable per-slot ownership id derived from slot order. Offset off 0 so it never collides with the
        // "free/none" sentinel (0). Stable across sessions as long as slot order is unchanged; reordering
        // slots in the editor remaps ownership, which is the expected behaviour for a content edit.
        private static int StableId(int index) => 1001 + index;

        private void ResetAll()
        {
            int n = 0;
            for (int i = 0; i < _slots.Count; i++)
                if (_slots[i] != null) { _slots[i].ResetInPlace(); n++; }
            MyLogger.LogInfo($"[ShopShelf] Reset {n} slot(s) in place.");
        }
    }
}
