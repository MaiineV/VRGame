using Services.UpdateService;
using UnityEngine;

namespace Services.Haptics
{
    /// <summary>
    /// Drives OVRInput controller vibration. Mirrors the AudioService model: a tiny amount of
    /// per-hand state, ticked once per frame off IUpdateService — no per-pulse coroutines or
    /// GameObjects. SetControllerVibration must be re-issued each frame to sustain, and zeroed
    /// when the pulse ends, which is exactly what the tick does.
    /// </summary>
    public sealed class HapticService : IHapticService, IUpdateListener
    {
        // Touch controllers ignore frequency for the most part; keep it mid so amplitude carries the feel.
        private const float Frequency = 1f;

        // Index 0 = LTouch, 1 = RTouch.
        private readonly float[] _remaining = new float[2];
        private readonly float[] _amplitude = new float[2];

        private IUpdateService _updates;

        public void Initialize()
        {
            if (ServiceLocator.TryGet<IUpdateService>(out _updates))
                _updates.AddUpdateListener(this);
        }

        public void Pulse(OVRInput.Controller controller, float amplitude, float seconds)
        {
            amplitude = Mathf.Clamp01(amplitude);
            if (amplitude <= 0f || seconds <= 0f) return;
            if (!TryIndex(controller, out int i)) return;

            // Strongest/longest pulse wins so a small overlapping pulse can't cut a big one short.
            _amplitude[i] = Mathf.Max(_amplitude[i], amplitude);
            _remaining[i] = Mathf.Max(_remaining[i], seconds);
        }

        public void PulseBoth(float amplitude, float seconds)
        {
            Pulse(OVRInput.Controller.LTouch, amplitude, seconds);
            Pulse(OVRInput.Controller.RTouch, amplitude, seconds);
        }

        public void MyUpdate()
        {
            Tick(0, OVRInput.Controller.LTouch);
            Tick(1, OVRInput.Controller.RTouch);
        }

        private void Tick(int i, OVRInput.Controller c)
        {
            if (_remaining[i] <= 0f) return;

            _remaining[i] -= Time.unscaledDeltaTime;
            if (_remaining[i] <= 0f)
            {
                _remaining[i] = 0f;
                _amplitude[i] = 0f;
                OVRInput.SetControllerVibration(0f, 0f, c);
            }
            else
            {
                OVRInput.SetControllerVibration(Frequency, _amplitude[i], c);
            }
        }

        private static bool TryIndex(OVRInput.Controller c, out int i)
        {
            if (c == OVRInput.Controller.LTouch) { i = 0; return true; }
            if (c == OVRInput.Controller.RTouch) { i = 1; return true; }
            i = -1;
            return false;
        }
    }
}
