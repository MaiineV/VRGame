using Data.SO;

namespace Services.Night
{
    public interface INightService : IGameService
    {
        bool IsRunning { get; }
        float TimeRemaining { get; }
        int ActiveCustomers { get; }

        void StartNight(NightConfigSO config);
        void EndNight();

        event System.Action NightStarted;
        event System.Action NightEnded;
    }
}
