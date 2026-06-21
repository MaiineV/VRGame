namespace Services.Atmosphere
{
    /// <summary>
    /// Drives scene mood (post-processing grading, bloom, bar lights, night transition blink) from
    /// game state. Autonomous: it subscribes to IGameStateService itself, so there's nothing to call.
    /// The interface exists only so it can be registered with the ServiceLocator.
    /// </summary>
    public interface IAtmosphereService : IGameService
    {
    }
}
