using Data.Enums;
using UnityEngine;

namespace Data.SO
{
    /// <summary>
    /// Flat SfxId → clip config lookup. Built once into a 256-slot array so the
    /// audio service resolves without hashmap cost on the hot path.
    /// </summary>
    [CreateAssetMenu(menuName = "Pour Decisions/Sfx Database", fileName = "SfxDatabase")]
    public sealed class SfxDatabase : ScriptableObject
    {
        [System.Serializable]
        public struct Entry
        {
            public SfxId id;
            public AudioClip clip;
            [Range(0f, 1f)] public float volume;
            [Range(0f, 2f)] public float pitchMin;
            [Range(0f, 2f)] public float pitchMax;
            [Tooltip("0 = 2D/UI, 1 = fully 3D spatialized.")]
            [Range(0f, 1f)] public float spatialBlend;
            [Tooltip("Min seconds between retriggers to avoid stacking on spam (one-shots only).")]
            public float minRetriggerInterval;
            [Tooltip("Only used by loop API: source keeps playing until StopLoop.")]
            public bool loop;
        }

        [SerializeField] private Entry[] _entries;

        private Entry[] _cache;

        private void BuildCache()
        {
            _cache = new Entry[256];
            if (_entries == null) return;
            for (int i = 0; i < _entries.Length; i++)
                _cache[(byte)_entries[i].id] = _entries[i];
        }

        public bool TryGet(SfxId id, out Entry entry)
        {
            if (_cache == null) BuildCache();
            entry = _cache[(byte)id];
            return entry.clip != null;
        }

        void OnEnable() => _cache = null;
    }
}
