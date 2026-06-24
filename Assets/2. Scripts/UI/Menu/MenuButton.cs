using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.Menu
{
    /// <summary>
    /// A single menu button for the main-menu scene. Colour tinting is applied directly to an
    /// <see cref="Image"/> component.
    ///
    /// Driven by Meta Interaction SDK ray interaction: the menu Canvas carries a PointableCanvas +
    /// RayInteractable, and the EventSystem runs a PointableCanvasModule, so the controller/hand rays
    /// deliver standard uGUI pointer events. This button implements the pointer handlers to react to
    /// hover (highlight) and click (invoke <see cref="Clicked"/>).
    /// </summary>
    public sealed class MenuButton : MonoBehaviour,
        IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        // ── visual refs ──────────────────────────────────────────────────────────

        [Tooltip("Optional label — used to display the button text.")]
        [SerializeField] private TMP_Text _label;

        [Tooltip("Optional background image whose colour is tinted on highlight / disable.")]
        [SerializeField] private Image _background;

        // ── colours ──────────────────────────────────────────────────────────────

        [Header("Colours")]
        [SerializeField] private Color _normalColor      = new Color(0.15f, 0.15f, 0.20f, 0.90f);
        [SerializeField] private Color _highlightedColor = new Color(0.35f, 0.60f, 0.95f, 0.95f);
        [SerializeField] private Color _disabledColor    = new Color(0.10f, 0.10f, 0.10f, 0.50f);

        // ── state ────────────────────────────────────────────────────────────────

        private bool _interactable = true;
        private bool _highlighted;

        /// <summary>
        /// When <c>false</c> the button is visually dimmed and <see cref="Click"/> is a no-op.
        /// </summary>
        public bool Interactable
        {
            get => _interactable;
            set
            {
                _interactable = value;
                RefreshVisuals();
            }
        }

        // ── events ───────────────────────────────────────────────────────────────

        /// <summary>Raised when the button is clicked and <see cref="Interactable"/> is true.</summary>
        public event System.Action Clicked;

        // ── public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Called by <see cref="VrLaserPointer"/> when the ray enters or leaves this button.
        /// </summary>
        public void SetHighlighted(bool on)
        {
            _highlighted = on;
            RefreshVisuals();
        }

        /// <summary>
        /// Fires <see cref="Clicked"/> if <see cref="Interactable"/> is true.
        /// Called by <see cref="VrLaserPointer"/> on trigger input.
        /// </summary>
        public void Click()
        {
            if (!_interactable) return;
            Clicked?.Invoke();
        }

        /// <summary>
        /// Convenience wrapper — forwards to <c>gameObject.SetActive(v)</c>.
        /// </summary>
        public void SetActive(bool v) => gameObject.SetActive(v);

        // ── uGUI pointer events (driven by the ISDK PointableCanvasModule) ───────────

        public void OnPointerClick(PointerEventData eventData) => Click();
        public void OnPointerEnter(PointerEventData eventData) => SetHighlighted(true);
        public void OnPointerExit(PointerEventData eventData)  => SetHighlighted(false);

        // ── Unity ────────────────────────────────────────────────────────────────

        void Awake()
        {
            // Self-wire so the button works without manual Inspector assignment: the background is the
            // Image on this object, the label the first TMP text found in children.
            if (_background == null) _background = GetComponent<Image>();
            if (_label == null) _label = GetComponentInChildren<TMP_Text>(true);
        }

        void OnEnable()  => RefreshVisuals();
        void OnDisable() => RefreshVisuals();

        // ── helpers ──────────────────────────────────────────────────────────────

        private void RefreshVisuals()
        {
            if (_background == null) return;

            if (!_interactable)
                _background.color = _disabledColor;
            else if (_highlighted)
                _background.color = _highlightedColor;
            else
                _background.color = _normalColor;
        }
    }
}
