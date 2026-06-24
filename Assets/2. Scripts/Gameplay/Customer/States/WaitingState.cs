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

        // Services resolved once on Enter (this state instance is reused across customers, so the
        // serve frame must not pay for repeated ServiceLocator.TryGet lookups). Any may be null.
        private IAudioService _audio;
        private IHapticService _haptics;
        private IVfxService _vfx;
        private IDatabaseService _db;
        private INightService _night;

        public void Enter(CustomerEntity c)
        {
            // This state instance is recycled across customers — clear any serve carried over from a
            // previous occupant before we re-subscribe, or a stale serve could bleed into this one.
            _hasMatch = false;
            _pendingGlass = null;
            _pendingMix = null;
            _pendingOk = false;

            ServiceLocator.TryGet(out _audio);
            ServiceLocator.TryGet(out _haptics);
            ServiceLocator.TryGet(out _vfx);
            ServiceLocator.TryGet(out _db);
            ServiceLocator.TryGet(out _night);

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

                if (_audio != null)
                {
                    var sfx = accepted ? SfxId.CustomerServed : SfxId.CustomerLeft;
                    _audio.PlayOneShot(sfx, c.transform.position);
                    // Accepted = the customer takes the drink: layer a natural sip on top of the chime.
                    if (accepted) _audio.PlayOneShot(SfxId.DrinkSip, c.transform.position);
                }

                // Serve confirmation: a satisfying double-tap feel on success, a softer nudge on a miss.
                _haptics?.PulseBoth(accepted ? 0.5f : 0.3f, accepted ? 0.12f : 0.06f);

                if (accepted)
                {
                    // Green sparkle above the satisfied customer.
                    _vfx?.PlayBurst(VfxId.ServeSuccess, c.transform.position + Vector3.up * 1.2f,
                                    new Color(0.3f, 1f, 0.4f, 1f));

                    // Pay scales with score (full when perfect, half when one axis is wrong).
                    c.RaiseServed(c.TargetRecipe, score, isExact);
                    // Take the glass and drink it right here at the bar seat, then leave through the
                    // door — no detour to a table.
                    c.Machine.TransitionTo(CustomerStateId.Drinking);
                }
                else
                {
                    // Red puff above the unhappy customer — a visual fail cue so the rejection reads
                    // without sound (accessibility: feedback must not be audio-only).
                    _vfx?.PlayBurst(VfxId.ServeFail, c.transform.position + Vector3.up * 1.2f,
                                    new Color(1f, 0.3f, 0.25f, 1f));

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
        private bool EvaluateDrink(CustomerEntity c, LiquidMix mix)
        {
            if (mix == null || mix.IsEmpty) return false;
            if (_db == null) return true;
            var recipe = _db.GetRecipe(c.TargetRecipe);
            if (recipe == null || recipe.Steps == null || recipe.Steps.Length == 0) return true;
            return mix.DominantId() == recipe.Steps[0].id;
        }

        private float ComputeDrunkenness(LiquidMix mix)
        {
            if (mix == null || mix.IsEmpty) return 0f;
            if (_night == null) return 0f;
            var cfg = (_night as NightService)?.DrunkennessConfig;
            if (cfg == null) return 0f;
            if (_db == null) return 0f;

            float alcoholMl = 0f;
            for (int i = 0; i < mix.Count; i++)
            {
                var ing = _db.GetIngredient(mix.IdAt(i));
                if (ing != null && ing.Type == IngredientType.Alcohol)
                    alcoholMl += mix.VolumeAt(i);
            }

            float denom = Mathf.Max(0.01f, cfg.AlcoholMlForMax);
            return Mathf.Clamp01(alcoholMl / denom);
        }
    }
}
