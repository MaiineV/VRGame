using Gameplay.Customer;
using Services;
using Services.UpdateService;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Diegetic
{
    /// <summary>
    /// World-space patience bar floating above a seated customer. It drains from full
    /// (just sat down) to empty (out of patience) and tints green → yellow → red, pulsing
    /// near the end, so the player can read at a glance how urgent each order is.
    ///
    /// Seat-anchored like <see cref="UI.CustomerOrderLabel"/>: it watches one
    /// <see cref="CustomerSeatPoint"/> and tracks whichever customer is bound to it. The
    /// visual (canvas + background + fill quad) is built procedurally at runtime so there
    /// are no UI references to wire — only the seat, which is auto-found from the parent
    /// when left unset. The fill is sized with anchors (not Image.fillAmount) so it renders
    /// correctly without a sprite.
    /// </summary>
    public sealed class CustomerPatienceBar : MonoBehaviour, IUpdateListener
    {
        [SerializeField] private CustomerSeatPoint _seat;

        [Header("Placement")]
        [Tooltip("Offset above the customer root (world units). Sits just under the order label.")]
        [SerializeField] private Vector3 _headOffset = new Vector3(0f, 2.25f, 0f);
        [Tooltip("Bar size in world units (width, height).")]
        [SerializeField] private Vector2 _size = new Vector2(0.55f, 0.075f);

        [Header("Colors (full → empty)")]
        [SerializeField] private Color _calmColor   = new Color(0.27f, 0.85f, 0.35f);
        [SerializeField] private Color _warnColor   = new Color(0.96f, 0.80f, 0.20f);
        [SerializeField] private Color _urgentColor = new Color(0.92f, 0.22f, 0.16f);
        [Tooltip("Below this remaining ratio the bar pulses to scream 'hurry up'.")]
        [Range(0f, 1f)]
        [SerializeField] private float _pulseThreshold = 0.25f;

        private CustomerEntity _customer;
        private Transform _cam;
        private bool _registered;

        // Procedurally built visual.
        private RectTransform _fillRect;
        private Image _fillImage;

        void Awake()
        {
            if (_seat == null) _seat = GetComponentInParent<CustomerSeatPoint>();
            BuildVisual();
            SetVisible(false);
        }

        void OnEnable()
        {
            _cam = Camera.main != null ? Camera.main.transform : null;

            if (ServiceLocator.TryGet<IUpdateService>(out var svc))
            {
                svc.AddUpdateListener(this);
                _registered = true;
            }

            if (_seat == null) return;
            _seat.CustomerBound += HandleBound;
            _seat.CustomerCleared += HandleCleared;
            if (_seat.CurrentCustomer != null) HandleBound(_seat.CurrentCustomer);
            else HandleCleared();
        }

        void OnDisable()
        {
            if (_registered && ServiceLocator.TryGet<IUpdateService>(out var svc))
                svc.RemoveUpdateListener(this);
            _registered = false;

            if (_seat != null)
            {
                _seat.CustomerBound -= HandleBound;
                _seat.CustomerCleared -= HandleCleared;
            }
            Unsubscribe();
        }

        private void HandleBound(CustomerEntity c)
        {
            Unsubscribe();
            _customer = c;
            if (c != null)
            {
                c.Served += OnResolved;
                c.Left += OnLeft;
            }
            SetVisible(c != null);
        }

        private void HandleCleared()
        {
            Unsubscribe();
            _customer = null;
            SetVisible(false);
        }

        // Once served or gone the countdown is meaningless — hide the bar.
        private void OnResolved(CustomerEntity c, Data.Enums.RecipeId recipe, float score, bool isExact) => SetVisible(false);
        private void OnLeft(CustomerEntity c, bool happy) => SetVisible(false);

        private void Unsubscribe()
        {
            if (_customer == null) return;
            _customer.Served -= OnResolved;
            _customer.Left -= OnLeft;
        }

        public void MyUpdate()
        {
            if (_customer == null || _fillImage == null) return;

            // Keep the bar pinned above the customer and facing the player.
            transform.position = _customer.transform.position + _headOffset;
            if (_cam == null && Camera.main != null) _cam = Camera.main.transform;
            if (_cam != null)
                transform.rotation = Quaternion.LookRotation(transform.position - _cam.position);

            float max = _customer.So != null ? _customer.So.PatienceSeconds : 0f;
            float ratio = max > 0.01f ? Mathf.Clamp01(_customer.WaitTimer / max) : 0f;

            // Width via anchors so it renders without a sprite.
            _fillRect.anchorMax = new Vector2(ratio, 1f);

            // Green when full, through yellow, to red when nearly out.
            Color c = ratio > 0.5f
                ? Color.Lerp(_warnColor, _calmColor, (ratio - 0.5f) * 2f)
                : Color.Lerp(_urgentColor, _warnColor, ratio * 2f);

            if (ratio <= _pulseThreshold && ratio > 0f)
            {
                float pulse = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Time.time * 6f));
                c.r *= pulse; c.g *= pulse; c.b *= pulse;
            }
            _fillImage.color = c;
        }

        private void SetVisible(bool visible)
        {
            if (_fillImage != null) _fillImage.transform.parent.gameObject.SetActive(visible);
        }

        /// <summary>Builds a world-space canvas with a dark background and a left-anchored fill quad.</summary>
        private void BuildVisual()
        {
            var canvasGo = new GameObject("PatienceBar", typeof(RectTransform), typeof(Canvas));
            var canvasRect = (RectTransform)canvasGo.transform;
            canvasRect.SetParent(transform, false);

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasRect.sizeDelta = _size;            // world-space: 1 unit = 1 metre
            canvasRect.localPosition = Vector3.zero;

            // Background: full-stretch dark quad.
            var bg = NewImage("BG", canvasRect);
            bg.color = new Color(0f, 0f, 0f, 0.6f);
            Stretch(bg.rectTransform, Vector2.zero, Vector2.one);

            // Fill: anchored to the left edge; width driven by anchorMax.x each frame.
            _fillImage = NewImage("Fill", canvasRect);
            _fillImage.color = _calmColor;
            _fillRect = _fillImage.rectTransform;
            _fillRect.anchorMin = new Vector2(0f, 0f);
            _fillRect.anchorMax = new Vector2(1f, 1f);
            _fillRect.pivot = new Vector2(0f, 0.5f);
            _fillRect.offsetMin = new Vector2(0.004f, 0.004f);   // tiny inset border
            _fillRect.offsetMax = new Vector2(-0.004f, -0.004f);
        }

        private static Image NewImage(string name, RectTransform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.raycastTarget = false;   // non-interactive HUD element
            return img;
        }

        private static void Stretch(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
