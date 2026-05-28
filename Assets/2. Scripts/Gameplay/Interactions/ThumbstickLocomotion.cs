using UnityEngine;
using Utilities;

namespace Gameplay.Interactions
{
    public sealed class ThumbstickLocomotion : MonoBehaviour
    {
        [SerializeField] private float _speed = 1.5f;
        [SerializeField] private OVRInput.Controller _moveController = OVRInput.Controller.LTouch;
        [SerializeField] private float _deadzone = 0.15f;

        private Transform _head;

        void Start()
        {
            var rig = FindAnyObjectByType<OVRCameraRig>();
            if (rig != null)
            {
                _head = rig.centerEyeAnchor;
            }
            else
            {
                MyLogger.LogWarning("[ThumbstickLocomotion] No OVRCameraRig found. Movement will use local forward.");
            }
        }

        void Update()
        {
            var input = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, _moveController);
            if (input.sqrMagnitude < _deadzone * _deadzone) return;

            Vector3 forward = _head != null ? _head.forward : transform.forward;
            Vector3 right = _head != null ? _head.right : transform.right;

            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            transform.position += (forward * input.y + right * input.x) * (_speed * Time.deltaTime);
        }
    }
}
