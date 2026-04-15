using Data.SO;
using Gameplay.Interactions;
using Services;
using Services.Economy;
using Services.GameState;
using Services.Save;
using TMPro;
using UnityEngine;

namespace UI.Diegetic
{
    /// <summary>
    /// Diegetic, grabbable night-flow panel. Replaces the old screen-space NightFlowView.
    ///
    /// Physical layout (setup in Unity):
    ///  - Root is a clipboard mesh with Rigidbody + Collider + GrabBridge.
    ///  - Three child groups (Idle / Running / Summary) each holding the relevant PokeButtons
    ///    and TMP labels mounted as world-space children.
    ///  - Only the group matching the current GameState is active at any time.
    ///
    /// Input is purely physical: the player grabs the clipboard with one hand and pokes the
    /// embedded PokeButtons with the other hand's index finger. No GraphicRaycaster / pointer.
    /// </summary>
    public sealed class NightClipboard : MonoBehaviour
    {
        [Header("Config")]
        [Tooltip("Staged as PendingConfig when the clipboard is enabled.")]
        [SerializeField] private NightConfigSO _config;

        [Header("Grab (optional)")]
        [Tooltip("If set, the clipboard only reacts to buttons while held. Leave null to ignore.")]
        [SerializeField] private GrabBridge _grab;

        [Header("Groups (one active per state)")]
        [SerializeField] private GameObject _idleGroup;
        [SerializeField] private GameObject _runningGroup;
        [SerializeField] private GameObject _summaryGroup;

        [Header("Physical buttons")]
        [SerializeField] private PokeButton _startButton;
        [SerializeField] private PokeButton _abortButton;
        [SerializeField] private PokeButton _continueButton;

        [Header("Summary labels")]
        [SerializeField] private TMP_Text _summaryCash;
        [SerializeField] private TMP_Text _summarySales;
        [SerializeField] private TMP_Text _summaryFailed;
        [SerializeField] private TMP_Text _summaryExpenses;
        [SerializeField] private TMP_Text _summaryNightlyEarnings;

        [Header("Idle labels (progression)")]
        [SerializeField] private TMP_Text _idleNightNumber;
        [SerializeField] private TMP_Text _idleBestEarnings;
        [SerializeField] private TMP_Text _idleCash;

        private IGameStateService _state;
        private IEconomyService _economy;
        private ISaveService _save;

        void OnEnable()
        {
            if (!ServiceLocator.TryGet<IGameStateService>(out _state)) return;
            ServiceLocator.TryGet<IEconomyService>(out _economy);
            ServiceLocator.TryGet<ISaveService>(out _save);

            if (_config != null) _state.SetPendingConfig(_config);

            _state.StateChanged += OnStateChanged;
            if (_startButton != null)    _startButton.Pressed    += OnStartPressed;
            if (_abortButton != null)    _abortButton.Pressed    += OnAbortPressed;
            if (_continueButton != null) _continueButton.Pressed += OnContinuePressed;

            if (_grab != null)
            {
                _grab.Grabbed += RefreshInteractable;
                _grab.Released += RefreshInteractable;
            }

            ApplyState(_state.Current);
        }

        void OnDisable()
        {
            if (_state != null) _state.StateChanged -= OnStateChanged;
            if (_startButton != null)    _startButton.Pressed    -= OnStartPressed;
            if (_abortButton != null)    _abortButton.Pressed    -= OnAbortPressed;
            if (_continueButton != null) _continueButton.Pressed -= OnContinuePressed;
            if (_grab != null)
            {
                _grab.Grabbed -= RefreshInteractable;
                _grab.Released -= RefreshInteractable;
            }
            _state = null;
            _economy = null;
            _save = null;
        }

        private void OnStartPressed()    { if (IsActive()) _state?.BeginNight(); }
        private void OnAbortPressed()    { if (IsActive()) _state?.AbortNight(); }
        private void OnContinuePressed() { if (IsActive()) _state?.AcknowledgeSummary(); }

        private bool IsActive() => _grab == null || _grab.IsHeld;

        private void OnStateChanged(Services.GameState.GameState from, Services.GameState.GameState to) => ApplyState(to);

        private void ApplyState(Services.GameState.GameState s)
        {
            if (_idleGroup != null)    _idleGroup.SetActive(s == Services.GameState.GameState.Idle);
            if (_runningGroup != null) _runningGroup.SetActive(s == Services.GameState.GameState.NightRunning);
            if (_summaryGroup != null) _summaryGroup.SetActive(s == Services.GameState.GameState.NightSummary);

            if (s == Services.GameState.GameState.NightSummary) FillSummary();
            else if (s == Services.GameState.GameState.Idle) FillIdle();
            RefreshInteractable();
        }

        private void RefreshInteractable()
        {
            bool held = IsActive();
            if (_startButton != null)    _startButton.Interactable    = held && _state != null && _state.Current == Services.GameState.GameState.Idle;
            if (_abortButton != null)    _abortButton.Interactable    = held && _state != null && _state.Current == Services.GameState.GameState.NightRunning;
            if (_continueButton != null) _continueButton.Interactable = held && _state != null && _state.Current == Services.GameState.GameState.NightSummary;
        }

        private void FillSummary()
        {
            if (_economy == null) return;
            if (_summaryCash != null)             _summaryCash.text             = $"${_economy.Cash}";
            if (_summarySales != null)            _summarySales.text            = _economy.Sales.ToString();
            if (_summaryFailed != null)           _summaryFailed.text           = _economy.FailedOrders.ToString();
            if (_summaryExpenses != null)         _summaryExpenses.text         = $"-${_economy.Expenses}";
            if (_summaryNightlyEarnings != null)  _summaryNightlyEarnings.text  = FormatSigned(_economy.NightlyEarnings);
        }

        private void FillIdle()
        {
            if (_save == null) return;
            var d = _save.Current;
            if (_idleNightNumber != null)  _idleNightNumber.text  = $"Night {d.nightsCompleted + 1}";
            if (_idleBestEarnings != null) _idleBestEarnings.text = $"Best: ${d.bestNightEarnings}";
            if (_idleCash != null)         _idleCash.text         = $"${d.cash}";
        }

        private static string FormatSigned(int n) => n >= 0 ? $"+${n}" : $"-${-n}";
    }
}
