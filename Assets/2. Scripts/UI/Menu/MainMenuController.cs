using Core;
using Core.Managers;
using Services;
using Services.Save;
using UnityEngine;
using Utilities;

namespace UI.Menu
{
    /// <summary>
    /// Drives the main-menu scene: wires button events, shows/hides panels, and
    /// delegates scene transitions to <see cref="SceneLoadManager"/>.
    ///
    /// Implements design doc "Main Menu" (main-menu scene, laser-pointer navigation):
    ///   - Continuar   — only available when a save file exists; loads the Bar scene.
    ///   - Nueva partida — prompts for confirmation when a save exists; resets save and loads Bar.
    ///   - Cómo jugar  — toggles an informational panel (objective + controls).
    ///   - Salir       — quits the application.
    ///
    /// All button references are optional-guarded; missing refs emit a warning via
    /// <see cref="MyLogger"/> but do not throw.
    /// </summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        // ── main buttons ─────────────────────────────────────────────────────────

        [Header("Main Buttons")]
        [Tooltip("Loads the existing save into the Bar scene. Hidden/disabled when no save exists.")]
        [SerializeField] private MenuButton _continueButton;

        [Tooltip("Starts a new game. Shows a confirmation panel if a save already exists.")]
        [SerializeField] private MenuButton _newGameButton;

        [Tooltip("Toggles the how-to-play informational panel.")]
        [SerializeField] private MenuButton _howToButton;

        [Tooltip("Quits the application.")]
        [SerializeField] private MenuButton _quitButton;

        // ── panels ───────────────────────────────────────────────────────────────

        [Header("Panels")]
        [Tooltip("Panel showing objective and controls. Hidden by default.")]
        [SerializeField] private GameObject _howToPanel;

        [Tooltip("Confirmation panel shown before overwriting an existing save.")]
        [SerializeField] private GameObject _confirmNewGamePanel;

        // ── confirm buttons ───────────────────────────────────────────────────────

        [Header("Confirm New Game Buttons")]
        [Tooltip("Confirm overwrite — calls StartNewGame().")]
        [SerializeField] private MenuButton _confirmYesButton;

        [Tooltip("Cancel — hides the confirmation panel.")]
        [SerializeField] private MenuButton _confirmNoButton;

        // ── private state ─────────────────────────────────────────────────────────

        private ISaveService _save;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        void Start()
        {
            // Resolve the save service. The menu is usable without it (continue hidden),
            // but the ISaveService should always be available when bootstrapped via Boot.
            if (!ServiceLocator.TryGet<ISaveService>(out _save))
                MyLogger.LogWarning("[MainMenuController] ISaveService not found — Continuar will be hidden.");

            InitialiseUI();
            Subscribe();

            // Place the menu in front of the player at their real eye height. A fixed world Y is
            // fragile because the OVR tracking origin (eye-level vs floor) and seated/standing posture
            // change where "eye level" actually is — anchoring to the live head pose fixes the
            // "menu floats too high" problem regardless of setup.
            StartCoroutine(PositionInFrontOfUser());
        }

        [Header("Placement")]
        [Tooltip("Distance (m) the menu is placed in front of the player on start.")]
        [SerializeField] private float _spawnDistance = 1.8f;

        private System.Collections.IEnumerator PositionInFrontOfUser()
        {
            // The OVR head pose is NOT reliable for the first frames after load (it can read identity /
            // backwards before tracking settles — which placed the menu behind the player). Wait a beat
            // for a valid, settled pose, THEN snap the menu in front of wherever the player is looking.
            Camera cam = Camera.main;
            float waited = 0f;
            while (waited < 0.8f)
            {
                if (cam == null) cam = Camera.main;
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            if (cam == null) yield break;

            Transform head = cam.transform;
            Vector3 fwd = head.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 1e-4f) fwd = Vector3.forward;
            fwd.Normalize();

            transform.position = head.position + fwd * _spawnDistance;       // in front, at eye height
            transform.rotation = Quaternion.LookRotation(fwd, Vector3.up);   // face the player
        }

