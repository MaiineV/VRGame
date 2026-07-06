namespace Gameplay.Interactions
{
    /// <summary>
    /// Optional gate a grabbable object can expose to veto being picked up right now.
    /// <see cref="SimpleVRGrabber"/> skips any candidate whose <see cref="CanGrab"/> is false.
    /// Used by for-sale bottles: a locked bottle the player can't afford is visible but not grabbable.
    /// </summary>
    public interface IGrabGate
    {
        bool CanGrab { get; }
    }
}
