using Data.Enums;
using Gameplay.Customer;
using Gameplay.Liquid;
using UnityEngine;

namespace UI
{
    /// <summary>
    /// Text-free order indicator floating over a customer:
    ///  - a coloured ORB in the drink's colour (match it to the bottle's color tag), and
    ///  - a vertical GAUGE showing the requested fill level.
    /// Built procedurally (no per-seat scene wiring), seat-anchored, billboards to the camera.
    /// On serve it flashes green/amber/red, then returns to the order colour. Replaces the old
    /// "Pedido: X% / Tu vaso: Y%" text label.
    /// </summary>
    public sealed class CustomerOrderLabel : MonoBehaviour
    {
        [SerializeField] private CustomerSeatPoint _seat;
        [Tooltip("Offset above the customer root (world units).")]
        [SerializeField] private Vector3 _headOffset = new Vector3(0f, 2.6f, 0f);
        [SerializeField] private float _orbSize = 0.16f;
        [SerializeField] private float _gaugeHeight = 0.22f;
        [SerializeField] private float _resultSeconds = 2f;
        [Tooltip("Legacy text label root from the old order UI; hidden on enable if still present.")]
        [SerializeField] private GameObject _root;

        private CustomerEntity _customer;
        private Transform _cam;

        private Transform _indicator;       // container toggled on/off
        private Material _orbMat, _gaugeFillMat;
        private Transform _gaugeFill;
        private float _gaugeBottomY, _gaugeFillW, _gaugeX;

        private Color _drinkColor = Color.white;
        private float _resultTimer;

        void OnEnable()
        {
            if (_root != null) _root.SetActive(false); // kill the old text UI if it lingers
            if (_seat == null) return;
            _seat.CustomerBound += HandleBound;
            _seat.CustomerCleared += HandleCleared;
            if (_seat.CurrentCustomer != null) HandleBound(_seat.CurrentCustomer);
            else SetVisible(false);
        }

        void OnDisable()
        {
            if (_seat != null)
            {
                _seat.CustomerBound -= HandleBound;
                _seat.CustomerCleared -= HandleCleared;
            }
            Unsub();
        }

        private void HandleBound(CustomerEntity c)
        {
            Unsub();
            _customer = c;
            if (c != null) { c.Served += OnServed; c.Left += OnLeft; }

            EnsureVisuals();
            _drinkColor = DrinkColorUtil.For(c != null ? c.TargetRecipe : RecipeId.None);
            ApplyColor(_drinkColor);
            SetGauge(FillLevels.RatioOf(c != null ? c.TargetLevel : 0));
            _resultTimer = 0f;
            SetVisible(true);
        }

        private void HandleCleared()
        {
            Unsub();
            _customer = null;
            if (_resultTimer <= 0f) SetVisible(false); // keep a lingering result on screen
        }

        private void Unsub()
        {
            if (_customer != null) { _customer.Served -= OnServed; _customer.Left -= OnLeft; }
        }

        private void OnServed(CustomerEntity c, RecipeId recipe, float score, bool isExact)
        {
            Color flash = isExact
                ? new Color(0.3f, 1f, 0.4f)                          // perfect: green
                : (score > 0f ? new Color(1f, 0.8f, 0.3f)            // partial: amber
                              : new Color(1f, 0.3f, 0.25f));         // bad: red
            ApplyColor(flash);
            _resultTimer = _resultSeconds;
        }

        private void OnLeft(CustomerEntity c, bool happy)
        {
            if (happy) return;
            ApplyColor(new Color(1f, 0.3f, 0.25f));
            _resultTimer = _resultSeconds;
        }

        void LateUpdate()
        {
            if (_customer != null) transform.position = _customer.transform.position + _headOffset;
            if (_cam == null && Camera.main != null) _cam = Camera.main.transform;
            if (_cam != null) transform.rotation = Quaternion.LookRotation(transform.position - _cam.position);

            if (_resultTimer > 0f)
            {
                _resultTimer -= Time.deltaTime;
                if (_resultTimer <= 0f)
                {
                    if (_customer == null) SetVisible(false);
                    else ApplyColor(_drinkColor); // back to the order colour
                }
            }
        }

