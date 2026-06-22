namespace Services.Save
{
    public interface ISaveService : IGameService
    {
        /// <summary>Live data instance. Mutate directly and call <see cref="Save"/>.</summary>
        SaveData Current { get; }

        /// <summary>Write the current data to disk atomically.</summary>
        void Save();
    }
}
