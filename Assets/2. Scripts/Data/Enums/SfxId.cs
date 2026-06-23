namespace Data.Enums
{
    public enum SfxId : byte
    {
        None            = 0,
        PourLoop        = 10,
        GlassFull       = 11,
        GlassBreak      = 20,
        BottleBreak     = 21,
        GlassPlace      = 22,
        BottlePlace     = 23,
        CashSale        = 30,
        CashExpense     = 31,
        CustomerServed  = 40,
        CustomerLeft    = 41,
        DrinkSip        = 42,
        NightStart      = 50,
        NightEnd        = 51,
        ButtonPress     = 60,
        Footstep        = 70,
        GrabObject      = 80,
        ReleaseObject   = 81,
        BarAmbience     = 110,
        MusicIdle       = 120,
        MusicNight      = 130,
    }
}
