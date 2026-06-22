using Core.FSM;
using Data.Enums;
using Data.SO;
using Gameplay.Customer.States;
using Gameplay.Liquid;
using Gameplay.Systems;
using Services;
using Services.Audio;
using Services.Recipe;
using Services.UpdateService;
using UnityEngine;
using UnityEngine.AI;
using Utilities;

namespace Gameplay.Customer
{
    public sealed class CustomerEntity : MonoBehaviour, IUpdateListener
    {
        [Header("Seating")]
        [Tooltip("Extra yaw (degrees) applied when facing the bar, in case the model's forward axis is rotated. Leave 0 if the model faces +Z.")]
        [SerializeField] private float _faceYawOffsetDeg = 0f;

        [Header("Serving")]
        [Tooltip("Local offset (relative to the customer root) where the served glass is held while the " +
                 "customer wanders off and leaves. Roughly hand height, slightly forward-right.")]
        [SerializeField] private Vector3 _glassHoldOffset = new Vector3(0.16f, 1.05f, 0.18f);

        // Metres the customer ROOT rises when "seated". The bar has no stools now, so customers
        // stand at full height on the ground instead of being lifted — keep this at 0.
        // Hardcoded as a const (not a SerializeField) on purpose: the model is a Humanoid
        // FBX whose Animator overwrites any child transform, and a serialized field kept
        // a stale baked value that ignored code changes. The root is never animated.
        private const float SeatLiftY = 0f;

        public CustomerSO So { get; private set; }
        public CustomerSeatPoint Seat { get; private set; }
        public RecipeId TargetRecipe { get; private set; }
        // Requested fill level (index into FillLevels: 0=30% 1=50% 2=70% 3=100%). Set on spawn.
        public int TargetLevel { get; set; }
        // The glass the customer was served (correct or not), despawned when they leave.
        public Gameplay.Liquid.LiquidContainer ServedGlass { get; set; }
        public Transform ExitPoint { get; private set; }
        public StateMachine<CustomerStateId, CustomerEntity> Machine { get; private set; }

        public float WaitTimer;
        public float DrinkTimer;
        public float Drunkenness;

        // --- Locomotion animation ---
        // The customer ROOT is moved by code (MoveTowards); the Animator on the model child plays the
        // matching clip. We pick the clip from the root's real per-frame speed (so pauses, approach,
        // wander and leave are all covered without the states knowing about animation) plus drunkenness.
        [Tooltip("Drunkenness (0..1) at or above which the customer plays the drunk idle/walk clips.")]
        [SerializeField] private float _drunkAnimThreshold = 0.4f;
        [Tooltip("Root speed (m/s) above which the customer is considered walking rather than idle.")]
        [SerializeField] private float _walkSpeedThreshold = 0.05f;

        private Animator _animator;
        private NavMeshAgent _agent;   // optional; drives pathing when a runtime navmesh exists (else straight-line)
        private readonly int _hashIdle = Animator.StringToHash("Idle");
        private readonly int _hashWalking = Animator.StringToHash("Walking");
        private readonly int _hashDrunk = Animator.StringToHash("Drunk");
        private readonly int _hashDrunkWalk = Animator.StringToHash("Drunk Walk");
        private int _currentAnimHash;
        private Vector3 _lastAnimPos;
        private bool _hasAnimPos;

        // Footsteps: emit one each time the customer covers a stride's worth of ground (scales with
        // speed for free). Independent of the Animator, so it works even on un-rigged customers.
        [Tooltip("Metres walked between footstep sounds.")]
        [SerializeField] private float _strideLength = 0.55f;
        private float _stepDistance;
        private IAudioService _audio;

        public event System.Action<CustomerEntity, RecipeId, float, bool> Served;
        public event System.Action<CustomerEntity, bool> Left;

        public void RaiseServed(RecipeId recipe, float score, bool isExact) => Served?.Invoke(this, recipe, score, isExact);
        public void RaiseLeft(bool happy) => Left?.Invoke(this, happy);

        // Prefab reference used as the pool bucket key — set once on first Init, reused across recycles.
        private GameObject _prefabKey;

        private bool _registered;
        private bool _initialized;

