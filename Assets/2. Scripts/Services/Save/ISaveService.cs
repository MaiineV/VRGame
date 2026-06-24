namespace Services.Save
{
    public interface ISaveService : IGameService
    {
        /// <summary>Live data instance. Mutate directly and call <see cref="Save"/>.</summary>
        SaveData Current { get; }

        /// <summary>True if a save file exists on disk (a prior game was played).</summary>
        bool HasSave { get; }

        /// <summary>Write the current data to disk atomically.</summary>
        void Save();

        /// <summary>Wipes progress: deletes the save file and resets Current to a fresh game. ProgressionService re-seeds starter unlocks when it next sees an empty unlocked set.</summary>
        void NewGame();
    }
}
