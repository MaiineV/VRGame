using System.Collections.Generic;
using UnityEngine;

namespace Utilities.Pool
{
    public class PoolGeneric<T> where T : Object
    {
        private readonly T _mPrefab;
        private readonly Transform _mParent;
        private readonly Queue<T> _mAvailables = new();

        public PoolGeneric(T pPrefab, Transform pTransformParent = null)
        {
            _mPrefab = pPrefab;
            _mParent = pTransformParent;
        }

        public T GetOrCreate()
        {
            if (_mAvailables.Count > 0)
            {
                var lObj = _mAvailables.Dequeue();
                while (lObj == null && _mAvailables.Count > 0)
                {
                    lObj = _mAvailables.Dequeue();
                }

                if (lObj == null)
                {
                    lObj = InstantiateInactive();
                }

                return lObj;
            }

            return InstantiateInactive();
        }

        public void ReturnToPool(T pPoolEntry)
        {
            if (pPoolEntry == null)
                return;

            // Re-parent back to the pool root before deactivating. A pooled entry can be parented
            // elsewhere while live (e.g. a glass parented to a customer as it's carried off); without
            // this it returns still under that transform and comes back stale/invisible.
            if (pPoolEntry is Component lComponent && _mParent != null)
                lComponent.transform.SetParent(_mParent, false);

            SetActiveState(pPoolEntry, false);
            _mAvailables.Enqueue(pPoolEntry);
        }

        public void ClearData()
        {
            _mAvailables.Clear();
        }

        private T InstantiateInactive()
        {
            var lInstance = Object.Instantiate(_mPrefab, _mParent);
            SetActiveState(lInstance, false);
            return lInstance;
        }

        private static void SetActiveState(T pObject, bool pActive)
        {
            if (pObject is Component lComponent)
            {
                lComponent.gameObject.SetActive(pActive);
            }
            else if (pObject is GameObject lGameObject)
            {
                lGameObject.SetActive(pActive);
            }
        }
    }
}