using Data.Enums;

namespace HSM.Events
{
    public sealed class CustomerOrderPlacedEvent : GameEvent
    {
        public int SeatIndex { get; }
        public RecipeId Recipe { get; }
        public CustomerOrderPlacedEvent(int seatIndex, RecipeId recipe)
        {
            SeatIndex = seatIndex;
            Recipe = recipe;
        }
    }

    public sealed class CustomerServedEvent : GameEvent
    {
        public int SeatIndex { get; }
        public RecipeId Recipe { get; }
        public float Score { get; }
        public bool IsExact { get; }
        public CustomerServedEvent(int seatIndex, RecipeId recipe, float score, bool isExact)
        {
            SeatIndex = seatIndex;
            Recipe = recipe;
            Score = score;
            IsExact = isExact;
        }
    }

    public sealed class CustomerLeftEvent : GameEvent
    {
        public int SeatIndex { get; }
        public bool Happy { get; }
        public CustomerLeftEvent(int seatIndex, bool happy)
        {
            SeatIndex = seatIndex;
            Happy = happy;
        }
    }
}
