namespace Data.Enums
{
    /// <summary>
    /// Identifies a short-lived particle burst. Each maps to a pre-built, pooled ParticleSystem
    /// in VfxService (configured in code — no prefab/editor authoring).
    /// </summary>
    public enum VfxId : byte
    {
        None         = 0,
        Splash       = 10,   // liquid droplets where a pour stream lands
        Shatter      = 20,   // glass/bottle break burst
        ServeSuccess = 30,   // sparkle above a correctly served customer
        Coins        = 40,   // gold burst on a sale
        ServeFail    = 50,   // red puff above a rejected serve (visual fail cue, not audio-only)
    }
}
