using Core.FSM;
using Gameplay;
using UnityEngine;

namespace Gameplay.Customer.States
{
    /// <summary>
    /// Post-serve state: the customer stands up and strolls around the bar for
    /// <see cref="CustomerEntity.DrinkTimer"/> seconds (= <c>CustomerSO.DrinkSeconds</c>) instead of
    /// leaving immediately. It walks between the open-floor waypoints on <see cref="BarSceneRoot"/>
    /// (<see cref="BarSceneRoot.WanderPoints"/>), pausing briefly at each, then transitions to
    /// <see cref="CustomerStateId.Leaving"/> when the timer runs out.
    ///
    /// Movement reuses <see cref="CustomerEntity.MoveTowards"/> (straight-line, no NavMesh) — the same
    /// mover as Approaching/Leaving — so waypoints must sit in walkable, obstacle-free floor.
    /// </summary>
    public sealed class WanderingState : IState<CustomerEntity>
    {
        // Seconds to dwell at a waypoint before picking the next one.
        private const float PauseSeconds = 1.25f;
        // Fallback wander radius when no waypoints are configured on BarSceneRoot.
        private const float FallbackRadius = 1.5f;

        private Vector3 _target;
        private float _pauseTimer;

        public void Enter(CustomerEntity c)
        {
            c.Stand();
            _pauseTimer = 0f;
            _target = PickTarget(c);
        }

        public void Update(CustomerEntity c)
        {
            // DrinkTimer doubles as the "time spent in the bar after being served" budget. Counting it
            // down here keeps LeavingState's happy check (DrinkTimer < DrinkSeconds) correct.
            c.DrinkTimer -= Time.deltaTime;
            if (c.DrinkTimer <= 0f)
            {
                c.Machine.TransitionTo(CustomerStateId.Leaving);
                return;
            }

            // Dwell at the current spot for a beat, then walk to the next one.
            if (_pauseTimer > 0f)
            {
                _pauseTimer -= Time.deltaTime;
                return;
            }

            if (c.MoveTowards(_target, 0.2f))
            {
                _pauseTimer = PauseSeconds;
                _target = PickTarget(c);
            }
        }

        public void Exit(CustomerEntity c) { }

        private Vector3 PickTarget(CustomerEntity c)
        {
            var root = BarSceneRoot.Instance;
            var points = root != null ? root.WanderPoints : null;
            if (points != null && points.Length > 0)
            {
                // Pick a random configured waypoint; retry once to avoid re-picking the closest one.
                var p = points[Random.Range(0, points.Length)];
                if (p != null) return p.position;
            }

            // No waypoints set: wander to a random offset around the current position.
            var offset = new Vector3(Random.Range(-FallbackRadius, FallbackRadius), 0f,
                                     Random.Range(-FallbackRadius, FallbackRadius));
            return c.transform.position + offset;
        }
    }
}
