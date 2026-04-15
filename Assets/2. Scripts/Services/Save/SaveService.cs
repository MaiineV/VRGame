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

        private string _path;
        private string _tempPath;

        public SaveData Current { get; private set; }

        public void Initialize()
        {
            _path = Path.Combine(Application.persistentDataPath, FileName);
            _tempPath = _path + TempSuffix;
            Current = Load() ?? new SaveData();
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

        public void ResetToDefaults()
        {
            Current = new SaveData();
            Save();
        }

        private SaveData Load()
        {
            if (!File.Exists(_path)) return null;
            try
            {
                var json = File.ReadAllText(_path);
                var data = JsonUtility.FromJson<SaveData>(json);
                if (data == null) return null;
                if (data.version != SaveData.CurrentVersion)
                {
                    MyLogger.LogWarning($"[SaveService] Save file version {data.version} != current {SaveData.CurrentVersion}. Resetting.");
                    return null;
                }
                return data;
            }
            catch (System.Exception ex)
            {
                MyLogger.LogError($"[SaveService] Load failed: {ex.Message}. Starting fresh.");
                return null;
            }
        }
    }
}
