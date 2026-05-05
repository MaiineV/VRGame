using Services;
using Services.GameState;
using Services.Save;
using TMPro;
using UnityEngine;

namespace UI.Diegetic
{
    public sealed class ProgressionBoard : MonoBehaviour
    {
        [Header("Labels")]
        [SerializeField] private TMP_Text _nightLabel;
        [SerializeField] private TMP_Text _cashLabel;
        [SerializeField] private TMP_Text _bestLabel;
        [SerializeField] private TMP_Text _starsLabel;

        [Header("Star Milestones")]
        [SerializeField] private int _nightsPerStar = 3;
        [SerializeField] private int _maxStars = 5;
        [SerializeField] private string _filledStar = "*";
        [SerializeField] private string _emptyStar = ".";

        private IGameStateService _state;
        private ISaveService _save;

        void OnEnable()
        {
            ServiceLocator.TryGet<ISaveService>(out _save);
            if (ServiceLocator.TryGet<IGameStateService>(out _state))
                _state.StateChanged += OnStateChanged;
            Refresh();
        }

        void OnDisable()
        {
            if (_state != null) _state.StateChanged -= OnStateChanged;
            _state = null;
            _save = null;
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
            if (_cashLabel != null) _cashLabel.text = $"${d.cash}";
            if (_bestLabel != null) _bestLabel.text = $"Record: ${d.bestNightEarnings}";
            if (_starsLabel != null) _starsLabel.text = BuildStars(d.nightsCompleted);
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
