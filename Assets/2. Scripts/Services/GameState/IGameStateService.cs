using Data.SO;

namespace Services.GameState
{
    public interface IGameStateService : IGameService
    {
        GameState Current { get; }
        NightConfigSO PendingConfig { get; }

        /// <summary>Stage a config for the next night (e.g. MVP config loaded from Resources).</summary>
        void SetPendingConfig(NightConfigSO config);

        /// <summary>Transition Idle → NightRunning using PendingConfig.</summary>
        void BeginNight();

        /// <summary>Force EndNight early (e.g. player quits).</summary>
        void AbortNight();

        /// <summary>Acknowledge the summary and return to Idle for the next run.</summary>
        void AcknowledgeSummary();

        event System.Action<GameState, GameState> StateChanged; // (from, to)
    }
}
