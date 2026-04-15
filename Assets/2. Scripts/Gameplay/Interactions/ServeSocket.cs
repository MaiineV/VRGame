using Data.Enums;
using Gameplay.Liquid;
using Services;
using Services.Recipe;
using UnityEngine;
using Utilities;

namespace Gameplay.Interactions
{
    /// <summary>
    /// Trigger volume placed in front of a customer seat. When a Glass enters and rests,
    /// it evaluates the mix against the active ticket's target recipe and raises Served.
    /// No per-frame work — events fire on trigger enter/exit only.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class ServeSocket : MonoBehaviour
    {
        [SerializeField] private RecipeId _targetRecipe = RecipeId.None;

        public RecipeId TargetRecipe
        {
            get => _targetRecipe;
            set => _targetRecipe = value;
        }

        public Glass CurrentGlass { get; private set; }

        public event System.Action<Glass, RecipeMatch> Served;
        public event System.Action<Glass> GlassRemoved;

        void Reset()
        {
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        void OnTriggerEnter(Collider other)
        {
            var glass = other.GetComponentInParent<Glass>();
            if (glass == null || CurrentGlass == glass) return;

            CurrentGlass = glass;

            if (_targetRecipe == RecipeId.None)
            {
                MyLogger.LogWarning("[ServeSocket] No target recipe set; skipping evaluation.");
                return;
            }

            if (!ServiceLocator.TryGet<IRecipeService>(out var recipes)) return;

            var match = recipes.Evaluate(glass.Mix, _targetRecipe);
            Served?.Invoke(glass, match);
        }

        void OnTriggerExit(Collider other)
        {
            var glass = other.GetComponentInParent<Glass>();
            if (glass == null || glass != CurrentGlass) return;

            var leaving = CurrentGlass;
            CurrentGlass = null;
            GlassRemoved?.Invoke(leaving);
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0f, 1f, 0.4f, 0.3f);
            var col = GetComponent<Collider>();
            if (col is BoxCollider b) Gizmos.DrawCube(transform.position + b.center, b.size);
        }
#endif
    }
}
