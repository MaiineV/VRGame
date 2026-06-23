using Data.SO;
using Gameplay.Interactions;
using UI.Diegetic;
using UnityEngine;

namespace Gameplay.Systems
{
    /// <summary>
    /// A fixed shop position on a shelf. Its single <see cref="BottleSO"/> field drives EVERYTHING about
    /// the bottle that lives here: the visual (<see cref="BottleSO.Prefab"/>), the price tag
    /// (<see cref="BottleSO.UnlockCost"/>), the per-slot ownership identity (a stable slot id), and the
    /// recipe link (by ingredient). To swap or add a bottle you change ONLY this field — nothing else is
    /// wired by hand.
    ///
    /// The bottle is instantiated ONCE from the SO's prefab (data-driven, no duplicated scene objects) and
    /// then reused for the whole session: <see cref="ResetInPlace"/> snaps it home between nights instead
    /// of destroying/recreating it, so its id, tag binding and event subscriptions never churn.
    /// </summary>
    public sealed class ShelfSlot : MonoBehaviour
    {
        [Tooltip("The bottle that lives in this slot. Drives visual, price, ownership and recipe link. " +
                 "Swap this single field to change the bottle — no other wiring needed.")]
        [SerializeField] private BottleSO _bottle;

        [Tooltip("Optional child price tag. If null, the first BottlePriceTag in children is used. " +
                 "Bound directly to this slot's bottle (no nearest-bottle search).")]
        [SerializeField] private BottlePriceTag _priceTag;

        private Bottle _instance;
        private int _slotId;

        public BottleSO BottleSO => _bottle;
        public int SlotId => _slotId;
        public Bottle Instance => _instance;

        /// <summary>Spawn (first time) or reset the bottle for this slot and bind its price tag. Called by
        /// <see cref="ShopShelf"/> with a stable, unique slot id used as the per-bottle ownership id.</summary>
        public void Configure(int slotId)
        {
            _slotId = slotId;
            if (_priceTag == null) _priceTag = GetComponentInChildren<BottlePriceTag>(true);

            if (_bottle == null || _bottle.Prefab == null)
            {
                // Empty slot: nothing to show. Hide any leftover tag so it doesn't read against no bottle.
                if (_priceTag != null) _priceTag.gameObject.SetActive(false);
                return;
            }

            if (_instance == null) SpawnInstance();
            else ResetInPlace();

            if (_priceTag != null)
            {
                _priceTag.gameObject.SetActive(true);
                _priceTag.Bind(_instance, _bottle);
            }
        }

        /// <summary>Snap the bottle back to the slot, zero its motion, and re-fill it. Never destroys the
        /// object — the same instance is reused so ownership/tag/subscriptions stay valid across nights.</summary>
        public void ResetInPlace()
        {
            if (_instance == null)
            {
                if (_bottle != null && _bottle.Prefab != null) SpawnInstance();
                return;
            }

            var t = _instance.transform;
            t.SetParent(transform, false);
            t.SetPositionAndRotation(transform.position, transform.rotation);

            var body = _instance.Body;
            if (body != null)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

            // SetSO resets the fill flag so the bottle refills from the SO; re-stamp the id in case the
            // prefab carried a different one.
            _instance.SetSO(_bottle);
            _instance.SetInstanceId(_slotId);
            if (!_instance.gameObject.activeSelf) _instance.gameObject.SetActive(true);
        }

        private void SpawnInstance()
        {
            var go = Object.Instantiate(_bottle.Prefab, transform.position, transform.rotation, transform);
            go.name = _bottle.Prefab.name; // drop the "(Clone)" suffix for readable hierarchies
            _instance = go.GetComponent<Bottle>();
            if (_instance == null)
            {
                Utilities.MyLogger.LogWarning($"[ShelfSlot:{name}] Prefab '{_bottle.Prefab.name}' has no Bottle component.");
                return;
            }
            _instance.SetSO(_bottle);
            _instance.SetInstanceId(_slotId);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = _bottle != null ? new Color(0.2f, 0.85f, 1f, 0.7f) : new Color(1f, 0.4f, 0.2f, 0.7f);
            Gizmos.DrawWireCube(transform.position + Vector3.up * 0.15f, new Vector3(0.09f, 0.3f, 0.09f));
        }
    }
}
