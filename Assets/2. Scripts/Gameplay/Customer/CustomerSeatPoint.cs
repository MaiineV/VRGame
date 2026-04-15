using Gameplay.Interactions;
using UnityEngine;

namespace Gameplay.Customer
{
    public sealed class CustomerSeatPoint : MonoBehaviour
    {
        [SerializeField] private int _index;
        [SerializeField] private Transform _servePoint;
        [SerializeField] private Transform _lookAtPoint;
        [SerializeField] private ServeSocket _serveSocket;

        public int Index => _index;
        public Transform ServePoint => _servePoint != null ? _servePoint : transform;
        public Transform LookAtPoint => _lookAtPoint != null ? _lookAtPoint : transform;
        public ServeSocket ServeSocket => _serveSocket;

        public bool IsOccupied { get; private set; }
        public CustomerEntity CurrentCustomer { get; private set; }

        public event System.Action<CustomerEntity> CustomerBound;
        public event System.Action CustomerCleared;

        public void SetOccupied(bool occupied) => IsOccupied = occupied;

        public void Bind(CustomerEntity customer)
        {
            CurrentCustomer = customer;
            IsOccupied = true;
            CustomerBound?.Invoke(customer);
        }

        public void Clear()
        {
            CurrentCustomer = null;
            IsOccupied = false;
            CustomerCleared?.Invoke();
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            Gizmos.color = IsOccupied ? Color.red : Color.green;
            Gizmos.DrawWireSphere(transform.position, 0.25f);
            if (_servePoint)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(_servePoint.position, Vector3.one * 0.1f);
            }
        }
#endif
    }
}
