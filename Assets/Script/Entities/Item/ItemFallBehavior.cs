using UnityEngine;
using Script.Game.Item.Coin;
using Script.Data_Model;

namespace Script.Game.Item
{
    /// <summary>
    /// Handles falling and throwing physics for PhysicalItem.
    /// Cung cấp 3 trạng thái: FALLING (Dynamic), STATIONARY (Kinematic), CONTROLLED (Kinematic - No Collision)
    /// </summary>
    [RequireComponent(typeof(PhysicalItem))]
    [RequireComponent(typeof(Rigidbody2D))]
    public class ItemFallBehavior : MonoBehaviour
    {
        private PhysicalItem _physicalItem;
        private Animator _animator;
        private Rigidbody2D _rb;

        [Header("Physics State")]
        public bool floating; // Đồng bộ với PhysicalItem.floating
        public int fallSpeed = 10;

        [Header("Throwing Physics")]
        [Range(1f, 20f)]
        public float throwGravity = 9.81f;

        private Vector2 _initialVelocity;
        private float _throwTime;
        private Vector2 _startPosition;
        private bool _isThrown;

        private static LayerMask _groundMask;
        private static bool _groundMaskCached;

        private bool _canLand = false;
        private int _framesSinceReset = 0;
        private const int LANDING_DELAY_FRAMES = 3;

        public event System.Action<PhysicalItem> OnReadyForTownCenterPickup;

        private void Awake()
        {
            _physicalItem = GetComponent<PhysicalItem>();
            _rb = GetComponent<Rigidbody2D>();

            if (_rb != null)
            {
                _rb.gravityScale = 0f;
                _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            }

            if (!_groundMaskCached)
            {
                _groundMask = Script.Utils.GameLayers.GroundMask;
                _groundMaskCached = true;
            }
        }

        private void Start()
        {
            fallSpeed = PrefabData.Instance.fallSpeed;
            if (_physicalItem.itemType == ItemType.Coin) _animator = GetComponent<Animator>();
        }

        #region API Trạng thái (3 State Logic)

        /// <summary>
        /// STATE 1: FALL - Bật Dynamic để rơi theo vật lý.
        /// Dùng khi: Vừa spawn, bị release từ transit, bị ném.
        /// </summary>
        public void SetFalling(bool isThrown = false)
        {
            floating = false;
            _isThrown = isThrown;
            _canLand = false;
            _framesSinceReset = 0;

            if (_rb != null)
            {
                _rb.bodyType = RigidbodyType2D.Dynamic;
                _rb.simulated = true;
                _rb.velocity = isThrown ? _rb.velocity : Vector2.zero;
            }
        }

        /// <summary>
        /// STATE 2: STATIONARY - Chuyển sang Kinematic để nằm im.
        /// Dùng khi: Chạm đất, load game (đang ở trên đất), đặt trên kệ vendor.
        /// </summary>
        public void SetStationary()
        {
            floating = true;
            _isThrown = false;
            
            if (_rb != null)
            {
                _rb.velocity = Vector2.zero;
                _rb.bodyType = RigidbodyType2D.Kinematic;
                _rb.simulated = true; // Vẫn simulated để Trigger pickup hoạt động
            }
            CheckPivotOnLanding();
        }

        /// <summary>
        /// STATE 3: CONTROLLED - Chuyển sang Kinematic để script ngoài điều khiển position.
        /// Dùng khi: CoinUser đang kéo coin bay vào slot.
        /// </summary>
        public void SetControlled()
        {
            floating = true;
            _isThrown = false;

            if (_rb != null)
            {
                _rb.velocity = Vector2.zero;
                _rb.bodyType = RigidbodyType2D.Kinematic;
                // Có thể tắt va chạm ở đây nếu cần, nhưng thường script điều khiển sẽ tắt Collider
            }
        }

        #endregion

        // Tương thích với code cũ
        public void ResetFallState() => SetFalling(false);
        public void SetFloating(bool value)
        {
            if (value) SetStationary();
            else SetFalling(false);
        }

        private void CheckPivotOnLanding()
        {
            if (transform.position.y < 0f)
            {
                transform.position = new Vector3(transform.position.x, 0f, transform.position.z);
            }
        }

        private void OnCollisionEnter2D(Collision2D col)
        {
            if (!_canLand || floating) return;
            if (((1 << col.gameObject.layer) & _groundMask) == 0) return;

            if (!_isThrown || _rb.velocity.y <= 0f)
            {
                OnReadyForTownCenterPickup?.Invoke(_physicalItem);
                SetStationary();
                StartRotate();
            }
        }

        public void UpdateFallPhysics()
        {
            if (floating) return;

            if (!_canLand)
            {
                _framesSinceReset++;
                if (_framesSinceReset >= LANDING_DELAY_FRAMES) _canLand = true;
            }

            if (_isThrown) ThrowMovement();
            else if (_rb != null && _rb.simulated) _rb.velocity = Vector2.down * fallSpeed;
        }

        public void InitializeThrow(Vector2 direction, float throwForce, float throwDistance)
        {
            SetFalling(true);
            _startPosition = transform.position;

            if (_rb != null)
            {
                float timeToReachDistance = Mathf.Sqrt((2 * throwDistance) / throwForce);
                float vx = throwDistance / timeToReachDistance;
                float vy = throwForce * timeToReachDistance / 2;
                _initialVelocity = new Vector2(direction.x * vx, vy);
                _throwTime = 0;
                _rb.velocity = _initialVelocity;
            }
        }

        private void ThrowMovement()
        {
            _throwTime += Time.deltaTime;
            float vx = _initialVelocity.x;
            float vy = _initialVelocity.y - throwGravity * _throwTime;
            if (_rb != null && _rb.simulated) _rb.velocity = new Vector2(vx, vy);
        }

        public void StopRotate() { if (_animator != null) _animator.Play("Stay"); }
        public void StartRotate() { if (_animator != null) _animator.Play("Rotate"); }
    }
}
