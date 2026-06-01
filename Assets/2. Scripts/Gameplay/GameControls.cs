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

        private IGameStateService _state;
        private float _quitHeld;

        void Update()
        {
            if (_state == null && !ServiceLocator.TryGet<IGameStateService>(out _state)) return;

            if (OVRInput.GetDown(OVRInput.Button.One))
            {
                if (_state.Current == Services.GameState.GameState.Idle) _state.BeginNight();
                else if (_state.Current == Services.GameState.GameState.NightSummary) _state.AcknowledgeSummary();
            }

            if (OVRInput.GetDown(OVRInput.Button.Two))
            {
                if (_state.Current == Services.GameState.GameState.NightRunning) _state.AbortNight();
            }

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
