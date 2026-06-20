using Gameplay.Liquid;
using UnityEngine;

namespace Gameplay.Interactions
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class Glass : LiquidContainer
    {
        [Header("Identity")]
        [SerializeField] private int _id;

        [Header("Physics (applied on Awake)")]
        [Tooltip("Mass in kg. A drinking glass is light (~0.3 kg).")]
        [SerializeField] private float _mass = 0.35f;
        [Tooltip("Linear damping (air resistance). Low so a thrown glass flies instead of floating.")]
        [SerializeField] private float _linearDamping = 0.05f;
        [Tooltip("Angular damping. Kept high (with the low centre of mass) so the glass self-rights when set down.")]
        [SerializeField] private float _angularDamping = 0.6f;

        public int Id => _id;
        public Rigidbody Body { get; private set; }

        /// <summary>
        /// Fill level the nearby customer requested (index into FillLevels), or null when the glass
        /// isn't sitting in a serve socket. Set by <see cref="ServeSocket"/> on enter/exit and read by
        /// the glass fill gauge to draw the target marker.
        /// </summary>
        public int? RequestedLevel { get; set; }

        /// <summary>Pool bucket this instance was spawned from; set by IGlassPoolService.</summary>
        public GameObject SourcePrefab { get; set; }

        protected override void Awake()
        {
            base.Awake();
            Body = GetComponent<Rigidbody>();
            Body.interpolation = RigidbodyInterpolation.Interpolate;
            Body.mass = _mass;
            Body.linearDamping = _linearDamping;
            // Keep the glass upright when set down: a low, explicit centre of mass plus
            // extra angular damping makes it self-right instead of tipping over. Paired
            // with the flat-bottomed BoxCollider on the prefab (a CapsuleCollider's rounded
            // base balances on a point and tips).
            Body.centerOfMass = new Vector3(0f, 0.02f, 0f);
            Body.angularDamping = _angularDamping;
        }

        /// <summary>
        /// Restore a recycled glass to a clean state before re-use: empty the liquid, drop any
        /// held flag, and zero physics so it doesn't inherit the velocity it had when pooled.
        /// </summary>
        public void ResetForPool()
        {
            Empty();

            if (Body != null)
            {
                Body.isKinematic = false;
                Body.useGravity = true;
                Body.linearVelocity = Vector3.zero;
                Body.angularVelocity = Vector3.zero;
            }

            var grab = GetComponent<GrabBridge>();
            if (grab != null) grab.SetHeld(false);
        }
    }
}
