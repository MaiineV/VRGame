using Data.Enums;
using Gameplay.Customer;
using Services;
using Services.UpdateService;
using TMPro;
using UnityEngine;

namespace UI.Diegetic
{
    public sealed class ServeFeedbackDisplay : MonoBehaviour, IUpdateListener
    {
        [SerializeField] private CustomerSeatPoint _seat;
        [SerializeField] private GameObject _popupRoot;
        [SerializeField] private TMP_Text _resultLabel;

        [Header("Correct")]
        [SerializeField] private string _correctText = "OK!";
        [SerializeField] private Color _correctColor = new Color(0.2f, 1f, 0.3f, 1f);

        [Header("Wrong")]
        [SerializeField] private string _wrongText = "MAL";
        [SerializeField] private Color _wrongColor = new Color(1f, 0.3f, 0.2f, 1f);

        [Header("Timing")]
        [SerializeField] private float _displayDuration = 2f;
        [SerializeField] private float _floatSpeed = 0.3f;

        private CustomerEntity _customer;
        private float _timer;
        private bool _ticking;
        private Vector3 _startPos;

        void Awake()
        {
            if (_popupRoot != null)
            {
                _startPos = _popupRoot.transform.localPosition;
                _popupRoot.SetActive(false);
            }
        }

        void OnEnable()
        {
            if (_seat == null) return;
            _seat.CustomerBound += HandleBound;
            _seat.CustomerCleared += HandleCleared;
            if (_seat.CurrentCustomer != null) HandleBound(_seat.CurrentCustomer);
        }

        void OnDisable()
        {
            if (_seat != null)
            {
                _seat.CustomerBound -= HandleBound;
                _seat.CustomerCleared -= HandleCleared;
            }
            if (_customer != null) _customer.Served -= HandleServed;
            _customer = null;
            StopTicking();
        }

        public void MyUpdate()
        {
            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                Hide();
                StopTicking();
                return;
            }

            if (_popupRoot != null)
            {
                var pos = _startPos;
                pos.y += (_displayDuration - _timer) * _floatSpeed;
                _popupRoot.transform.localPosition = pos;

                float alpha = Mathf.Clamp01(_timer / 0.5f);
                if (_resultLabel != null)
                {
                    var c = _resultLabel.color;
                    c.a = alpha;
                    _resultLabel.color = c;
                }
            }
        }

        private void HandleBound(CustomerEntity c)
        {
            _customer = c;
            _customer.Served += HandleServed;
        }

        private void HandleCleared()
        {
            if (_customer != null) _customer.Served -= HandleServed;
            _customer = null;
            Hide();
            StopTicking();
        }

        private void HandleServed(CustomerEntity entity, RecipeId recipe, float score, bool isExact)
        {
            if (_popupRoot == null || _resultLabel == null) return;

            _resultLabel.text = isExact ? _correctText : _wrongText;
            _resultLabel.color = isExact ? _correctColor : _wrongColor;
            _popupRoot.transform.localPosition = _startPos;
            _popupRoot.SetActive(true);
            _timer = _displayDuration;
            StartTicking();
        }

        private void Hide()
        {
            if (_popupRoot != null) _popupRoot.SetActive(false);
        }

        private void StartTicking()
        {
            if (_ticking) return;
            if (!ServiceLocator.TryGet<IUpdateService>(out var svc)) return;
            svc.AddUpdateListener(this);
            _ticking = true;
        }

        private void StopTicking()
        {
            if (!_ticking) return;
            if (ServiceLocator.TryGet<IUpdateService>(out var svc))
                svc.RemoveUpdateListener(this);
            _ticking = false;
        }
    }
}
