using Core.FSM;
using Data.Enums;
using Data.SO;
using Gameplay.Interactions;
using Gameplay.Liquid;
using Services;
using Services.Database;
using Services.Night;
using Services.Recipe;
using UnityEngine;

namespace Gameplay.Customer.States
{
    public sealed class WaitingState : IState<CustomerEntity>
    {
        private ServeSocket _socket;
        private RecipeMatch _pendingMatch;
        private LiquidMix _pendingMix;
        private bool _hasMatch;

        public void Enter(CustomerEntity c)
        {
            _socket = c.Seat.ServeSocket;
            if (_socket != null) _socket.Served += HandleServed;
        }

        public void Update(CustomerEntity c)
        {
            if (_hasMatch)
            {
                _hasMatch = false;
                c.Drunkenness = ComputeDrunkenness(_pendingMix);
                _pendingMix = null;
                c.RaiseServed(c.TargetRecipe, _pendingMatch.Score, _pendingMatch.IsExact);

                if (_pendingMatch.IsExact)
                    c.Machine.TransitionTo(CustomerStateId.Drinking);
                else
                    c.Machine.TransitionTo(CustomerStateId.Leaving);
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

        private void HandleServed(Glass glass, RecipeMatch match)
        {
            _pendingMatch = match;
            _pendingMix = glass != null ? glass.Mix : null;
            _hasMatch = true;
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
