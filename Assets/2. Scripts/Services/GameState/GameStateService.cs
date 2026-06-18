using Data.SO;
using Services.Economy;
using Services.Night;
using Services.Save;
using UnityEngine;
using Utilities;

namespace Services.GameState
{
    public sealed class GameStateService : IGameStateService
    {
        private readonly INightService _night;
        private readonly IEconomyService _economy;
        private readonly ISaveService _save;

        public GameState Current { get; private set; } = GameState.Boot;
        public NightConfigSO PendingConfig { get; private set; }

        public event System.Action<GameState, GameState> StateChanged;

        public GameStateService(INightService night, IEconomyService economy, ISaveService save)
        {
            _night = night;
            _economy = economy;
            _save = save;
        }

        public void Initialize()
        {
            _night.NightEnded += OnNightEnded;
            // Boot straight into the day shop: the player buys stock / unlocks drinks, then starts night 1.
            Transition(GameState.DayShop);
        }

        public void SetPendingConfig(NightConfigSO config) => PendingConfig = config;

        public void BeginNight()
        {
            // DayShop is the normal entry; Idle accepted for back-compat with the legacy night clipboard.
            if (Current != GameState.DayShop && Current != GameState.Idle)
            {
                MyLogger.LogWarning("[GameState] BeginNight ignored: not in DayShop.");
                return;
            }
            if (PendingConfig == null) { MyLogger.LogError("[GameState] BeginNight: no PendingConfig."); return; }

            _economy.ResetForNewNight();
            _night.StartNight(PendingConfig);
            Transition(GameState.NightRunning);
        }

        public void AbortNight()
        {
            if (Current != GameState.NightRunning) return;
            _night.EndNight();
            // OnNightEnded will drive the transition
        }

        public void AcknowledgeSummary()
        {
            if (Current != GameState.NightSummary) return;
            // After the summary the player returns to the day shop to restock / unlock before the next night.
            Transition(GameState.DayShop);
        }

        private void OnNightEnded()
        {
            if (Current != GameState.NightRunning) return;

            var data = _save.Current;
            data.cash = _economy.Cash;
            data.nightsCompleted++;
            if (_economy.NightlyEarnings > data.bestNightEarnings)
                data.bestNightEarnings = _economy.NightlyEarnings;
            _save.Save();
            MyLogger.LogInfo($"[GameState] Persisted: cash={data.cash}, nights={data.nightsCompleted}, best={data.bestNightEarnings}");

            Transition(GameState.NightSummary);
        }

        private void Transition(GameState to)
        {
            if (Current == to) return;
            var from = Current;
            Current = to;
            StateChanged?.Invoke(from, to);
            MyLogger.LogInfo($"[GameState] {from} → {to}");
        }
    }
}
