using UnityEngine;

namespace Utilities
{
    /// <summary>
    /// Helpers for keeping a transform's visible size stable across reparenting.
    /// </summary>
    public static class TransformScaleUtil
    {
        /// <summary>
        /// Re-imposes a fixed WORLD (lossy) scale on <paramref name="t"/> after a reparent, so moving an
        /// object between parents of different scale never changes its visible size. Idempotent — calling
        /// it with the same target every time does not accumulate drift (unlike relying on Unity's
        /// <c>SetParent(worldPositionStays:true)</c>, which approximates localScale under non-uniform
        /// parent scale and does not round-trip cleanly).
        /// </summary>
        public static void SetWorldScale(Transform t, Vector3 worldScale)
        {
            if (t == null) return;
            Vector3 p = t.parent != null ? t.parent.lossyScale : Vector3.one;
            t.localScale = new Vector3(
                p.x != 0f ? worldScale.x / p.x : worldScale.x,
                p.y != 0f ? worldScale.y / p.y : worldScale.y,
                p.z != 0f ? worldScale.z / p.z : worldScale.z);
        }
    }
}
