using UnityEngine;

namespace Services.UpdateService
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Services/UpdateService")]
    [DefaultExecutionOrder(-5000)]
    public class UpdateServiceObject : MonoBehaviour
    {
        private IUpdateService _service;

        private bool TryGetService()
        {
            if (_service != null) return true;
            return ServiceLocator.TryGet<IUpdateService>(out _service);
        }

        private void Update()
        {
            if (TryGetService()) _service.MyUpdate();
        }

        private void FixedUpdate()
        {
            if (TryGetService()) _service.MyFixedUpdate();
        }

        private void LateUpdate()
        {
            if (TryGetService()) _service.MyLateUpdate();
        }
    }
}