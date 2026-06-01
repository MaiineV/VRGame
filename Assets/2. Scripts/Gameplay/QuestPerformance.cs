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
        }
    }
}
