namespace Services.Atmosphere
{
    /// <summary>
    /// Drives scene mood (post-processing grading, bloom, bar lights, night transition blink) from
    /// game state. Autonomous: it subscribes to IGameStateService itself, so there's nothing to call.
    /// The interface exists only so it can be registered with the ServiceLocator.
    /// </summary>
    public interface IAtmosphereService : IGameService
    {
        /// <summary>
        /// Hold the screen black for <paramref name="holdSeconds"/> then fade in over
        /// <paramref name="fadeSeconds"/>. Used to cover the device-clock warm-up right after a scene load.
        /// </summary>
        void CoverFadeIn(float holdSeconds, float fadeSeconds);
    }
}
