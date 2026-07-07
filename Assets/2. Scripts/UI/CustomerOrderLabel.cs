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
        private Material _orbMat;
        private Shader _unlitShader;
        // One cube + material per recipe ingredient, stacked bottom-up in the gauge so the player
        // sees a proportional colour band per ingredient instead of one blended colour.
        private readonly System.Collections.Generic.List<Transform> _gaugeSegments = new();
        private readonly System.Collections.Generic.List<Material> _gaugeSegmentMats = new();
        private (Color color, float ratio)[] _segments = { (Color.white, 1f) };
        private float _gaugeBottomY, _gaugeFillW, _gaugeX, _levelRatio;

        private Color _drinkColor = Color.white;
        private float _resultTimer;

        // Non-colour signal for the serve result: colourblind players can't rely on green/amber/red
        // alone, so each outcome also gets a distinct motion pattern on the indicator.
        private enum ResultKind { None, Perfect, Partial, Bad }
        private ResultKind _resultKind = ResultKind.None;
        private Vector3 _indicatorBaseScale = Vector3.one;

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
            RecipeId recipe = c != null ? c.TargetRecipe : RecipeId.None;
            _drinkColor = DrinkColorUtil.For(recipe);
            _segments = DrinkColorUtil.Segments(recipe);
            _levelRatio = FillLevels.RatioOf(c != null ? c.TargetLevel : 0);
            ApplyOrbColor(_drinkColor);
            SetGauge(_levelRatio);
            _resultTimer = 0f;
            _resultKind = ResultKind.None;
            _indicator.localScale = _indicatorBaseScale; // clear any leftover pulse/shake from the previous customer
            _indicator.localPosition = Vector3.zero;
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
            Color flash;
            if (isExact) { flash = new Color(0.3f, 1f, 0.4f); _resultKind = ResultKind.Perfect; }
            else if (score > 0f) { flash = new Color(1f, 0.8f, 0.3f); _resultKind = ResultKind.Partial; }
            else { flash = new Color(1f, 0.3f, 0.25f); _resultKind = ResultKind.Bad; }
            ApplyColor(flash);
            _resultTimer = _resultSeconds;
        }

        private void OnLeft(CustomerEntity c, bool happy)
        {
            if (happy) return;
            _resultKind = ResultKind.Bad;
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
                float progress = 1f - Mathf.Clamp01(_resultTimer / _resultSeconds);
                AnimateResult(_resultKind, progress);

                if (_resultTimer <= 0f)
                {
                    _resultKind = ResultKind.None;
                    if (_indicator != null)
                    {
                        _indicator.localScale = _indicatorBaseScale;
                        _indicator.localPosition = Vector3.zero;
                    }
                    if (_customer == null) SetVisible(false);
                    else
                    {
                        ApplyOrbColor(_drinkColor);
                        SetGauge(_levelRatio); // back to the per-ingredient segments
                    }
                }
            }
        }

        /// <summary>Non-colour tell for the serve result: each outcome gets its own motion on the
        /// indicator (scale pulse count, or a shake) so the result reads without relying on hue.</summary>
        private void AnimateResult(ResultKind kind, float t)
        {
            if (_indicator == null) return;
            switch (kind)
            {
                case ResultKind.Perfect: // one big pulse
                    float pulse = Mathf.Sin(t * Mathf.PI) * 0.4f;
                    _indicator.localScale = _indicatorBaseScale * (1f + pulse);
                    _indicator.localPosition = Vector3.zero;
                    break;
                case ResultKind.Partial: // two short pulses
                    float doublePulse = Mathf.Abs(Mathf.Sin(t * Mathf.PI * 2f)) * 0.2f;
                    _indicator.localScale = _indicatorBaseScale * (1f + doublePulse);
                    _indicator.localPosition = Vector3.zero;
                    break;
                case ResultKind.Bad: // rapid shake, decaying out
                    float shakeX = Mathf.Sin(t * Mathf.PI * 24f) * 0.03f * (1f - t);
                    _indicator.localScale = _indicatorBaseScale;
                    _indicator.localPosition = new Vector3(shakeX, 0f, 0f);
                    break;
                default:
                    _indicator.localScale = _indicatorBaseScale;
                    _indicator.localPosition = Vector3.zero;
                    break;
            }
        }

        // --- Procedural visuals ---------------------------------------------------------------

        private void EnsureVisuals()
        {
            if (_indicator != null) return;

            _unlitShader = Shader.Find("Universal Render Pipeline/Unlit")
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
            _indicatorBaseScale = _indicator.localScale;

            // Orb
            var orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            orb.name = "OrderOrb";
            StripCollider(orb);
            orb.transform.SetParent(_indicator, false);
            orb.transform.localScale = Vector3.one * _orbSize;
            _orbMat = new Material(_unlitShader);
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
            var bgMat = new Material(_unlitShader);
            SetMatColor(bgMat, new Color(0.1f, 0.1f, 0.12f, 1f));
            bg.GetComponent<Renderer>().sharedMaterial = bgMat;
        }

        /// <summary>Grows/shrinks the pool of gauge-fill cubes to match the recipe's ingredient count.</summary>
        private void EnsureGaugeSegmentCount(int count)
        {
            while (_gaugeSegments.Count < count)
            {
                var fill = GameObject.CreatePrimitive(PrimitiveType.Cube);
                fill.name = "GaugeFill" + _gaugeSegments.Count;
                StripCollider(fill);
                fill.transform.SetParent(_indicator, false);
                var mat = new Material(_unlitShader);
                fill.GetComponent<Renderer>().sharedMaterial = mat;
                _gaugeSegments.Add(fill.transform);
                _gaugeSegmentMats.Add(mat);
            }
            while (_gaugeSegments.Count > count)
            {
                int last = _gaugeSegments.Count - 1;
                if (_gaugeSegments[last] != null) Destroy(_gaugeSegments[last].gameObject);
                _gaugeSegments.RemoveAt(last);
                _gaugeSegmentMats.RemoveAt(last);
            }
        }

        /// <summary>Stacks one coloured band per recipe ingredient, bottom-up, each sized to its
        /// share of the requested fill (ratio) times its share of the recipe (segment ratio) — so a
        /// two-ingredient order reads as two distinct colours instead of one blended colour.</summary>
        private void SetGauge(float ratio)
        {
            if (_indicator == null) return;
            ratio = Mathf.Clamp01(ratio);
            EnsureGaugeSegmentCount(_segments.Length);

            float y = _gaugeBottomY;
            for (int i = 0; i < _gaugeSegments.Count; i++)
            {
                float segRatio = i < _segments.Length ? _segments[i].ratio : 0f;
                float fh = Mathf.Max(0f, _gaugeHeight * ratio * segRatio);
                var t = _gaugeSegments[i];
                t.localScale = new Vector3(_gaugeFillW, Mathf.Max(0.0001f, fh), 0.013f);
                t.localPosition = new Vector3(_gaugeX, y + fh * 0.5f, 0f);
                SetMatColor(_gaugeSegmentMats[i], i < _segments.Length ? _segments[i].color : Color.white);
                y += fh;
            }
        }

        private void ApplyOrbColor(Color c)
        {
            if (_orbMat != null) SetMatColor(_orbMat, c);
        }

        /// <summary>Flashes the orb AND every gauge segment to one uniform colour — used for the
        /// serve-result flash, where a single alert colour (not per-ingredient) is the point.</summary>
        private void ApplyColor(Color c)
        {
            ApplyOrbColor(c);
            foreach (var m in _gaugeSegmentMats) SetMatColor(m, c);
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
