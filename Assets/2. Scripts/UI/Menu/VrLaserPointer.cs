using UnityEngine;

namespace UI.Menu
{
    /// <summary>
    /// Minimal controller laser-pointer for the main-menu scene.
    /// Casts a ray from the controller anchor (or this transform as fallback) and
    /// highlights any <see cref="MenuButton"/> found along the ray. Trigger or face-button A
    /// fires the currently targeted button.
    ///
    /// Deliberately avoids Unity's EventSystem / GraphicRaycaster path, which requires a
    /// Canvas + PhysicsRaycaster setup that conflicts with the project's custom OVR interaction
    /// conventions and has historically been fragile in VR world-space UIs.
    /// </summary>
    public sealed class VrLaserPointer : MonoBehaviour
    {
        [Tooltip("Which OVR controller to read input from.")]
        [SerializeField] private OVRInput.Controller _controller = OVRInput.Controller.RTouch;

        [Tooltip("The transform used as ray origin (e.g. the right controller anchor). " +
                 "Falls back to this.transform if left null.")]
        [SerializeField] private Transform _rayOrigin;

        [Tooltip("Maximum raycast distance in metres.")]
        [SerializeField] private float _maxDistance = 8f;

        [Tooltip("Layer mask for UI colliders. Default ~0 hits everything; restrict to a " +
                 "dedicated UI layer in the scene for cleaner raycast performance.")]
        [SerializeField] private LayerMask _uiMask = ~0;

        [Tooltip("Optional LineRenderer for the laser-beam visual. If null no beam is drawn.")]
        [SerializeField] private LineRenderer _line;

        // ── state ────────────────────────────────────────────────────────────────
        private MenuButton _currentButton;

        // ── Unity ────────────────────────────────────────────────────────────────

        void Update()
        {
            Transform origin = _rayOrigin != null ? _rayOrigin : transform;
            Ray ray = new Ray(origin.position, origin.forward);

            MenuButton hit = null;
            Vector3 endpoint = origin.position + origin.forward * _maxDistance;

            if (Physics.Raycast(ray, out RaycastHit info, _maxDistance, _uiMask))
            {
                endpoint = info.point;
                // Walk up the hierarchy: the collider may be a child of the MenuButton root.
                hit = info.collider.GetComponentInParent<MenuButton>();
            }

            // Update visual beam (origin → endpoint).
            if (_line != null)
            {
                _line.SetPosition(0, origin.position);
                _line.SetPosition(1, endpoint);
            }

            // Highlight state transitions.
            if (hit != _currentButton)
            {
                if (_currentButton != null) _currentButton.SetHighlighted(false);
                _currentButton = hit;
                if (_currentButton != null) _currentButton.SetHighlighted(true);
            }

            // Fire on primary index trigger OR button A (One) as a fallback.
            bool fired = OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, _controller)
                      || OVRInput.GetDown(OVRInput.Button.One, _controller);

            if (fired && _currentButton != null)
                _currentButton.Click();
        }

        void OnDisable()
        {
            // Clear highlight so the button doesn't stay highlighted if the pointer is disabled.
            if (_currentButton != null)
            {
                _currentButton.SetHighlighted(false);
                _currentButton = null;
            }

            if (_line != null)
            {
                _line.SetPosition(0, Vector3.zero);
                _line.SetPosition(1, Vector3.zero);
            }
        }
    }
}
