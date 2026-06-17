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
            Transition(GameState.Idle);
        }

        public void SetPendingConfig(NightConfigSO config) => PendingConfig = config;

        public void BeginNight()
        {
            if (Current != GameState.Idle) { MyLogger.LogWarning("[GameState] BeginNight ignored: not Idle."); return; }
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
            Transition(GameState.Idle);
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
