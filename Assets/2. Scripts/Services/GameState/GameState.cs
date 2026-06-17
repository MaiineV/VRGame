namespace Services.GameState
{
    public enum GameState : byte
    {
        Boot = 0,
        Idle = 10,          // pre-night, lobby / bar ready
        NightRunning = 20,  // night started, customers spawning
        NightSummary = 30,  // night ended, showing results
    }
}
