using HSM.Core.State;
using HSM.Events;
using Services;
using Services.Night;
using UnityEngine;

namespace Gameplay.Customer.States
{
    public sealed class LeavingState : BaseState
    {
        public override string StateId => CustomerStateIds.Leaving;

        private bool _published;
        private float _wobbleTime;

        protected override void OnEnter(IStateContext context)
        {
            _published = false;
            _wobbleTime = 0f;
        }

        protected override void OnUpdate(IStateContext context)
        {
            var c = context.GetService<CustomerEntity>();
            if (c == null || c.ExitPoint == null) { c?.DespawnNow(); return; }

            if (!_published)
            {
                bool happy = c.DrinkTimer < c.So.DrinkSeconds; // entered Drinking → consumed timer
                c.RaiseLeft(happy);
                PublishEvent(new CustomerLeftEvent(c.Seat.Index, happy));
                _published = true;
            }

            var target = c.ExitPoint.position;
            target += ComputeWobble(c);

            if (c.MoveTowards(target, 0.15f))
                c.DespawnNow();
        }

        private Vector3 ComputeWobble(CustomerEntity c)
        {
            if (c.Drunkenness <= 0.01f) return Vector3.zero;
            if (!ServiceLocator.TryGet<INightService>(out var n)) return Vector3.zero;
            var cfg = (n as NightService)?.DrunkennessConfig;
            if (cfg == null) return Vector3.zero;

            _wobbleTime += Time.deltaTime;
            var dir = (c.ExitPoint.position - c.transform.position); dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return Vector3.zero;
            var right = Vector3.Cross(Vector3.up, dir.normalized);

            float amp = cfg.WobbleAmplitude * c.Drunkenness;
            float phase = _wobbleTime * cfg.WobbleFrequency * Mathf.PI * 2f;
            return right * (Mathf.Sin(phase) * amp);
        }
    }
}
