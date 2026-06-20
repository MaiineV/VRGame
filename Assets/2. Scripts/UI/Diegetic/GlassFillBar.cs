using Data.Enums;
using Data.SO;
using Gameplay.Interactions;
using Gameplay.Liquid;
using Services;
using Services.UpdateService;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Diegetic
{
    /// <summary>
    /// World-space vertical fill bar floating above a LiquidContainer (glass/shaker). It tells the
    /// player three things at a glance:
    ///   - WHAT they're pouring: the fill is tinted with the liquid's own colour (the blended mix).
    ///   - HOW MUCH: the bar height tracks the fill ratio (0..1).
    ///   - UP TO WHERE: when the glass sits in a serve socket, a marker line (the "tope") shows the
    ///     level the customer requested; it turns green once the current fill matches that bucket.
    /// Billboard-faces the main camera each frame.
    /// </summary>
    public sealed class GlassFillBar : MonoBehaviour, IUpdateListener
    {
        [SerializeField] private LiquidContainer _container;
        [SerializeField] private Image _fillImage;
        [Tooltip("Marker line showing the customer's requested level (the 'tope'). Hidden when the " +
                 "glass isn't sitting at a customer's serve socket.")]
        [SerializeField] private RectTransform _targetMarker;
        [Tooltip("Bar height in world units — must match the FillBar canvas height — used to place the marker.")]
        [SerializeField] private float _barHeight = 0.13f;
        [Tooltip("Id→colour palette used to tint the bar with the actual liquid colour. Assign the same " +
                 "palette the glass's LiquidRenderer uses.")]
        [SerializeField] private IngredientPalette _palette;
        [Tooltip("Bar colour while the glass is empty (nothing poured yet).")]
        [SerializeField] private Color _emptyColor = new Color(0.5f, 0.5f, 0.55f, 0.6f);
        [Tooltip("Marker colour while the fill doesn't yet match the requested level.")]
        [SerializeField] private Color _targetColor = new Color(1f, 0.85f, 0.2f);
        [Tooltip("Marker colour once the fill matches the requested level.")]
        [SerializeField] private Color _matchColor = new Color(0.3f, 1f, 0.4f);

        private bool _registered;
        private Transform _cam;
        private Glass _glass;
        private System.Func<IngredientId, Color> _resolve;
        private Image _targetMarkerImage;
        private RectTransform _fillRect;

        void Awake()
        {
            if (_container == null) _container = GetComponentInParent<LiquidContainer>();
            _glass = _container as Glass ?? GetComponentInParent<Glass>();
            _resolve = ResolveColor;
            if (_targetMarker != null) _targetMarkerImage = _targetMarker.GetComponent<Image>();
            if (_fillImage != null) _fillRect = _fillImage.rectTransform;
        }

        private Color ResolveColor(IngredientId id) => _palette != null ? _palette.GetColor(id) : Color.white;

        void OnEnable()
        {
            _cam = Camera.main != null ? Camera.main.transform : null;
            if (!ServiceLocator.TryGet<IUpdateService>(out var svc)) return;
            svc.AddUpdateListener(this);
            _registered = true;
        }

        void OnDisable()
        {
            if (_registered && ServiceLocator.TryGet<IUpdateService>(out var svc))
                svc.RemoveUpdateListener(this);
            _registered = false;
        }

        public void MyUpdate()
        {
            if (_container == null || _fillImage == null) return;

            float fill = Mathf.Clamp01(_container.FillRatio);
            // Size the fill by the RectTransform's top anchor (grows from the bottom). This renders the
            // amount even though the Image has no sprite — Image.fillAmount needs a sprite and silently
            // did nothing, which is why the bar showed colour but never a level.
            if (_fillRect != null) _fillRect.anchorMax = new Vector2(1f, fill);

            // Tint the bar with the liquid's actual colour so the player sees WHAT they're pouring.
            bool hasLiquid = _container.Mix != null && _container.Mix.TotalMl > 0f && _palette != null;
            Color liquid = hasLiquid ? _container.Mix.BlendColor(_resolve) : _emptyColor;
            liquid.a = 1f;
            _fillImage.color = liquid;

            int? req = _glass != null ? _glass.RequestedLevel : null;
            bool match = req.HasValue && FillLevels.BucketOf(fill) == FillLevels.Clamp(req.Value);

            if (_targetMarker != null)
            {
                bool show = req.HasValue;
                if (_targetMarker.gameObject.activeSelf != show) _targetMarker.gameObject.SetActive(show);
                if (show)
                {
                    float r = Mathf.Clamp01(FillLevels.RatioOf(req.Value));
                    var p = _targetMarker.anchoredPosition;
                    p.y = (r - 0.5f) * _barHeight; // canvas pivot is centred → 0 = middle of the bar
                    _targetMarker.anchoredPosition = p;
                    // Marker goes green when the fill reaches the requested level.
                    if (_targetMarkerImage != null) _targetMarkerImage.color = match ? _matchColor : _targetColor;
                }
            }

            if (_cam == null && Camera.main != null) _cam = Camera.main.transform;
            if (_cam != null)
                transform.rotation = Quaternion.LookRotation(transform.position - _cam.position);
        }
    }
}
