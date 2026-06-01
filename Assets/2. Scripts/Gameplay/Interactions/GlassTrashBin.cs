using Data.Enums;
using Gameplay.Systems;
using Services;
using Services.Audio;
using UnityEngine;

namespace Gameplay.Interactions
{
    /// <summary>
    /// Trash bin for unwanted glasses. Drop a glass inside (release it — not while held) and it is
    /// emptied and returned to the pool, freeing a budget slot so the dispenser can spawn another.
    /// This is how the player clears leftovers (a customer who left without their drink, a botched
    /// pour). Requires a trigger Collider sized as the bin's mouth.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class GlassTrashBin : MonoBehaviour
    {
        [Tooltip("Optional SFX when a glass is binned. None = silent.")]
        [SerializeField] private SfxId _trashSfx = SfxId.None;

        void Reset()
        {
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        // OnTriggerStay (not Enter) so we only recycle once the glass is actually released inside,
        // letting the player reach in and pull a glass back out without losing it.
        void OnTriggerStay(Collider other)
        {
            var glass = other.GetComponentInParent<Glass>();
            if (glass == null || !glass.gameObject.activeSelf) return;

            var rb = glass.Body;
            if (rb == null || rb.isKinematic) return; // still held by the grabber

            Recycle(glass);
        }

        private void Recycle(Glass glass)
        {
            glass.Empty();

            if (_trashSfx != SfxId.None && ServiceLocator.TryGet<IAudioService>(out var audio))
                audio.PlayOneShot(_trashSfx, transform.position);

            if (ServiceLocator.TryGet<IGlassPoolService>(out var pool))
                pool.Return(glass);
            else
                Object.Destroy(glass.gameObject);
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.3f);
            var col = GetComponent<Collider>();
            if (col is BoxCollider b) Gizmos.DrawCube(transform.position + b.center, b.size);
        }
#endif
    }
}
