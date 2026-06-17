using System;
using Data.Enums;
using UnityEngine;

namespace Gameplay.Liquid
{
    /// <summary>
    /// Zero-alloc mixture of ingredients. Allocated once per container (Glass/Shaker) on Awake.
    /// Max 8 ingredients — overflow is ignored. All mutation reuses the same arrays.
    /// </summary>
    public sealed class LiquidMix
    {
        public const int Capacity = 8;

        private readonly IngredientId[] _ids = new IngredientId[Capacity];
        private readonly float[] _volumes = new float[Capacity];
        private int _count;
        private float _totalMl;

        public int Count => _count;
        public float TotalMl => _totalMl;
        public bool IsEmpty => _count == 0;

        public IngredientId IdAt(int index) => _ids[index];
        public float VolumeAt(int index) => _volumes[index];

        public void Add(IngredientId id, float volumeMl)
        {
            if (volumeMl <= 0f || id == IngredientId.None) return;

            for (int i = 0; i < _count; i++)
            {
                if (_ids[i] == id)
                {
                    _volumes[i] += volumeMl;
                    _totalMl += volumeMl;
                    return;
                }
            }

            if (_count >= Capacity) return;

            _ids[_count] = id;
            _volumes[_count] = volumeMl;
            _count++;
            _totalMl += volumeMl;
        }

        public float VolumeOf(IngredientId id)
        {
            for (int i = 0; i < _count; i++)
                if (_ids[i] == id) return _volumes[i];
            return 0f;
        }

        /// <summary>Ingredient holding the most volume (the "main" drink), or None if empty.</summary>
        public IngredientId DominantId()
        {
            int best = -1;
            float bestVol = 0f;
            for (int i = 0; i < _count; i++)
            {
                if (_volumes[i] > bestVol) { bestVol = _volumes[i]; best = i; }
            }
            return best >= 0 ? _ids[best] : IngredientId.None;
        }

        public void Clear()
        {
            for (int i = 0; i < _count; i++)
            {
                _ids[i] = IngredientId.None;
                _volumes[i] = 0f;
            }
            _count = 0;
            _totalMl = 0f;
        }

        /// <summary>Weighted color blend. Caller supplies id -> Color lookup (from database).</summary>
        public Color BlendColor(Func<IngredientId, Color> resolve)
        {
            if (_count == 0 || _totalMl <= 0f) return new Color(0f, 0f, 0f, 0f);

            float r = 0f, g = 0f, b = 0f, a = 0f;
            for (int i = 0; i < _count; i++)
            {
                var c = resolve(_ids[i]);
                float w = _volumes[i] / _totalMl;
                r += c.r * w; g += c.g * w; b += c.b * w; a += c.a * w;
            }
            return new Color(r, g, b, a);
        }
    }
}
