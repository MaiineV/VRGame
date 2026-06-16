using Gameplay.Customer;
using Gameplay.Interactions;
using Gameplay.Liquid;
using TMPro;
using UnityEngine;

namespace UI
{
    /// <summary>
    /// Floating label over a seated customer's head. Shows the requested fill level (% + colour)
    /// and the glass's current level (% + colour) so the player matches the colours. Also flashes
    /// the serve result: liked, wrong, or left by timeout. Seat-anchored like TicketView.
    /// </summary>
    public sealed class CustomerOrderLabel : MonoBehaviour
    {
        [SerializeField] private CustomerSeatPoint _seat;
        [SerializeField] private TMP_Text _label;
        [SerializeField] private GameObject _root;          // visual to hide when empty
        [Tooltip("Offset above the customer root (world units).")]
        [SerializeField] private Vector3 _headOffset = new Vector3(0f, 2.6f, 0f);
        [SerializeField] private float _resultSeconds = 2f;

        private CustomerEntity _customer;
        private ServeSocket _socket;
        private Transform _cam;

        private float _resultTimer;
        private bool _served;

        // Cache so LateUpdate only rebuilds the (allocating) rich-text string when the displayed
        // value actually changes — not every frame. Sentinels force a rebuild on first show.
        private int _lastTargetLevel = int.MinValue;
        private int _lastGlassBucket = int.MinValue;

        void OnEnable()
        {
            if (_seat == null) return;
            _seat.CustomerBound += HandleBound;
            _seat.CustomerCleared += HandleCleared;
            if (_seat.CurrentCustomer != null) HandleBound(_seat.CurrentCustomer);
            else HandleCleared();
        }

        void OnDisable()
        {
            if (_seat != null)
            {
                _seat.CustomerBound -= HandleBound;
                _seat.CustomerCleared -= HandleCleared;
            }
            Unsubscribe();
        }

        private void HandleBound(CustomerEntity c)
        {
            Unsubscribe();
            _customer = c;
            _socket = c != null && c.Seat != null ? c.Seat.ServeSocket : null;
            _served = false;
            _resultTimer = 0f;
            InvalidateText();
            if (c != null)
            {
                c.Served += OnServed;
                c.Left += OnLeft;
            }
            SetRootActive(true);
        }

        private void HandleCleared()
        {
            Unsubscribe();
            _customer = null;
            _socket = null;
            if (_resultTimer <= 0f) SetRootActive(false); // keep showing a lingering result
        }

        private void Unsubscribe()
        {
            if (_customer != null)
            {
                _customer.Served -= OnServed;
                _customer.Left -= OnLeft;
            }
        }

        private void OnServed(CustomerEntity c, Data.Enums.RecipeId recipe, float score, bool isExact)
        {
            _served = true;
            if (isExact) ShowResult("☺ ¡Gracias!", FillLevels.Colors[0]);   // green
            else ShowResult("☹ ¡No es esto!", FillLevels.Colors[3]);        // red
        }

        private void OnLeft(CustomerEntity c, bool happy)
        {
            if (!_served) ShowResult("⏰ Se fue", new Color(0.8f, 0.8f, 0.85f));
        }

        private void ShowResult(string text, Color color)
        {
            // Built once per result (not per frame); the timer just holds it on screen.
            _label.color = Color.white;
            _label.text = $"<color=#{Hex(color)}>{text}</color>";
            _resultTimer = _resultSeconds;
            InvalidateText();
            SetRootActive(true);
        }

        void LateUpdate()
        {
            // Billboard every frame — cheap, no allocation.
            if (_customer != null)
                transform.position = _customer.transform.position + _headOffset;

            if (_cam == null && Camera.main != null) _cam = Camera.main.transform;
            if (_cam != null)
                transform.rotation = Quaternion.LookRotation(transform.position - _cam.position);

            if (_label == null) return;

            // Lingering serve result holds its (already-set) text until the timer runs out.
            if (_resultTimer > 0f)
            {
                _resultTimer -= Time.deltaTime;
                if (_resultTimer > 0f) return;
                if (_customer == null) { SetRootActive(false); return; }
                // Result ended with a customer still bound: fall through and rebuild the order.
            }

            if (_customer == null) return;

            // Only rebuild the string when the displayed values change. The glass fill is
            // continuous but the label shows discrete buckets, so compare the bucket index.
            int target = _customer.TargetLevel;
            bool hasGlass = _socket != null && _socket.CurrentGlass != null;
            int bucket = hasGlass ? FillLevels.BucketOf(_socket.CurrentGlass.FillRatio) : -1;

            if (target == _lastTargetLevel && bucket == _lastGlassBucket) return;
            _lastTargetLevel = target;
            _lastGlassBucket = bucket;

            _label.color = Color.white;
            int reqPct = FillLevels.PercentOf(target);
            string reqHex = Hex(FillLevels.ColorOf(target));

            if (bucket < 0)
            {
                _label.text = $"Pedido: <color=#{reqHex}>{reqPct}%</color>";
                return;
            }

            // Match by bucket, not raw percentages, so the player only chases the colour/check —
            // no more two ambiguous "100%" lines side by side.
            bool match = bucket == FillLevels.Clamp(target);
            int glassPct = FillLevels.PercentOf(bucket);
            string glassHex = Hex(FillLevels.ColorOf(bucket));
            string mark = match
                ? $"<color=#{Hex(FillLevels.Colors[0])}>✓</color>"
                : $"<color=#{Hex(FillLevels.Colors[3])}>✗</color>";
            _label.text = $"Pedido: <color=#{reqHex}>{reqPct}%</color>\n" +
                          $"Tu vaso: <color=#{glassHex}>{glassPct}%</color> {mark}";
        }

        /// <summary>Force the next LateUpdate to rebuild the order text.</summary>
        private void InvalidateText()
        {
            _lastTargetLevel = int.MinValue;
            _lastGlassBucket = int.MinValue;
        }

        private static string Hex(Color c) => ColorUtility.ToHtmlStringRGB(c);

        private void SetRootActive(bool active)
        {
            if (_root != null) _root.SetActive(active);
        }
    }
}
