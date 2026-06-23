using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.Customer
{
    /// <summary>
    /// An editable walking route for customers. Each DIRECT CHILD GameObject is a waypoint, used in
    /// sibling (hierarchy) order. To edit the path in the Scene view:
    ///   - Move a point: drag its child GameObject.
    ///   - Add a point: create an empty child (it appends to the end of the route).
    ///   - Remove a point: delete the child.
    ///   - Reorder: drag children up/down in the Hierarchy.
    /// The route is drawn with gizmos (numbered spheres + direction arrows) so it's visible while editing.
    ///
    /// Customers follow the points in order (NavMesh handles avoiding obstacles BETWEEN points); when the
    /// route is empty they just go straight to their final target. Position only — Y is taken from each
    /// waypoint, so place them at floor height.
    /// </summary>
    public sealed class CustomerRoute : MonoBehaviour
    {
        [Tooltip("Gizmo colour for this route in the Scene view.")]
        [SerializeField] private Color _color = new Color(0.2f, 0.9f, 1f);
        [Tooltip("Gizmo sphere radius drawn at each waypoint.")]
        [SerializeField] private float _pointRadius = 0.12f;

        /// <summary>Number of waypoints (direct children).</summary>
        public int Count => transform.childCount;

        /// <summary>World position of waypoint <paramref name="i"/> (in hierarchy order).</summary>
        public Vector3 GetPoint(int i) => transform.GetChild(i).position;

        /// <summary>Fills <paramref name="buffer"/> with every waypoint's world position, in order.</summary>
        public void GetPoints(List<Vector3> buffer)
        {
            if (buffer == null) return;
            buffer.Clear();
            for (int i = 0; i < transform.childCount; i++)
                buffer.Add(transform.GetChild(i).position);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            int n = transform.childCount;
            if (n == 0) return;

            Gizmos.color = _color;
            Vector3 prev = Vector3.zero;
            bool hasPrev = false;

            for (int i = 0; i < n; i++)
            {
                Vector3 p = transform.GetChild(i).position;
                Gizmos.DrawSphere(p, _pointRadius);
                UnityEditor.Handles.color = _color;
                UnityEditor.Handles.Label(p + Vector3.up * (_pointRadius + 0.12f), $"{name} · {i}");

                if (hasPrev)
                {
                    Gizmos.DrawLine(prev, p);
                    DrawArrowHead(prev, p);
                }
                prev = p;
                hasPrev = true;
            }
        }

        // Small arrowhead at the midpoint of each segment to show travel direction.
        private void DrawArrowHead(Vector3 from, Vector3 to)
        {
            Vector3 dir = to - from;
            float len = dir.magnitude;
            if (len < 0.001f) return;
            dir /= len;
            Vector3 mid = (from + to) * 0.5f;
            Vector3 side = Vector3.Cross(dir, Vector3.up);
            if (side.sqrMagnitude < 0.0001f) side = Vector3.Cross(dir, Vector3.forward);
            side.Normalize();
            float s = Mathf.Min(0.18f, len * 0.3f);
            Gizmos.DrawLine(mid, mid - dir * s + side * s * 0.6f);
            Gizmos.DrawLine(mid, mid - dir * s - side * s * 0.6f);
        }
#endif
    }
}
