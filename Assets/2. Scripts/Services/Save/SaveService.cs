using System.IO;
using UnityEngine;
using Utilities;

namespace Services.Save
{
    /// <summary>
    /// JSON-on-disk save under Application.persistentDataPath. Writes are atomic
    /// (temp file + File.Replace) so a crash mid-save can't leave a truncated file.
    /// No async: saves happen at night boundaries only, not on the hot path.
    /// </summary>
    public sealed class SaveService : ISaveService
    {
        private const string FileName = "save.json";
        private const string TempSuffix = ".tmp";

        // Seed cash for a brand-new game so the first day shop is usable: enough to buy the starter
        // purchasable bottles by grabbing them. Without it the player would start with $0 and be unable
        // to unlock any bottle. Migrated saves keep their own cash.
        private const int StartingCash = 300;

        private string _path;
        private string _tempPath;

        public SaveData Current { get; private set; }

        public bool HasSave => !string.IsNullOrEmpty(_path) && System.IO.File.Exists(_path);

        public void Initialize()
        {
            _path = Path.Combine(Application.persistentDataPath, FileName);
            _tempPath = _path + TempSuffix;
            Current = Load() ?? new SaveData { cash = StartingCash };
        }

        public void NewGame()
        {
            try
            {
                if (File.Exists(_path)) File.Delete(_path);
            }
            catch (System.Exception ex)
            {
                MyLogger.LogError($"[SaveService] NewGame delete failed: {ex.Message}");
            }

            Current = new SaveData { cash = StartingCash };

            // Do NOT call Save() here: the save file will be written at the next night boundary,
            // consistent with how the game normally persists progress. Writing immediately would
            // make HasSave true again right away, defeating the "no prior save" check in the menu.
            MyLogger.LogInfo("[SaveService] New game started (save wiped).");
        }

        public void Save()
        {
            if (Current == null) Current = new SaveData();
            Current.version = SaveData.CurrentVersion;

            try
            {
                var json = JsonUtility.ToJson(Current);
                File.WriteAllText(_tempPath, json);

                if (File.Exists(_path))
                    File.Replace(_tempPath, _path, null);
                else
                    File.Move(_tempPath, _path);
            }
            catch (System.Exception ex)
            {
                MyLogger.LogError($"[SaveService] Save failed: {ex.Message}");
                if (File.Exists(_tempPath))
                {
                    try { File.Delete(_tempPath); } catch { /* ignored */ }
                }
            }
        }

        private SaveData Load()
        {
            if (!File.Exists(_path)) return null;
            try
            {
                var json = File.ReadAllText(_path);
                var data = JsonUtility.FromJson<SaveData>(json);
                if (data == null) return null;

                if (data.version > SaveData.CurrentVersion)
                {
                    // Future save from a newer build: we can't understand it. Reset rather than corrupt.
                    MyLogger.LogWarning($"[SaveService] Save file version {data.version} > current {SaveData.CurrentVersion}. Resetting.");
                    return null;
                }

                if (data.version < SaveData.CurrentVersion)
                    Migrate(data);

                return data;
            }
            catch (System.Exception ex)
            {
                MyLogger.LogError($"[SaveService] Load failed: {ex.Message}. Starting fresh.");
                return null;
            }
        }

        /// <summary>
        /// Upgrades an older save in place to <see cref="SaveData.CurrentVersion"/> with no data loss.
        /// JsonUtility already populated absent fields (the v2 unlock/stock lists) with empty
        /// defaults, so v1→v2 only needs to stamp the version. Starter unlocks are seeded later by
        /// ProgressionService when it detects an empty unlocked set.
        /// </summary>
        private static void Migrate(SaveData data)
        {
            int from = data.version;
            data.unlockedRecipes ??= new System.Collections.Generic.List<int>();
            data.unlockedBottles ??= new System.Collections.Generic.List<int>();
            data.stock ??= new System.Collections.Generic.List<StockEntry>();

            // v3→v4: wipe per-instance bottle ownership. Old v3 saves accumulated stale owned instance
            // ids (a prior respawn bug marked for-sale bottles owned), which made purchasable bottles
            // come back free without payment. Resetting returns them to "for sale"; purchases made after
            // the migration persist normally (buy once, owned forever).
            if (from < 4)
                data.ownedBottleInstances = new System.Collections.Generic.List<int>();

            data.version = SaveData.CurrentVersion;
            MyLogger.LogInfo($"[SaveService] Migrated save v{from} -> v{SaveData.CurrentVersion}.");
        }
    }
}
