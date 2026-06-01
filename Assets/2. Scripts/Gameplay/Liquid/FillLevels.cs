using UnityEngine;

namespace Gameplay.Liquid
{
    /// <summary>
    /// Discrete fill levels for the serve mini-game. A customer asks for one level (shown as a
    /// colour); the glass is bucketed to the nearest level and counts as correct when its
    /// bucket matches the customer's. Levels and colours are index-aligned.
    ///   0 -> 30% Green, 1 -> 50% Yellow, 2 -> 70% Orange, 3 -> 100% Red.
    /// </summary>
    public static class FillLevels
    {
        public static readonly float[] Ratios = { 0.30f, 0.50f, 0.70f, 1.00f };

        public static readonly Color[] Colors =
        {
            new Color(0.25f, 0.80f, 0.30f), // 30% green
            new Color(0.95f, 0.85f, 0.20f), // 50% yellow
            new Color(0.95f, 0.55f, 0.15f), // 70% orange
            new Color(0.90f, 0.25f, 0.20f), // 100% red
        };

        public static int Count => Ratios.Length;

        /// <summary>Index of the level nearest a 0..1 fill ratio.</summary>
        public static int BucketOf(float ratio)
        {
            int best = 0;
            float bestD = Mathf.Abs(ratio - Ratios[0]);
            for (int i = 1; i < Ratios.Length; i++)
            {
                float d = Mathf.Abs(ratio - Ratios[i]);
                if (d < bestD) { bestD = d; best = i; }
            }
            return best;
        }

        public static int Clamp(int index) => Mathf.Clamp(index, 0, Ratios.Length - 1);
        public static Color ColorOf(int index) => Colors[Clamp(index)];
        public static Color ColorForRatio(float ratio) => ColorOf(BucketOf(ratio));
        public static float RatioOf(int index) => Ratios[Clamp(index)];
        public static int PercentOf(int index) => Mathf.RoundToInt(RatioOf(index) * 100f);
        public static int PercentForRatio(float ratio) => PercentOf(BucketOf(ratio));
    }
}