        public void Init(CustomerSO so, CustomerSeatPoint seat, RecipeId recipe, Transform exitPoint)
        {
            // Track the prefab key so ReturnToPool can route back to the correct bucket.
            if (_prefabKey == null) _prefabKey = so.Prefab;

            So = so;
            Seat = seat;
            TargetRecipe = recipe;
            ExitPoint = exitPoint;

            // Resolve the model's Animator once (Human customer has one; kobolds don't — then this
            // stays null and all animation calls are harmless no-ops). Reset anim tracking for reuse.
            if (_animator == null) _animator = GetComponentInChildren<Animator>();
            _currentAnimHash = 0;
            _hasAnimPos = false;
            _stepDistance = 0f;

            // Reset runtime state for clean reuse.
            WaitTimer = so.PatienceSeconds;
            DrinkTimer = so.DrinkSeconds;
            Drunkenness = 0f;
            ServedGlass = null;
            Stand();

            // NavMesh pathing (optional): we keep moving the ROOT manually (so Sit/Stand/animation stay
            // as-is) but steer toward the agent's path corners so NPCs route AROUND the bar/furniture.
            // updatePosition/Rotation are off — the agent only computes the path; we drive the transform.
            if (_agent == null) _agent = GetComponent<NavMeshAgent>();
            if (_agent != null)
            {
                _agent.updatePosition = false;
                _agent.updateRotation = false;
                _agent.speed = so.WalkSpeed;
                _agent.enabled = true;
                // Seat the agent on the baked navmesh near the spawn so SetDestination works.
                if (NavMesh.SamplePosition(transform.position, out var navHit, 3f, NavMesh.AllAreas))
                    _agent.Warp(navHit.position);
            }

            // Build the machine once; on recycle, shut it down and restart to avoid
            // allocating new state objects on every spawn (allocation-conscious reuse).
            if (Machine == null)
            {
                Machine = new StateMachine<CustomerStateId, CustomerEntity>(this);
                Machine.AddState(CustomerStateId.Approaching, new ApproachingState());
                Machine.AddState(CustomerStateId.Waiting, new WaitingState());
                Machine.AddState(CustomerStateId.Wandering, new WanderingState());
                Machine.AddState(CustomerStateId.Leaving, new LeavingState());
            }
            else
            {
                Machine.Shutdown();
            }

            Machine.Start(CustomerStateId.Approaching);

            _initialized = true;
            RegisterTick();
        }

        void OnEnable()
        {
            if (_initialized) RegisterTick();
        }

        void OnDisable() => UnregisterTick();

        void OnDestroy()
        {
            // Safety fallback: fires only when the pooled GameObject is truly destroyed
            // (scene teardown or pool.ClearData). The normal despawn path is ReturnToPool.
            UnregisterTick();
            if (Seat != null && Seat.CurrentCustomer == this) Seat.Clear();
            Machine?.Shutdown();
        }

        public void MyUpdate()
        {
            Machine?.Update();
            UpdateAnimator();
        }

        /// <summary>
        /// Drives the model's locomotion animation from the root's actual movement this frame and the
        /// current drunkenness. Walking/Idle by speed; Drunk Walk/Drunk once drunk enough. No-op when
        /// the model has no Animator (e.g. kobold customers).
        /// </summary>
        private void UpdateAnimator()
        {
            Vector3 p = transform.position;
            float dt = Time.deltaTime;
            float dist = 0f;
            if (_hasAnimPos && dt > 0f)
            {
                Vector3 d = p - _lastAnimPos;
                d.y = 0f;
                dist = d.magnitude;
            }
            _lastAnimPos = p;
            _hasAnimPos = true;

            float speed = dt > 0f ? dist / dt : 0f;
            bool moving = speed > _walkSpeedThreshold;

            UpdateFootsteps(moving, dist);

            if (_animator == null) return;

            bool drunk = Drunkenness > _drunkAnimThreshold;
            int hash = moving ? (drunk ? _hashDrunkWalk : _hashWalking)
                              : (drunk ? _hashDrunk : _hashIdle);

            if (hash != _currentAnimHash)
            {
                _animator.CrossFade(hash, 0.15f, 0);
                _currentAnimHash = hash;
            }
        }

        /// <summary>Emits a positional footstep each time the customer covers a stride's worth of
        /// distance. Speed-correct for free (faster walk → more distance → more steps).</summary>
        private void UpdateFootsteps(bool moving, float distThisFrame)
        {
            if (!moving) return;
            _stepDistance += distThisFrame;
            if (_stepDistance < _strideLength) return;
            _stepDistance = 0f;

            if (_audio == null) ServiceLocator.TryGet<IAudioService>(out _audio);
            _audio?.PlayOneShot(SfxId.Footstep, transform.position);
        }

        public bool MoveTowards(Vector3 target, float arriveDistance = 0.05f)
        {
            var pos = transform.position;
            // Arrival is judged against the REAL target on the ground plane.
            Vector2 here2 = new Vector2(pos.x, pos.z);
            if (Vector2.Distance(here2, new Vector2(target.x, target.z)) <= arriveDistance) return true;

            var step = So.WalkSpeed * Time.deltaTime;

            // Where to head this frame. With a navmesh, follow the path's next corner so we route AROUND
            // obstacles; without one (no bake / off-mesh), fall back to heading straight at the target.
            Vector3 waypoint = target;
            if (_agent != null && _agent.isOnNavMesh)
            {
                _agent.speed = So.WalkSpeed;
                if (!_agent.pathPending && (_agent.destination - target).sqrMagnitude > 0.01f)
                    _agent.SetDestination(target);
                if (_agent.hasPath) waypoint = _agent.steeringTarget;
            }

            var dir = waypoint - pos;
            dir.y = 0f;
            float d = dir.magnitude;
            if (d > 0.0001f)
            {
                transform.position = pos + dir.normalized * Mathf.Min(step, d);
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation(dir.normalized, Vector3.up), 10f * Time.deltaTime);
            }

