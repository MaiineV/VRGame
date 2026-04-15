using Data.Enums;
using Data.SO;
using Gameplay.Interactions;
using Gameplay.Liquid;
using HSM.Core.State;
using HSM.Events;
using Services;
using Services.Database;
using Services.Night;
using Services.Recipe;
using UnityEngine;

namespace Gameplay.Customer.States
{
    public sealed class WaitingState : BaseState
    {
        public override string StateId => CustomerStateIds.Waiting;

        private CustomerEntity _customer;
        private ServeSocket _socket;
        private RecipeMatch _pendingMatch;
        private LiquidMix _pendingMix;
        private bool _hasMatch;

        protected override void OnEnter(IStateContext context)
        {
            _customer = context.GetService<CustomerEntity>();
            if (_customer == null) return;

            _socket = _customer.Seat.ServeSocket;
            if (_socket != null) _socket.Served += HandleServed;
        }

        protected override void OnUpdate(IStateContext context)
        {
            if (_customer == null) return;

            if (_hasMatch)
            {
                _hasMatch = false;
                _customer.Drunkenness = ComputeDrunkenness(_pendingMix);
                _pendingMix = null;
                _customer.RaiseServed(_customer.TargetRecipe, _pendingMatch.Score, _pendingMatch.IsExact);
                PublishEvent(new CustomerServedEvent(
                    _customer.Seat.Index, _customer.TargetRecipe,
                    _pendingMatch.Score, _pendingMatch.IsExact));

                if (_pendingMatch.IsExact)
                    context.StateMachine.TransitionTo(CustomerStateIds.Drinking);
                else
                    context.StateMachine.TransitionTo(CustomerStateIds.Leaving);
                return;
            }

            _customer.WaitTimer -= Time.deltaTime;
            if (_customer.WaitTimer <= 0f)
                context.StateMachine.TransitionTo(CustomerStateIds.Leaving);
        }

        protected override void OnExit(IStateContext context)
        {
            if (_socket != null) _socket.Served -= HandleServed;
            _socket = null;
            _customer = null;
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
