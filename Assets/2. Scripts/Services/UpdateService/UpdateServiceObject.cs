using UnityEngine;

namespace Services.UpdateService
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Services/UpdateService")]
    [DefaultExecutionOrder(-5000)]
    public class UpdateServiceObject : MonoBehaviour
    {
        private static IUpdateService UpdateService => ServiceLocator.Get<IUpdateService>();
        
        private void Update()
        {
            UpdateService.MyUpdate();
        }

        private void FixedUpdate()
        {
            UpdateService.MyFixedUpdate();
        }

        private void LateUpdate()
        {
            UpdateService.MyLateUpdate();
        }
    }
}