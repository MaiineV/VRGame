using Gameplay.Interactions;
using Gameplay.Liquid;
using Services;
using Services.UpdateService;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Diegetic
{
    /// <summary>
    /// World-space vertical fill bar floating above a LiquidContainer (glass/shaker). The bar height
    /// shows how full the glass is (0..1). When the glass is sitting in a serve socket, a marker line
    /// (the "tope") shows the level the customer requested — the player tops up until the fill reaches
    /// the marker, mirroring the customer's own gauge. The fill no longer encodes the level by colour;
    /// it just tints green once the current bucket matches the request as a confirmation.
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
        [SerializeField] private Color _fillColor = new Color(0.55f, 0.8f, 1f);
        [SerializeField] private Color _matchColor = new Color(0.3f, 1f, 0.4f);

        private bool _registered;
        private Transform _cam;
        private Glass _glass;

        void Awake()
        {
            if (_container == null) _container = GetComponentInParent<LiquidContainer>();
            _glass = _container as Glass ?? GetComponentInParent<Glass>();
        }

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
            _fillImage.fillAmount = fill;

            int? req = _glass != null ? _glass.RequestedLevel : null;

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
                }
            }

            bool match = req.HasValue && FillLevels.BucketOf(fill) == FillLevels.Clamp(req.Value);
            _fillImage.color = match ? _matchColor : _fillColor;

            if (_cam == null && Camera.main != null) _cam = Camera.main.transform;
            if (_cam != null)
                transform.rotation = Quaternion.LookRotation(transform.position - _cam.position);
        }
    }
}
