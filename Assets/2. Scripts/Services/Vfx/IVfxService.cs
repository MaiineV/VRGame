using Data.Enums;
using UnityEngine;

namespace Services.Vfx
{
    /// <summary>
    /// Fire-and-forget particle bursts keyed by VfxId. Systems are pre-built and pooled (zero
    /// runtime Instantiate), so callers just say "burst here, this color".
    /// </summary>
    public interface IVfxService : IGameService
    {
        /// <summary>
        /// Emit a burst at a world position, tinted with <paramref name="tint"/>.
        /// <paramref name="count"/> 0 uses the effect's default particle count.
        /// </summary>
        void PlayBurst(VfxId id, Vector3 position, Color tint, int count = 0);
    }
}
