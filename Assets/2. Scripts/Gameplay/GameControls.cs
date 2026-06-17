using Data.SO;
using Services;
using Services.GameState;
using UnityEngine;

namespace Gameplay
{
    /// <summary>
    /// Controller-based game flow — a reliable way to start/abort/quit without depending on the
    /// diegetic poke clipboard (whose finger-collider setup is hard to verify).
    ///   A (right)  : Start night when idle / acknowledge the night summary.
    ///   B (right)  : Abort the running night.
    ///   Hold Y (left, 1.5s): Quit the game.
    /// </summary>
    public sealed class GameControls : MonoBehaviour
    {
        [SerializeField] private float _quitHoldSeconds = 1.5f;

        [Tooltip("Night to start with the A button when no clipboard staged one. Lets the A button " +
                 "begin the night on its own, independent of the diegetic clipboard.")]
        [SerializeField] private NightConfigSO _fallbackConfig;

        private IGameStateService _state;
        private float _quitHeld;

        void Update()
        {
            if (_state == null && !ServiceLocator.TryGet<IGameStateService>(out _state)) return;

            // A/B no longer start or abort the night — that lives on the diegetic clipboard.
            // They used to flip the game state, which restarted the state-driven music on every
            // press. A still acknowledges the end-of-night summary (a deliberate, one-off step).
            if (OVRInput.GetDown(OVRInput.Button.One)
                && _state.Current == Services.GameState.GameState.NightSummary)
                _state.AcknowledgeSummary();

            if (OVRInput.Get(OVRInput.Button.Four))
            {
                _quitHeld += Time.deltaTime;
                if (_quitHeld >= _quitHoldSeconds) Quit();
            }
            else
            {
                _quitHeld = 0f;
            }
        }

        /// <summary>
        /// Starts the night. If the clipboard never staged a config (e.g. it was never grabbed/enabled),
        /// fall back to our own so the A button works on its own.
        /// </summary>
        private void BeginNight()
        {
            if (_state.PendingConfig == null && _fallbackConfig != null)
                _state.SetPendingConfig(_fallbackConfig);
            _state.BeginNight();
        }

        private static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
