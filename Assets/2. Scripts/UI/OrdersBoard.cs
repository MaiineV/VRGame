using Services;
using Services.Night;
using Services.UpdateService;
using TMPro;
using UnityEngine;

namespace UI
{
    /// <summary>
    /// Aggregate board for the whole bar. Just a header label showing night timer + KPI.
    /// Per-seat rows are handled by TicketView references (assigned in inspector).
    /// Ticks via IUpdateService only while the night is running.
    /// </summary>
    public sealed class OrdersBoard : MonoBehaviour, IUpdateListener
    {
        [SerializeField] private TMP_Text _timerLabel;
        [SerializeField] private TMP_Text _statsLabel;
        [SerializeField] private TicketView[] _rows;

        private INightService _night;
        private Services.Economy.IEconomyService _economy;
        private IUpdateService _updates;
        private bool _ticking;

        void OnEnable()
        {
            ServiceLocator.TryGet<IUpdateService>(out _updates);

            if (ServiceLocator.TryGet<INightService>(out _night))
            {
                _night.NightStarted += OnNightStarted;
                _night.NightEnded += OnNightEnded;
                if (_night.IsRunning) StartTicking();
            }
            if (ServiceLocator.TryGet<Services.Economy.IEconomyService>(out _economy))
            {
                _economy.CashChanged += OnCashChanged;
                _economy.SaleRegistered += OnSale;
            }
            Refresh();
        }

        void OnDisable()
        {
            StopTicking();
            if (_night != null) { _night.NightStarted -= OnNightStarted; _night.NightEnded -= OnNightEnded; }
            if (_economy != null) { _economy.CashChanged -= OnCashChanged; _economy.SaleRegistered -= OnSale; }
            _night = null; _economy = null; _updates = null;
        }

        public void MyUpdate()
        {
            if (_timerLabel == null || _night == null || !_night.IsRunning) return;
            int t = Mathf.CeilToInt(_night.TimeRemaining);
            int mm = t / 60, ss = t % 60;
            _timerLabel.text = $"{mm:00}:{ss:00}";
        }

        private void OnNightStarted() { StartTicking(); Refresh(); }
        private void OnNightEnded() { StopTicking(); Refresh(); }

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

        private void Refresh()
        {
            if (_statsLabel != null && _economy != null)
                _statsLabel.text = $"Sales {_economy.Sales}   Cash ${_economy.Cash}";
            if (_timerLabel != null && _night != null && !_night.IsRunning)
                _timerLabel.text = "--:--";
        }

        private void OnCashChanged(int _) => Refresh();
        private void OnSale(Data.Enums.RecipeId _, int __) => Refresh();
    }
}
