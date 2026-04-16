using Gameplay.Liquid;
using Services;
using Services.UpdateService;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Diegetic
{
    /// <summary>
    /// World-space fill bar floating above a LiquidContainer (glass/shaker).
    /// Billboard-faces the main camera each frame.
    /// </summary>
    public sealed class GlassFillBar : MonoBehaviour, IUpdateListener
    {
        [SerializeField] private LiquidContainer _container;
        [SerializeField] private Image _fillImage;
        [SerializeField] private Color _emptyColor = new Color(0.4f, 0.6f, 1f);
        [SerializeField] private Color _fullColor = new Color(0.2f, 0.9f, 0.3f);
        [SerializeField] private Color _overflowColor = new Color(1f, 0.3f, 0.2f);

        private bool _registered;
        private Transform _cam;

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

            float fill = _container.FillRatio;
            _fillImage.fillAmount = fill;

            if (fill >= 0.95f)
                _fillImage.color = _overflowColor;
            else
                _fillImage.color = Color.Lerp(_emptyColor, _fullColor, fill);

            if (_cam != null)
                transform.rotation = Quaternion.LookRotation(transform.position - _cam.position);
        }
    }
}
