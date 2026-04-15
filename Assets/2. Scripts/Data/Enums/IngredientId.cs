namespace Data.Enums
{
    public enum IngredientId : byte
    {
        None    = 0,
        Vodka   = 10,
        Gin     = 20,
        Rum     = 30,
        Whiskey = 40,
        Tequila = 50,
        Tonic   = 100,
        Cola    = 110,
        Lime    = 120,
        Sugar   = 130,
        Ice     = 140,
    }

    public enum IngredientType : byte
    {
        Alcohol = 10,
        Mixer   = 20,
        Garnish = 30,
    }
}
