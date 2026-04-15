using Data.Enums;
using Services;
using Services.Audio;
using Services.Economy;
using Services.UpdateService;
using TMPro;
using UnityEngine;

namespace Gameplay.CashRegister
{
    /// <summary>
    /// Diegetic cash display: world-mounted TMP label that mirrors IEconomyService.Cash.
    /// Updates only on CashChanged — no per-frame polling. Flash pop ticks via
    /// IUpdateService on-demand (only while a flash is active).
    /// </summary>
    public sealed class CashRegister : MonoBehaviour, IUpdateListener
    {
        [SerializeField] private TMP_Text _cashLabel;
        [SerializeField] private TMP_Text _flashLabel; // optional "+5" / "-50" pop
        [SerializeField] private string _prefix = "$ ";
        [SerializeField] private float _flashSeconds = 1.2f;
        [SerializeField] private Color _saleColor = new Color(0.5f, 1f, 0.5f);
        [SerializeField] private Color _expenseColor = new Color(1f, 0.4f, 0.4f);

        private IEconomyService _economy;
        private IUpdateService _updates;
        private IAudioService _audio;
        private float _flashTimer;
        private bool _ticking;

        void OnEnable()
        {
            ServiceLocator.TryGet<IUpdateService>(out _updates);
            ServiceLocator.TryGet<IAudioService>(out _audio);
            if (!ServiceLocator.TryGet<IEconomyService>(out _economy)) return;
            _economy.CashChanged += HandleCashChanged;
            _economy.SaleRegistered += HandleSale;
            _economy.ExpenseRegistered += HandleExpense;
            HandleCashChanged(_economy.Cash);
            if (_flashLabel != null) _flashLabel.text = string.Empty;
        }

        void OnDisable()
        {
            StopTicking();
            if (_economy != null)
            {
                _economy.CashChanged -= HandleCashChanged;
                _economy.SaleRegistered -= HandleSale;
                _economy.ExpenseRegistered -= HandleExpense;
            }
            _economy = null;
            _updates = null;
            _audio = null;
        }

        public void MyUpdate()
        {
            if (_flashTimer <= 0f || _flashLabel == null) { StopTicking(); return; }
            _flashTimer -= Time.deltaTime;
            if (_flashTimer <= 0f)
            {
                _flashLabel.text = string.Empty;
                StopTicking();
            }
        }

        private void StartTicking()
        {
            if (_ticking || _updates == null) return;
            _updates.AddUpdateListener(this);
            _ticking = true;
        }

        private void StopTicking()
        {
            if (!_ticking || _updates == null) return;
            _updates.RemoveUpdateListener(this);
            _ticking = false;
        }

        private void HandleCashChanged(int cash)
        {
            if (_cashLabel != null) _cashLabel.text = _prefix + cash;
        }

        private void HandleSale(Data.Enums.RecipeId recipe, int gross)
        {
            _audio?.PlayOneShot(SfxId.CashSale, transform.position);
            if (_flashLabel == null) return;
            _flashLabel.color = _saleColor;
            _flashLabel.text = "+" + gross;
            _flashTimer = _flashSeconds;
            StartTicking();
        }

        private void HandleExpense(int amount, string reason)
        {
            _audio?.PlayOneShot(SfxId.CashExpense, transform.position);
            if (_flashLabel == null) return;
            _flashLabel.color = _expenseColor;
            _flashLabel.text = "-" + amount;
            _flashTimer = _flashSeconds;
            StartTicking();
        }
    }
}
