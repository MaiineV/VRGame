using Core;
using Core.Managers;
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
    ///   Hold Y (left, 1.5s): Exit to the main menu (its Salir button does the actual quit).
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

            if (OVRInput.GetDown(OVRInput.Button.One))
            {
                if (_state.Current == Services.GameState.GameState.DayShop
                    || _state.Current == Services.GameState.GameState.Idle) BeginNight();
                else if (_state.Current == Services.GameState.GameState.NightSummary) _state.AcknowledgeSummary();
            }

            if (OVRInput.GetDown(OVRInput.Button.Two))
            {
                if (_state.Current == Services.GameState.GameState.NightRunning) _state.AbortNight();
            }

            if (OVRInput.Get(OVRInput.Button.Four))
            {
                _quitHeld += Time.deltaTime;
                if (_quitHeld >= _quitHoldSeconds) ExitToMenu();
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

        /// <summary>
        /// Leaves the bar for the main menu (whose Salir button owns the real Application.Quit).
        /// A running night is aborted first so the persistent services don't come back to the menu
        /// still in NightRunning.
        /// </summary>
        private void ExitToMenu()
        {
            _quitHeld = 0f;
            if (_state.Current == Services.GameState.GameState.NightRunning) _state.AbortNight();
            SceneLoadManager.LoadWithLoading(SceneNames.MainMenu, SceneNames.Loading);
        }
    }
}
