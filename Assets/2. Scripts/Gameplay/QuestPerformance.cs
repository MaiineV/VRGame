using UnityEngine;

namespace Gameplay
{
    /// <summary>
    /// Locks the render rate to the headset's refresh (72 Hz on Quest 2). Stable frame pacing
    /// is critical for VR comfort: dropped frames cause reprojection judder that induces nausea.
    /// </summary>
    public sealed class QuestPerformance : MonoBehaviour
    {
        [SerializeField] private int _targetHz = 72;
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
        }
    }
}
