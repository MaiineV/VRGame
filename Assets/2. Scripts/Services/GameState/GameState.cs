namespace Services.GameState
{
    public enum GameState : byte
    {
        Boot = 0,
        Idle = 10,          // legacy pre-night lobby; flow now routes through DayShop instead
        DayShop = 15,       // between nights: buy stock / unlock drinks, then start the next night
        NightRunning = 20,  // night started, customers spawning
        NightSummary = 30,  // night ended, showing results
    }
}
