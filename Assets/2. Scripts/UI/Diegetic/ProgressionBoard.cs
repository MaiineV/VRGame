using Data.Enums;
using Services;
using Services.Audio;
using Services.Economy;
using Services.GameState;
using Services.Save;
using Services.UpdateService;
using TMPro;
using UnityEngine;

namespace UI.Diegetic
{
    /// <summary>
    /// Single diegetic money/progress board. Shows night number, cash, record and stars between
    /// nights (from the save), and — absorbing the old CashRegister role — updates cash live during
    /// the night via IEconomyService, with a "+N"/"-N" flash pop and sale/expense sfx.
    /// </summary>
    public sealed class ProgressionBoard : MonoBehaviour, IUpdateListener
    {
        [Header("Labels")]
        [SerializeField] private TMP_Text _nightLabel;
        [SerializeField] private TMP_Text _cashLabel;
        [SerializeField] private TMP_Text _bestLabel;
        [SerializeField] private TMP_Text _starsLabel;

        [Header("Cash flash (optional)")]
        [SerializeField] private TMP_Text _flashLabel; // optional "+5" / "-50" pop
        [SerializeField] private float _flashSeconds = 1.2f;
        [SerializeField] private Color _saleColor = new Color(0.5f, 1f, 0.5f);
        [SerializeField] private Color _expenseColor = new Color(1f, 0.4f, 0.4f);

        [Header("Star Milestones")]
        [SerializeField] private int _nightsPerStar = 3;
        [SerializeField] private int _maxStars = 5;
        [SerializeField] private string _filledStar = "*";
        [SerializeField] private string _emptyStar = ".";

        private IGameStateService _state;
        private ISaveService _save;
        private IEconomyService _economy;
        private IUpdateService _updates;
        private IAudioService _audio;
        private float _flashTimer;
        private bool _ticking;

        void OnEnable()
        {
            ServiceLocator.TryGet<ISaveService>(out _save);
            ServiceLocator.TryGet<IUpdateService>(out _updates);
            ServiceLocator.TryGet<IAudioService>(out _audio);

            if (ServiceLocator.TryGet<IGameStateService>(out _state))
                _state.StateChanged += OnStateChanged;

            if (ServiceLocator.TryGet<IEconomyService>(out _economy))
            {
                _economy.CashChanged += HandleCashChanged;
                _economy.SaleRegistered += HandleSale;
                _economy.ExpenseRegistered += HandleExpense;
            }

            if (_flashLabel != null) _flashLabel.text = string.Empty;
            Refresh();
        }

        void OnDisable()
        {
            StopTicking();
            if (_state != null) _state.StateChanged -= OnStateChanged;
            if (_economy != null)
            {
                _economy.CashChanged -= HandleCashChanged;
                _economy.SaleRegistered -= HandleSale;
                _economy.ExpenseRegistered -= HandleExpense;
            }
            _state = null;
            _save = null;
            _economy = null;
            _updates = null;
            _audio = null;
        }

        private void OnStateChanged(GameState from, GameState to)
        {
            if (to == GameState.Idle || to == GameState.NightSummary)
                Refresh();
        }

        private void Refresh()
        {
            if (_save == null) return;
            var d = _save.Current;

            if (_nightLabel != null) _nightLabel.text = $"Noche {d.nightsCompleted + 1}";
            if (_cashLabel != null) _cashLabel.text = $"${(_economy != null ? _economy.Cash : d.cash)}";
            if (_bestLabel != null) _bestLabel.text = $"Record: ${d.bestNightEarnings}";
            if (_starsLabel != null) _starsLabel.text = BuildStars(d.nightsCompleted);
        }

        // --- Live cash (absorbed from CashRegister) ---------------------------------------------

        private void HandleCashChanged(int cash)
        {
            if (_cashLabel != null) _cashLabel.text = $"${cash}";
        }

        private void HandleSale(RecipeId recipe, int gross)
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

        private string BuildStars(int nightsCompleted)
        {
            int earned = _nightsPerStar > 0
                ? Mathf.Min(nightsCompleted / _nightsPerStar, _maxStars)
                : 0;
            var sb = new System.Text.StringBuilder(_maxStars * 2);
            for (int i = 0; i < _maxStars; i++)
                sb.Append(i < earned ? _filledStar : _emptyStar);
            return sb.ToString();
        }
    }
}
