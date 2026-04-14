using UnityEngine;
using Script.Game.Item.Coin;
using Script.Data_Model;

namespace Script.Game.Item
{
    /// <summary>
    /// Handles falling and throwing physics for PhysicalItem.
    /// Includes staggered raycast for performance optimization.
    /// </summary>
    [RequireComponent(typeof(PhysicalItem))]
    public class ItemFallBehavior : MonoBehaviour
    {
        private PhysicalItem _physicalItem;
        private Animator _animator;

        [Header("Physics State")]
        public bool floating;
        public float rayLength = 0.6f;
        public int fallSpeed = 10;

        [Header("Throwing Physics")]
        [Range(1f, 20f)]
        public float throwGravity = 9.81f;

        private Vector2 initialVelocity;
        private float throwTime;
        private Vector2 startPosition;
        private bool isThrown;

        private static LayerMask _groundMask;
        private static bool _groundMaskCached;
        private const int RAYCAST_INTERVAL = 3;
        private int _raycastFrameOffset;
        private bool _previousFloating = false;

        // Event for TownCenter pickup system
        public event System.Action<PhysicalItem> OnReadyForTownCenterPickup;

        private void Awake()
        {
            _physicalItem = GetComponent<PhysicalItem>();
        }

        private void Start()
        {
            fallSpeed = PrefabData.Instance.fallSpeed;

            if (_physicalItem.itemType == ItemType.Coin)
            {
                _animator = GetComponent<Animator>();
            }

            if (!_groundMaskCached)
            {
                _groundMask = Script.Utils.GameLayers.GroundMask;
                _groundMaskCached = true;
            }

            _raycastFrameOffset = Mathf.Abs(GetInstanceID()) % RAYCAST_INTERVAL;
        }

        public void ResetFallState()
        {
            floating = false;
            isThrown = false;
            throwTime = 0f;
            _previousFloating = false;
        }

        public void SetFloating(bool value)
        {
            floating = value;
            if (floating)
            {
                CheckPivotOnLanding();
            }
        }

        private void CheckPivotOnLanding()
        {
            if (transform.position.y < 0f)
            {
                transform.position = new Vector3(transform.position.x, 0f, transform.position.z);
            }
        }

        public void UpdateFallPhysics()
        {
            if (floating) return;

            if ((Time.frameCount + _raycastFrameOffset) % RAYCAST_INTERVAL != 0) return;

            if (isThrown)
            {
                ThrowMovement();
            }
            else
            {
                Falling();
            }

#if UNITY_EDITOR
            Debug.DrawRay(transform.position, Vector2.down * rayLength, Color.red);
#endif
        }

        public void Falling()
        {
            RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, rayLength, _groundMask);

            if (hit.collider != null)
            {
                if (!_previousFloating && !floating)
                {
                    OnReadyForTownCenterPickup?.Invoke(_physicalItem);
                }
                SetFloating(true);
                _previousFloating = floating;

                StartRotate();
            }
            else
            {
                transform.Translate(Vector2.down * fallSpeed * RAYCAST_INTERVAL * Time.deltaTime);
                _previousFloating = floating;
            }
        }

        public void InitializeThrow(Vector2 direction, float throwForce, float throwDistance)
        {
            isThrown = true;
            startPosition = transform.position;

            float timeToReachDistance = Mathf.Sqrt((2 * throwDistance) / throwForce);
            float velocityX = throwDistance / timeToReachDistance;
            float velocityY = throwForce * timeToReachDistance / 2;

            initialVelocity = new Vector2(direction.x * velocityX, velocityY);
            throwTime = 0;
            floating = false;
        }

        private void ThrowMovement()
        {
            throwTime += Time.deltaTime * RAYCAST_INTERVAL;

            float x = startPosition.x + initialVelocity.x * throwTime;
            float y = startPosition.y + (initialVelocity.y * throwTime) - (0.5f * throwGravity * throwTime * throwTime);

            transform.position = new Vector3(x, y, transform.position.z);

            RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, rayLength, _groundMask);
            if (hit.collider != null)
            {
                isThrown = false;
                SetFloating(true);
                StartRotate();
            }
        }

        public void StopRotate()
        {
            if (_animator != null)
            {
                _animator.Play("Stay");
            }
        }

        public void StartRotate()
        {
            if (_animator != null)
            {
                _animator.Play("Rotate");
            }
        }
    }
}
