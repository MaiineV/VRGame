using Gameplay.Liquid;
using UnityEngine;

namespace Gameplay.Interactions
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class Glass : LiquidContainer
    {
        [Header("Identity")]
        [SerializeField] private int _id;

        public int Id => _id;
        public Rigidbody Body { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            Body = GetComponent<Rigidbody>();
            Body.interpolation = RigidbodyInterpolation.Interpolate;
        }
    }
}