        void OnDisable()
        {
            Unsubscribe();
        }

        // ── setup ─────────────────────────────────────────────────────────────────

        private void InitialiseUI()
        {
            // Panels off by default.
            if (_howToPanel != null)           _howToPanel.SetActive(false);
            if (_confirmNewGamePanel != null)  _confirmNewGamePanel.SetActive(false);

            // Continuar is only meaningful when a save exists.
            bool hasSave = _save != null && _save.HasSave;
            if (_continueButton != null)
            {
                _continueButton.gameObject.SetActive(hasSave);
                _continueButton.Interactable = hasSave;
            }

            // Warn about missing mandatory button refs.
            if (_newGameButton == null) MyLogger.LogWarning("[MainMenuController] _newGameButton is not assigned.");
            if (_howToButton   == null) MyLogger.LogWarning("[MainMenuController] _howToButton is not assigned.");
            if (_quitButton    == null) MyLogger.LogWarning("[MainMenuController] _quitButton is not assigned.");
        }

        private void Subscribe()
        {
            if (_continueButton   != null) _continueButton.Clicked   += OnContinue;
            if (_newGameButton    != null) _newGameButton.Clicked    += OnNewGame;
            if (_howToButton      != null) _howToButton.Clicked      += OnHowTo;
            if (_quitButton       != null) _quitButton.Clicked       += OnQuit;
            if (_confirmYesButton != null) _confirmYesButton.Clicked += OnConfirmYes;
            if (_confirmNoButton  != null) _confirmNoButton.Clicked  += OnConfirmNo;
        }

        private void Unsubscribe()
        {
            if (_continueButton   != null) _continueButton.Clicked   -= OnContinue;
            if (_newGameButton    != null) _newGameButton.Clicked    -= OnNewGame;
            if (_howToButton      != null) _howToButton.Clicked      -= OnHowTo;
            if (_quitButton       != null) _quitButton.Clicked       -= OnQuit;
            if (_confirmYesButton != null) _confirmYesButton.Clicked -= OnConfirmYes;
            if (_confirmNoButton  != null) _confirmNoButton.Clicked  -= OnConfirmNo;
        }

        // ── button handlers ───────────────────────────────────────────────────────

        /// <summary>Loads the Bar scene, resuming the existing save.</summary>
        private void OnContinue()
        {
            SceneLoadManager.LoadWithLoading(SceneNames.Bar, SceneNames.Loading);
        }

        /// <summary>
        /// Shows a confirmation panel if a save exists; starts a new game directly otherwise.
        /// </summary>
        private void OnNewGame()
        {
            bool hasSave = _save != null && _save.HasSave;
            if (hasSave)
            {
                if (_confirmNewGamePanel != null) _confirmNewGamePanel.SetActive(true);
            }
            else
            {
                StartNewGame();
            }
        }

        /// <summary>Toggles the how-to-play / controls panel.</summary>
        private void OnHowTo()
        {
            if (_howToPanel == null) return;
            _howToPanel.SetActive(!_howToPanel.activeSelf);
        }

        /// <summary>Quits the application (exits play-mode in the Editor).</summary>
        private static void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        /// <summary>Confirmed overwrite — resets the save and loads the Bar scene.</summary>
        private void OnConfirmYes()
        {
            if (_confirmNewGamePanel != null) _confirmNewGamePanel.SetActive(false);
            StartNewGame();
        }

        /// <summary>Cancels new-game confirmation.</summary>
        private void OnConfirmNo()
        {
            if (_confirmNewGamePanel != null) _confirmNewGamePanel.SetActive(false);
        }

        // ── helpers ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Resets the save data and loads the Bar scene through the loading screen.
        /// </summary>
        private void StartNewGame()
        {
            if (_save != null)
                _save.NewGame();
            else
                MyLogger.LogWarning("[MainMenuController] StartNewGame called but ISaveService is null — proceeding without reset.");

            SceneLoadManager.LoadWithLoading(SceneNames.Bar, SceneNames.Loading);
        }
    }
}
