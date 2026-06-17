using UnityEngine;

namespace Gameplay.Liquid
{
    /// <summary>
    /// Visible liquid trickle between a bottle neck and the container below it.
    /// Driven by PourDetector — no per-frame ticking of its own.
    ///
    /// Setup: a thin child cylinder/capsule mesh pivoted at its top (origin at top cap,
    /// growing downward along +Z when un-rotated). Assigned to _mesh. Color is pushed to the
    /// renderer via MaterialPropertyBlock using the "_BaseColor" property (URP lit).
    /// </summary>
    public sealed class PourStream : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private Transform _mesh;
        [SerializeField] private Renderer _renderer;
        [Tooltip("Radius of the stream (world units). Applied to local X/Y scale of the mesh.")]
        [SerializeField] private float _radius = 0.006f;
        [Tooltip("Min length to render the stream, below which it is hidden.")]
        [SerializeField] private float _minLength = 0.01f;

        private MaterialPropertyBlock _block;
        private bool _visible;

        void Awake()
        {
            _block = new MaterialPropertyBlock();
            SetVisible(false);
        }

        /// <summary>Show the stream from 'from' to 'to' tinted by 'color'.</summary>
        public void Show(Vector3 from, Vector3 to, Color color)
        {
            if (_mesh == null) return;

            var delta = to - from;
            float length = delta.magnitude;
            if (length < _minLength) { SetVisible(false); return; }

            transform.position = from;
            transform.rotation = Quaternion.FromToRotation(Vector3.down, delta / length);
            _mesh.localScale = new Vector3(_radius, _radius, length);

            if (_renderer != null)
            {
                _renderer.GetPropertyBlock(_block);
                _block.SetColor(BaseColorId, color);
                _renderer.SetPropertyBlock(_block);
            }

            SetVisible(true);
        }

        public void Hide() => SetVisible(false);

        private void SetVisible(bool v)
        {
            if (_visible == v) return;
            _visible = v;
            if (_mesh != null && _mesh.gameObject.activeSelf != v)
                _mesh.gameObject.SetActive(v);
        }
    }
}