        // --- Procedural visuals ---------------------------------------------------------------

        private void EnsureVisuals()
        {
            if (_indicator != null) return;

            var unlit = Shader.Find("Universal Render Pipeline/Unlit")
                     ?? Shader.Find("Unlit/Color")
                     ?? Shader.Find("Sprites/Default");

            var container = new GameObject("OrderIndicator");
            _indicator = container.transform;
            _indicator.SetParent(transform, false);

            // Robust to whatever scale this GameObject has in the scene. The label used to be a
            // world-space text Canvas (RectTransform scaled to ~0.004), and the orb/gauge primitives
            // below are built at metre scale (0.16 etc). If we inherited that 0.004 the indicator
            // would render sub-millimetre — invisible. Cancel the parent's lossy scale so the
            // container sits at world scale 1 regardless of how the seat object is set up.
            Vector3 ls = transform.lossyScale;
            _indicator.localScale = new Vector3(
                Mathf.Abs(ls.x) > 1e-5f ? 1f / ls.x : 1f,
                Mathf.Abs(ls.y) > 1e-5f ? 1f / ls.y : 1f,
                Mathf.Abs(ls.z) > 1e-5f ? 1f / ls.z : 1f);

            // Orb
            var orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            orb.name = "OrderOrb";
            StripCollider(orb);
            orb.transform.SetParent(_indicator, false);
            orb.transform.localScale = Vector3.one * _orbSize;
            _orbMat = new Material(unlit);
            orb.GetComponent<Renderer>().sharedMaterial = _orbMat;

            // Gauge geometry (to the right of the orb)
            float w = 0.05f;
            _gaugeX = _orbSize * 0.5f + 0.06f;
            _gaugeBottomY = -_gaugeHeight * 0.5f;
            _gaugeFillW = w * 0.7f;

            var bg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bg.name = "GaugeBg";
            StripCollider(bg);
            bg.transform.SetParent(_indicator, false);
            bg.transform.localScale = new Vector3(w, _gaugeHeight, 0.01f);
            bg.transform.localPosition = new Vector3(_gaugeX, 0f, 0.012f);
            var bgMat = new Material(unlit);
            SetMatColor(bgMat, new Color(0.1f, 0.1f, 0.12f, 1f));
            bg.GetComponent<Renderer>().sharedMaterial = bgMat;

            var fill = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fill.name = "GaugeFill";
            StripCollider(fill);
            fill.transform.SetParent(_indicator, false);
            _gaugeFillMat = new Material(unlit);
            fill.GetComponent<Renderer>().sharedMaterial = _gaugeFillMat;
            _gaugeFill = fill.transform;
        }

        private void SetGauge(float ratio)
        {
            if (_gaugeFill == null) return;
            ratio = Mathf.Clamp01(ratio);
            float fh = Mathf.Max(0.001f, _gaugeHeight * ratio);
            _gaugeFill.localScale = new Vector3(_gaugeFillW, fh, 0.013f);
            _gaugeFill.localPosition = new Vector3(_gaugeX, _gaugeBottomY + fh * 0.5f, 0f);
        }

        private void ApplyColor(Color c)
        {
            if (_orbMat != null) SetMatColor(_orbMat, c);
            if (_gaugeFillMat != null) SetMatColor(_gaugeFillMat, c);
        }

        private void SetVisible(bool on)
        {
            if (_indicator != null && _indicator.gameObject.activeSelf != on)
                _indicator.gameObject.SetActive(on);
        }

        private static void StripCollider(GameObject go)
        {
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);
        }

        private static void SetMatColor(Material m, Color c)
        {
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            m.color = c;
        }
    }
}
