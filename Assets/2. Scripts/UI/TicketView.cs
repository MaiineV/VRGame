using Data.SO;
using Gameplay.Customer;
using Services;
using Services.Database;
using Services.UpdateService;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// World-space ticket over a seat. Shows active recipe and patience bar.
    /// Self-subscribes to the seat — no per-frame cost when the seat is empty.
    /// Only registers as IUpdateListener while a customer is bound.
    /// </summary>
    public sealed class TicketView : MonoBehaviour, IUpdateListener
    {
        [SerializeField] private CustomerSeatPoint _seat;
        [SerializeField] private GameObject _root;        // parent to hide when empty
        [SerializeField] private TMP_Text _recipeLabel;
        [SerializeField] private Image _patienceFill;     // Image.type = Filled, FillMethod = Horizontal
        [SerializeField] private bool _hideWhenEmpty = true;
        [SerializeField] private string _emptyLabel = "—";

        [Header("Serve Feedback")]
        [SerializeField] private Color _correctColor = new Color(0.2f, 1f, 0.3f, 0.85f);
        [SerializeField] private Color _wrongColor = new Color(1f, 0.3f, 0.2f, 0.85f);
        [SerializeField] private float _flashDuration = 1.5f;

        private CustomerEntity _customer;
        private bool _registered;
        private Image _background;
        private Color _defaultBgColor;
        private float _flashTimer;

        void OnEnable()
        {
            if (_root != null)
            {
                _background = _root.GetComponentInChildren<Image>();
                if (_background != null) _defaultBgColor = _background.color;
            }
            if (_seat == null) return;
            _seat.CustomerBound += HandleBound;
            _seat.CustomerCleared += HandleCleared;
            if (_seat.CurrentCustomer != null) HandleBound(_seat.CurrentCustomer);
            else HandleCleared();
        }

        void OnDisable()
        {
            if (_seat != null)
            {
                _seat.CustomerBound -= HandleBound;
                _seat.CustomerCleared -= HandleCleared;
            }
            UnregisterTick();
            if (_customer != null) _customer.Served -= HandleServed;
            _customer = null;
        }

        public void MyUpdate()
        {
            if (_customer == null || _patienceFill == null) return;
            float max = _customer.So != null ? _customer.So.PatienceSeconds : 0f;
            _patienceFill.fillAmount = max > 0f ? Mathf.Clamp01(_customer.WaitTimer / max) : 0f;

            if (_flashTimer > 0f)
            {
                _flashTimer -= Time.deltaTime;
                if (_flashTimer <= 0f && _background != null) _background.color = _defaultBgColor;
            }
        }

        private void HandleBound(CustomerEntity c)
        {
            _customer = c;
            _customer.Served += HandleServed;

            // Tint the ticket background to the drink colour (matches the bottle tag + orb) AND
            // spell out the order: drink name + the requested fill %, so the player can read
            // exactly what to pour without guessing the colour.
            Color drink = DrinkColorUtil.For(c.TargetRecipe);
            if (_background != null) { _defaultBgColor = drink; _background.color = drink; }
            SetOrderLabel(c);

            if (_patienceFill != null) _patienceFill.fillAmount = 1f;
            SetRootActive(true);
            RegisterTick();
        }

        private void HandleCleared()
        {
            if (_customer != null) _customer.Served -= HandleServed;
            _customer = null;
            UnregisterTick();
            if (_recipeLabel != null) _recipeLabel.text = _emptyLabel;
            if (_patienceFill != null) _patienceFill.fillAmount = 0f;
            if (_background != null) _background.color = _defaultBgColor;
            _flashTimer = 0f;
            SetRootActive(!_hideWhenEmpty);
        }

        private void HandleServed(CustomerEntity entity, Data.Enums.RecipeId recipe, float score, bool isExact)
        {
            if (_background == null) return;
            _background.color = isExact ? _correctColor : _wrongColor;
            _flashTimer = _flashDuration;
        }

        private void SetOrderLabel(CustomerEntity c)
        {
            if (_recipeLabel == null) return;

            string name = c.TargetRecipe.ToString();
            if (ServiceLocator.TryGet<IDatabaseService>(out var db))
            {
                RecipeSO so = db.GetRecipe(c.TargetRecipe);
                if (so != null && !string.IsNullOrEmpty(so.DisplayName)) name = so.DisplayName;
            }

            int percent = Gameplay.Liquid.FillLevels.PercentOf(c.TargetLevel);
            _recipeLabel.text = $"{name}\n{percent}%";
        }

        private void SetRootActive(bool active)
        {
            if (_root != null) _root.SetActive(active);
        }

        private void RegisterTick()
        {
            if (_registered) return;
            if (!ServiceLocator.TryGet<IUpdateService>(out var svc)) return;
            svc.AddUpdateListener(this);
            _registered = true;
        }

        private void UnregisterTick()
        {
            if (!_registered) return;
            if (ServiceLocator.TryGet<IUpdateService>(out var svc))
                svc.RemoveUpdateListener(this);
            _registered = false;
        }
    }
}
