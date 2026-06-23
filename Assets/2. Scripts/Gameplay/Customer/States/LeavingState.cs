using Core.FSM;
using Gameplay.Interactions;
using Gameplay.Systems;
using Services;
using Services.Night;
using UnityEngine;

namespace Gameplay.Customer.States
{
    public sealed class LeavingState : IState<CustomerEntity>
    {
        private bool _published;
        private float _wobbleTime;
        // Index of the next exit-route waypoint to walk to (before heading to the exit point).
        private int _wp;

        // Resolved once on Enter so ComputeWobble doesn't TryGet every tick. May be null.
        private INightService _night;

        public void Enter(CustomerEntity c)
        {
            c.Stand();
            _published = false;
            _wobbleTime = 0f;
            _wp = 0;
            ServiceLocator.TryGet(out _night);

            // Keep the glass visible if they actually drank at a table; otherwise (rejected serve or
            // patience timeout) carry it hidden so there's no glass floating beside the NPC. Either way it
            // rides with the customer and is recycled in CustomerEntity.DespawnNow.
            if (c.DrankAtTable) c.CarryServedGlassVisible();
            else c.CarryServedGlassHidden();
        }

        public void Update(CustomerEntity c)
        {
            if (c.ExitPoint == null) { c.DespawnNow(); return; }

            if (!_published)
            {
                bool happy = c.DrinkTimer < c.So.DrinkSeconds;
                c.RaiseLeft(happy);
                _published = true;
            }

            // Walk the editable exit route first (seat → ... → near the door), then to the exit point.
            var route = BarSceneRoot.Instance != null ? BarSceneRoot.Instance.ExitRoute : null;
            if (route != null && _wp < route.Count)
            {
                if (c.MoveTowards(route.GetPoint(_wp), 0.25f)) _wp++;
                return;
            }

            var target = c.ExitPoint.position;
            target += ComputeWobble(c);

            if (c.MoveTowards(target, 0.15f))
                c.DespawnNow();
        }

        public void Exit(CustomerEntity c) { }

        private Vector3 ComputeWobble(CustomerEntity c)
        {
            if (c.Drunkenness <= 0.01f) return Vector3.zero;
            if (_night == null) return Vector3.zero;
            var cfg = (_night as NightService)?.DrunkennessConfig;
            if (cfg == null) return Vector3.zero;

            _wobbleTime += Time.deltaTime;
            var dir = c.ExitPoint.position - c.transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return Vector3.zero;
            var right = Vector3.Cross(Vector3.up, dir.normalized);

            float amp = cfg.WobbleAmplitude * c.Drunkenness;
            float phase = _wobbleTime * cfg.WobbleFrequency * Mathf.PI * 2f;
            return right * (Mathf.Sin(phase) * amp);
        }
    }
}
