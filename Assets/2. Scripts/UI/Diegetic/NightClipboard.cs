using Data.SO;
using Gameplay.Interactions;
using Services;
using Services.Economy;
using Services.GameState;
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
        [Tooltip("If set, the clipboard tracks held state for visuals/feedback. Leave null to ignore.")]
        [SerializeField] private GrabBridge _grab;
        [Tooltip("When true, buttons only react while the clipboard is held (two-handed: grab with one " +
                 "hand, poke with the other). When false (default), you can poke the buttons while the " +
                 "clipboard rests on the bar too.")]
        [SerializeField] private bool _requireHeld = false;

        [Header("Input guard")]
        [Tooltip("After any button press, ALL clipboard buttons go non-interactive for this long. Stops a " +
                 "single poke (or a finger still in range) from also triggering the button that swaps into " +
                 "its place when the state changes, and prevents spamming. ~1-2s feels right.")]
        [SerializeField] private float _buttonCooldownSeconds = 1.5f;

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

        private IGameStateService _state;
        private IEconomyService _economy;
        private float _cooldownRemaining;

        void Update()
        {
            // Tick down the post-press guard; re-enable the buttons the moment it expires.
            if (_cooldownRemaining <= 0f) return;
            _cooldownRemaining -= Time.deltaTime;
            if (_cooldownRemaining <= 0f) RefreshInteractable();
        }

        void OnEnable()
        {
            if (!ServiceLocator.TryGet<IGameStateService>(out _state)) return;
            ServiceLocator.TryGet<IEconomyService>(out _economy);

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
        }

        private void OnStartPressed()
        {
            if (_cooldownRemaining > 0f || !IsActive() || _state == null) return;
            // Start works in any non-running state. After a night we sit in NightSummary; acknowledge
            // it first (resets the per-night economy) so the next night starts clean.
            if (_state.Current == Services.GameState.GameState.NightSummary) _state.AcknowledgeSummary();
            _state.BeginNight();
            BeginCooldown();
        }
        private void OnAbortPressed()    { if (_cooldownRemaining <= 0f && IsActive()) { _state?.AbortNight(); BeginCooldown(); } }
        private void OnContinuePressed() { if (_cooldownRemaining <= 0f && IsActive()) { _state?.AcknowledgeSummary(); BeginCooldown(); } }

        // Lock out all buttons briefly after a press, then refresh so the now-relevant button stays dead
        // until the guard expires (handled in Update).
        private void BeginCooldown()
        {
            _cooldownRemaining = Mathf.Max(0f, _buttonCooldownSeconds);
            RefreshInteractable();
        }

        private bool IsActive() => !_requireHeld || _grab == null || _grab.IsHeld;

        private void OnStateChanged(Services.GameState.GameState from, Services.GameState.GameState to) => ApplyState(to);

        private void ApplyState(Services.GameState.GameState s)
        {
            // Binary visibility: the Start group shows in every state that ISN'T the running night
            // (DayShop, Idle, NightSummary, Boot) so the player can always start the next night; the
            // Stop group shows only while the night runs. This avoids getting stranded in NightSummary
            // with no visible button (the old per-state split left nothing on screen there).
            bool running = s == Services.GameState.GameState.NightRunning;
            if (_idleGroup != null)    _idleGroup.SetActive(!running);
            if (_runningGroup != null) _runningGroup.SetActive(running);
            if (_summaryGroup != null) _summaryGroup.SetActive(s == Services.GameState.GameState.NightSummary);

            // The idle/running groups aren't wired in this scene, so toggle the physical buttons
            // directly: only the button relevant to the current state is shown. SetActive(false) hides
            // AND makes it non-interactive (so Start and Stop are mutually exclusive — Start while the
            // night isn't running, Stop only while it runs; Continue only in the summary).
            if (_startButton != null)    _startButton.gameObject.SetActive(!running);
            if (_abortButton != null)    _abortButton.gameObject.SetActive(running);
            if (_continueButton != null) _continueButton.gameObject.SetActive(s == Services.GameState.GameState.NightSummary);

            if (s == Services.GameState.GameState.NightSummary) FillSummary();
            RefreshInteractable();
        }

        private void RefreshInteractable()
        {
            bool held = IsActive();
            bool ready = _cooldownRemaining <= 0f;   // post-press guard blocks every button while active
            bool running = _state != null && _state.Current == Services.GameState.GameState.NightRunning;
            if (_startButton != null)    _startButton.Interactable    = ready && held && _state != null && !running;
            if (_abortButton != null)    _abortButton.Interactable    = ready && held && running;
            if (_continueButton != null) _continueButton.Interactable = ready && held && _state != null && _state.Current == Services.GameState.GameState.NightSummary;
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

        private static string FormatSigned(int n) => n >= 0 ? $"+${n}" : $"-${-n}";
    }
}
