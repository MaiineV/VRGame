using Data.Enums;
using UnityEngine;

namespace Data.SO
{
    /// <summary>
    /// Local id->color cache so LiquidRenderer can resolve without hitting the database service.
    /// Inject the same palette asset into every LiquidRenderer in the scene (or one per container prefab).
    /// </summary>
    [CreateAssetMenu(menuName = "Pour Decisions/Ingredient Palette", fileName = "IngredientPalette")]
    public sealed class IngredientPalette : ScriptableObject
    {
        [System.Serializable]
        public struct Entry
        {
            public IngredientId id;
            public Color color;
        }

        [SerializeField] private Entry[] _entries;

        // Flat O(1) lookup indexed by (byte)IngredientId. ~1KB, built lazily once.
        private Color[] _cache;

        private void BuildCache()
        {
            _cache = new Color[256];
            for (int i = 0; i < _cache.Length; i++) _cache[i] = Color.white;
            if (_entries == null) return;
            for (int i = 0; i < _entries.Length; i++)
                _cache[(byte)_entries[i].id] = _entries[i].color;
        }

        public Color GetColor(IngredientId id)
        {
            if (_cache == null) BuildCache();
            return _cache[(byte)id];
        }

        void OnEnable() => _cache = null; // rebuild after domain reload / editor edits
    }
}
