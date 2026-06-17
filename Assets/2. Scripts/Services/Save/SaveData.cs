namespace Services.Save
{
    /// <summary>
    /// On-disk progression state. Keep fields flat and JsonUtility-friendly
    /// (no properties, no Dictionary, no nullable). Bump <see cref="version"/>
    /// whenever the shape changes so old files can be migrated or discarded.
    /// </summary>
    [System.Serializable]
    public sealed class SaveData
    {
        public int version = CurrentVersion;
        public int cash;
        public int nightsCompleted;
        public int bestNightEarnings;

        public const int CurrentVersion = 1;
    }
}
