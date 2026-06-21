using Core.FSM;
using Data.Enums;
using Data.SO;
using Gameplay.Interactions;
using Gameplay.Liquid;
using Services;
using Services.Audio;
using Services.Database;
using Services.Haptics;
using Services.Night;
using Services.Vfx;
using UnityEngine;

namespace Gameplay.Customer.States
{
    public sealed class WaitingState : IState<CustomerEntity>
    {
        // Drink matters but with partial credit: score = 0.5*levelOk + 0.5*drinkOk.
        // The customer accepts (drinks + pays, scaled by score) at or above this; below, they leave.
        private const float AcceptThreshold = 0.5f;

        private ServeSocket _socket;
        private bool _pendingOk;
        private LiquidMix _pendingMix;
        private Glass _pendingGlass;
        private bool _hasMatch;

        public void Enter(CustomerEntity c)
        {
            c.Sit();
            _socket = c.Seat.ServeSocket;
            if (_socket != null) _socket.Served += HandleServed;
        }

        public void Update(CustomerEntity c)
        {
            if (_hasMatch)
            {
                _hasMatch = false;

                // Two axes: the fill level (from the socket) and the drink itself (dominant
                // ingredient vs the recipe's main ingredient). Each is worth half — so the
                // wrong drink at the right level (or vice versa) is a partial, not a hard fail.
                bool levelOk = _pendingOk;
                bool drinkOk = EvaluateDrink(c, _pendingMix);
                float score = (levelOk ? 0.5f : 0f) + (drinkOk ? 0.5f : 0f);
                bool isExact = levelOk && drinkOk;
                bool accepted = score >= AcceptThreshold;

                c.Drunkenness = ComputeDrunkenness(_pendingMix);
                _pendingMix = null;
                c.ServedGlass = _pendingGlass;   // despawned when the customer leaves
                _pendingGlass = null;

                if (ServiceLocator.TryGet<IAudioService>(out var audio))
                {
                    var sfx = accepted ? SfxId.CustomerServed : SfxId.CustomerLeft;
                    audio.PlayOneShot(sfx, c.transform.position);
                }

                // Serve confirmation: a satisfying double-tap feel on success, a softer nudge on a miss.
                if (ServiceLocator.TryGet<IHapticService>(out var hap))
                    hap.PulseBoth(accepted ? 0.5f : 0.3f, accepted ? 0.12f : 0.06f);

                if (accepted)
                {
                    // Green sparkle above the satisfied customer.
                    if (ServiceLocator.TryGet<IVfxService>(out var vfx))
                        vfx.PlayBurst(VfxId.ServeSuccess, c.transform.position + Vector3.up * 1.2f,
                                      new Color(0.3f, 1f, 0.4f, 1f));

                    // Pay scales with score (full when perfect, half when one axis is wrong).
                    c.RaiseServed(c.TargetRecipe, score, isExact);
                    c.Machine.TransitionTo(CustomerStateId.Wandering);
                }
                else
                {
                    // Both wrong: no sale; the customer leaves unhappy (counts as a failure).
                    c.Machine.TransitionTo(CustomerStateId.Leaving);
                }
                return;
            }

            c.WaitTimer -= Time.deltaTime;
            if (c.WaitTimer <= 0f)
                c.Machine.TransitionTo(CustomerStateId.Leaving);
        }

        public void Exit(CustomerEntity c)
        {
            if (_socket != null) _socket.Served -= HandleServed;
            _socket = null;
        }

        private void HandleServed(Glass glass, bool ok)
        {
            _pendingOk = ok;
            _pendingMix = glass != null ? glass.Mix : null;
            _pendingGlass = glass;
            _hasMatch = true;
        }

        /// <summary>
        /// True if the glass's dominant ingredient matches the recipe's main ingredient.
        /// Defensive defaults to true when we can't resolve the recipe (don't punish on missing data).
        /// </summary>
        private static bool EvaluateDrink(CustomerEntity c, LiquidMix mix)
        {
            if (mix == null || mix.IsEmpty) return false;
            if (!ServiceLocator.TryGet<IDatabaseService>(out var db)) return true;
            var recipe = db.GetRecipe(c.TargetRecipe);
            if (recipe == null || recipe.Steps == null || recipe.Steps.Length == 0) return true;
            return mix.DominantId() == recipe.Steps[0].id;
        }

        private static float ComputeDrunkenness(LiquidMix mix)
        {
            if (mix == null || mix.IsEmpty) return 0f;
            if (!ServiceLocator.TryGet<INightService>(out var night)) return 0f;
            var cfg = (night as NightService)?.DrunkennessConfig;
            if (cfg == null) return 0f;
            if (!ServiceLocator.TryGet<IDatabaseService>(out var db)) return 0f;

            float alcoholMl = 0f;
            for (int i = 0; i < mix.Count; i++)
            {
                var ing = db.GetIngredient(mix.IdAt(i));
                if (ing != null && ing.Type == IngredientType.Alcohol)
                    alcoholMl += mix.VolumeAt(i);
            }

            float denom = Mathf.Max(0.01f, cfg.AlcoholMlForMax);
            return Mathf.Clamp01(alcoholMl / denom);
        }
    }
}
