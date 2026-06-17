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

        public void Enter(CustomerEntity c)
        {
            c.Stand();
            _published = false;
            _wobbleTime = 0f;

            // The customer takes their glass with them: recycle it as they leave.
            // Return to the pool when possible; only Destroy as a fallback.
            if (c.ServedGlass != null)
            {
                if (c.ServedGlass is Glass glass &&
                    ServiceLocator.TryGet<IGlassPoolService>(out var pool))
                {
                    pool.Return(glass);
                }
                else
                {
                    Object.Destroy(c.ServedGlass.gameObject);
                }
                c.ServedGlass = null;
            }
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

            var target = c.ExitPoint.position;
            target += ComputeWobble(c);

            if (c.MoveTowards(target, 0.15f))
                c.DespawnNow();
        }

        public void Exit(CustomerEntity c) { }

        private Vector3 ComputeWobble(CustomerEntity c)
        {
            if (c.Drunkenness <= 0.01f) return Vector3.zero;
            if (!ServiceLocator.TryGet<INightService>(out var n)) return Vector3.zero;
            var cfg = (n as NightService)?.DrunkennessConfig;
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
