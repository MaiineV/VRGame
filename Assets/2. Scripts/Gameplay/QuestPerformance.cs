using UnityEngine;

namespace Gameplay
{
    /// <summary>
    /// Locks the render rate to the headset's native refresh. Targets 90 Hz on Quest 3;
    /// Quest 2 falls back to 72 Hz when 90 is not available. Stable frame pacing is critical
    /// for VR comfort: dropped frames cause reprojection judder that induces nausea.
    /// </summary>
    public sealed class QuestPerformance : MonoBehaviour
    {
        [SerializeField] private int _targetHz = 90;
        [Tooltip("Fixed Foveated Rendering reduces GPU cost toward the lens edges (where the eye " +
                 "can't resolve detail), buying frame-budget headroom so we hold the refresh rate. " +
                 "Stable pacing = far less judder-induced nausea. Dynamic auto-adjusts with load.")]
        [SerializeField] private bool _enableFoveatedRendering = true;
        [SerializeField] private OVRManager.FixedFoveatedRenderingLevel _foveationLevel =
            OVRManager.FixedFoveatedRenderingLevel.High;

        void Start()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = _targetHz;

            // Ask the Oculus runtime to run at the target refresh, if available.
            try
            {
                if (OVRManager.display != null)
                    OVRManager.display.displayFrequency = _targetHz;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[QuestPerformance] Could not set display frequency: {e.Message}");
            }

            // Fixed Foveated Rendering: cheap GPU win that protects frame pacing on Quest 2.
            if (_enableFoveatedRendering)
            {
                try
                {
                    OVRManager.fixedFoveatedRenderingLevel = _foveationLevel;
                    OVRManager.useDynamicFixedFoveatedRendering = true;
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[QuestPerformance] Could not set foveated rendering: {e.Message}");
                }
            }

            // Pin the CPU/GPU clocks high. By default the Quest's power governor downclocks during
            // low-load moments (cold start, the lull at each day/night transition) to save battery,
            // which is exactly the ~40 FPS valley seen before the clocks ramp back up. SustainedHigh
            // tells the runtime to hold high clocks instead of dropping them — the steady, sustainable
            // tier (Boost is only for brief transitions and the runtime ignores it as a permanent ask).
            try
            {
                OVRManager.suggestedCpuPerfLevel = OVRManager.ProcessorPerformanceLevel.SustainedHigh;
                OVRManager.suggestedGpuPerfLevel = OVRManager.ProcessorPerformanceLevel.SustainedHigh;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[QuestPerformance] Could not set processor perf levels: {e.Message}");
            }
        }
    }
}
