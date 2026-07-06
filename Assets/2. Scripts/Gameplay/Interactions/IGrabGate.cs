namespace Gameplay.Interactions
{
    /// <summary>
    /// Optional gate a grabbable object can expose to veto being picked up right now.
    /// <see cref="GrabGateEnforcer"/> polls <see cref="CanGrab"/> and toggles the Meta Interaction
    /// SDK grab/distance-grab interactables accordingly.
    /// Used by for-sale bottles: a locked bottle the player can't afford is visible but not grabbable.
    /// </summary>
    public interface IGrabGate
    {
        bool CanGrab { get; }
    }
}