            // Keep the agent's internal position synced to the manually-moved root.
            if (_agent != null && _agent.isOnNavMesh) _agent.nextPosition = transform.position;
            return false;
        }

        /// <summary>
        /// Raises the customer and turns to face the bar/player. We lift the ROOT transform
        /// (not the "Visual" child) because the model is a Humanoid FBX: its Animator drives
        /// the child's local transform every LateUpdate, so any lift applied to the child is
        /// immediately overwritten. The root is never animated, so the lift sticks.
        /// </summary>
        public void Sit()
        {
            var p = transform.position;
            transform.position = new Vector3(p.x, SeatLiftY, p.z);
            FaceLookAt();
        }

        /// <summary>Drops the customer back to the floor so it can walk away upright.</summary>
        public void Stand()
        {
            var p = transform.position;
            transform.position = new Vector3(p.x, 0f, p.z);
        }

        /// <summary>
        /// Attaches the served glass to the customer so it travels with them as they wander off and
        /// leave, instead of staying on the bar. Parents it at a hold offset, freezes its physics, and
        /// disables its colliders so the carried glass can't shove the customer or be grabbed. Idempotent.
        /// </summary>
        public void CarryServedGlass()
        {
            if (ServedGlass == null) return;

            var t = ServedGlass.transform;
            t.SetParent(transform, worldPositionStays: false);
            t.localPosition = _glassHoldOffset;
            t.localRotation = Quaternion.identity;

            if (ServedGlass is Gameplay.Interactions.Glass glass && glass.Body != null)
            {
                glass.Body.isKinematic = true;
                glass.Body.useGravity = false;
            }

            var cols = ServedGlass.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < cols.Length; i++) cols[i].enabled = false;
        }

        /// <summary>
        /// Recycles the served glass (pool return, or Destroy fallback) and clears the reference.
        /// Called from <see cref="DespawnNow"/> so the carried glass leaves the world with the customer.
        /// </summary>
        public void RecycleServedGlass()
        {
            if (ServedGlass == null) return;

            if (ServedGlass is Gameplay.Interactions.Glass glass &&
                ServiceLocator.TryGet<IGlassPoolService>(out var pool))
            {
                pool.Return(glass);
            }
            else
            {
                Destroy(ServedGlass.gameObject);
            }
            ServedGlass = null;
        }

        /// <summary>
        /// Yaws the whole customer to face the bar. Targets the ServePoint first: it sits
        /// on the bar in front of the customer, so its horizontal direction reliably points
        /// at the bar/player. The LookAtPoint can sit almost directly overhead (tiny, off-axis
        /// horizontal component) which made customers face sideways — so it is only a fallback.
        /// </summary>
        private void FaceLookAt()
        {
            if (Seat == null) return;

            Vector3 from = transform.position;
            Vector3 dir = Vector3.zero;

            // Primary: face the serve point (on the bar, in front of the seat).
            if (Seat.ServePoint != null)
            {
                dir = Seat.ServePoint.position - from;
                dir.y = 0f;
            }
            // Fallback: only if the serve point is essentially on top of us (no clear
            // horizontal direction), try the look point.
            if (dir.sqrMagnitude < 0.01f && Seat.LookAtPoint != null)
            {
                dir = Seat.LookAtPoint.position - from;
                dir.y = 0f;
            }

            if (dir.sqrMagnitude > 0.0001f)
            {
                var look = Quaternion.LookRotation(dir.normalized, Vector3.up);
                if (Mathf.Abs(_faceYawOffsetDeg) > 0.01f)
                    look *= Quaternion.Euler(0f, _faceYawOffsetDeg, 0f);
                transform.rotation = look;
            }
        }

        /// <summary>
        /// Returns this customer to the object pool (preferred path).
        /// Unregisters the tick, resets seat, shuts the FSM, and deactivates the GameObject.
        /// NightService must unsubscribe Served/Left BEFORE calling this.
        /// Falls back to Destroy if the pool service is unavailable.
        /// </summary>
        public void DespawnNow()
        {
            UnregisterTick();
            _initialized = false;

            // The customer takes their glass with them — recycle it as they leave the world.
            RecycleServedGlass();

            if (Seat != null && Seat.CurrentCustomer == this) Seat.Clear();
            Seat = null;

            Machine?.Shutdown();

            if (_prefabKey != null && ServiceLocator.TryGet<ICustomerPoolService>(out var pool))
            {
                pool.Return(_prefabKey, this);
            }
            else
            {
                MyLogger.LogWarning("[CustomerEntity] CustomerPoolService unavailable — falling back to Destroy.");
                Destroy(gameObject);
            }
        }

        private void RegisterTick()
        {
            if (_registered) return;
            if (!ServiceLocator.TryGet<IUpdateService>(out var svc)) return;
            svc.AddUpdateListener(this);
            _registered = true;
        }

        private void UnregisterTick()
        {
            if (!_registered) return;
            if (ServiceLocator.TryGet<IUpdateService>(out var svc))
                svc.RemoveUpdateListener(this);
            _registered = false;
        }
    }
}
