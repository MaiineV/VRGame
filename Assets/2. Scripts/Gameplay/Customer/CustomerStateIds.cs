namespace Gameplay.Customer
{
    public enum CustomerStateId
    {
        Approaching = 10,
        Waiting = 20,
        // After being served, the customer carries the glass to a free table.
        GoingToTable = 30,
        // At the table: plays the drink animation while the drink timer runs down.
        Drinking = 33,
        // Fallback "drink in place" used when no table point is free/authored.
        Wandering = 35,
        Leaving = 40
    }
}
