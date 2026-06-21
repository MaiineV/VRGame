using UnityEngine;

namespace Services.Haptics
{
    /// <summary>
    /// Controller vibration, timed and pooled per hand. Callers fire-and-forget a pulse;
    /// the service sustains the vibration each frame and stops it when the duration elapses.
    /// Stronger/longer pulses win so a big event isn't cut short by a small overlapping one.
    /// No-op on platforms without Touch controllers (e.g. editor with no headset).
    /// </summary>
    public interface IHapticService : IGameService
    {
        /// <summary>Vibrate one controller. amplitude 0..1, seconds &gt; 0.</summary>
        void Pulse(OVRInput.Controller controller, float amplitude, float seconds);

        /// <summary>Vibrate both controllers (for events with no clear owning hand).</summary>
        void PulseBoth(float amplitude, float seconds);
    }
}
