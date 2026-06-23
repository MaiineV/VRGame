using UnityEngine;

namespace Gameplay.Customer
{
    /// <summary>
    /// A spot a served customer walks to in order to "drink" before leaving. Mirrors
    /// <see cref="CustomerSeatPoint"/>'s occupancy contract (one customer at a time) but has no
    /// ServeSocket — tables are not served at, only occupied while drinking. Place these on the
    /// NavMesh near tables; the customer paths here, faces <see cref="LookAtPoint"/>, plays the drink
    /// animation, then releases the point when it leaves.
    /// </summary>
    public sealed class CustomerTablePoint : MonoBehaviour
    {
        [Tooltip("Optional point the customer turns to face while drinking (e.g. the table centre). " +
                 "Falls back to this transform's own forward if unset.")]
        [SerializeField] private Transform _lookAtPoint;

        public Transform LookAtPoint => _lookAtPoint != null ? _lookAtPoint : transform;

        public bool IsOccupied { get; private set; }
        public CustomerEntity CurrentCustomer { get; private set; }

        public void Bind(CustomerEntity customer)
        {
            CurrentCustomer = customer;
            IsOccupied = true;
        }

        public void Clear()
        {
            CurrentCustomer = null;
            IsOccupied = false;
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            Gizmos.color = IsOccupied ? Color.red : new Color(0.4f, 0.7f, 1f);
            Gizmos.DrawWireSphere(transform.position, 0.25f);
            if (_lookAtPoint != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position, _lookAtPoint.position);
            }
        }
#endif
    }
}
