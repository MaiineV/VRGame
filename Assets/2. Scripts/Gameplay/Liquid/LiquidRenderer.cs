using Data.Enums;
using Data.SO;
using UnityEngine;

namespace Gameplay.Liquid
{
    /// <summary>
    /// Drives the liquid shader on a container via MaterialPropertyBlock.
    /// Shader must expose `_FillAmount` (float 0..1) and `_LiquidColor` (color).
    /// Optional: Sway is done in the shader reading `_WobbleVelocity`.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public sealed class LiquidRenderer : MonoBehaviour
    {
        private static readonly int FillAmountId = Shader.PropertyToID("_FillAmount");
        private static readonly int LiquidColorId = Shader.PropertyToID("_LiquidColor");
        private static readonly int WobbleVelocityId = Shader.PropertyToID("_WobbleVelocity");

        [SerializeField] private IngredientPalette _palette;
        [Tooltip("Tint the liquid by its discrete fill level (green/yellow/orange/red) so the glass colour matches the customer's order. Off = blend by ingredient.")]
        [SerializeField] private bool _colorByFillLevel = true;

        private Renderer _renderer;
        private MaterialPropertyBlock _block;
        private System.Func<IngredientId, Color> _resolve; // cached to avoid per-call delegate alloc
        private Vector2 _lastWobble;   // last written wobble velocity — skip redundant PropertyBlock writes
        private bool _wobbleWritten;

        void Awake()
        {
            _renderer = GetComponent<Renderer>();
            _block = new MaterialPropertyBlock();
            _resolve = ResolveColor;
        }

        public void Refresh(LiquidContainer container)
        {
            if (_renderer == null || _block == null) return;

            var color = _colorByFillLevel
                ? FillLevels.ColorForRatio(container.FillRatio)
                : container.Mix.BlendColor(_resolve);
            _renderer.GetPropertyBlock(_block);
            _block.SetFloat(FillAmountId, container.FillRatio);
            _block.SetColor(LiquidColorId, color);
            _renderer.SetPropertyBlock(_block);
        }

        /// <summary>
        /// Per-frame wobble write. Kept separate from Refresh() so the mix/fill path
        /// stays event-driven while movement can tick on-demand from a held container.
        /// </summary>
        public void SetWobbleVelocity(Vector2 horizontalVelocity)
        {
            if (_renderer == null || _block == null) return;
            // Skip the Get/SetPropertyBlock round-trip when the velocity hasn't meaningfully changed —
            // it ran every LateUpdate while a glass was held, even once the liquid had settled.
            if (_wobbleWritten && (horizontalVelocity - _lastWobble).sqrMagnitude < 1e-6f) return;
            _lastWobble = horizontalVelocity;
            _wobbleWritten = true;
            _renderer.GetPropertyBlock(_block);
            _block.SetVector(WobbleVelocityId, new Vector4(horizontalVelocity.x, 0f, horizontalVelocity.y, 0f));
            _renderer.SetPropertyBlock(_block);
        }

        private Color ResolveColor(IngredientId id)
        {
            if (_palette == null) return Color.white;
            return _palette.GetColor(id);
        }
    }
}
