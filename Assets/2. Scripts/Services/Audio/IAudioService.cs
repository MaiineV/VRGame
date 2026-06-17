using Data.Enums;
using UnityEngine;

namespace Services.Audio
{
    /// <summary>
    /// Pooled SFX playback keyed by SfxId. Zero runtime Instantiate.
    /// Loops return an int handle (generation-tagged) so callers can safely Stop
    /// without worrying about pool rotation invalidating their slot.
    /// </summary>
    public interface IAudioService : IGameService
    {
        /// <summary>Fire-and-forget spatialized one-shot at world position.</summary>
        void PlayOneShot(SfxId id, Vector3 position, float volumeScale = 1f);

        /// <summary>Fire-and-forget 2D one-shot (for UI-style feedback).</summary>
        void PlayOneShot2D(SfxId id, float volumeScale = 1f);

        /// <summary>Reserve a source, attach to <paramref name="attachTo"/> (nullable for static world pos),
        /// play the clip as a loop. Returns a handle, or 0 on failure.</summary>
        int StartLoop(SfxId id, Transform attachTo, Vector3 fallbackWorldPos, float volumeScale = 1f);

        /// <summary>Release a looped source back to the pool. Safe to call with a stale handle.</summary>
        void StopLoop(int handle);

        /// <summary>Update volume scale for an active loop (no effect on stale handle).</summary>
        void SetLoopVolume(int handle, float volumeScale);
    }
}
