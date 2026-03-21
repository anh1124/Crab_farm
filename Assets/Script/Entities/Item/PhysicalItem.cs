using System;
using System.Collections;
using Script.Data_Model;
using Script.Data_Model.Interfaces;
using UnityEngine;

using Script.Game.Item;

namespace Script.Game.Item.Coin
{
    public enum ItemType { Coin, Tool_Bow, Tool_Hammer, Tool_Scythe }

    /*
     * VẬT PHẨM VẬT LÝ (Coin, Tools)
     * - Chức năng chính: Rơi tự do, ném parabol, tự đăng ký với ToolManager, Save/Load.
     * - Tối ưu hiệu năng: 
     *     + Staggered Update: Check Raycast mỗi 3 frame (chia đều tải CPU theo frame).
     *     + Early-Exit: Dừng rơi khi chạm đất (floating = true).
     *     + Static Caching: Cache LayerMask dùng chung cho tất cả instance.
     *     + Timer-based Cooldown: Thay thế coroutine bằng float timer — 0 GC alloc.
     */
    public class PhysicalItem : MonoBehaviour, ISaveable, IPickingUpMyItem
    {
        public ItemType itemType = ItemType.Coin;
        public GameObject pickedBy = null;
        public bool IsAvailable() => pickedBy == null; // Added to avoid lambda allocation in ToolManager

        public bool playerPickupAble = true;
        public bool denizensPickupAble  = true;
        public bool enemyPickupAble = false;
        public float pickupAbleCooldown = 3f;


        // IPickingUpMyItem implementation
        #region IPickingUpMyItem implementation

        public GameObject AssignedCarrier { get; set; }
        public void OnPickedByStranger(GameObject stranger)
        {
            // Người lạ vừa nhặt item này → báo ToolManager
            Script.Manager.MobsManager.ToolManager.Instance?.OnUnexpectedPickup(this, stranger);
        }

        // Gọi khi Villager được giao nhưng (nhặt đồ khác dọc đường)
        public void OnAssigneeAbandoned()
        {
            if (pickedBy != null) return; // Đã báo hoặc đã nhặt rồi
            AssignedCarrier = null;
            Script.Manager.MobsManager.ToolManager.Instance?.OnToolAbandoned(this);
        }
        #endregion
        
        #region physics
        [Header("Physics State")]
        public bool floating;
        public float rayLength = 0.6f;  // Độ dài của raycast
        public int fallSpeed = 10;  // Tốc độ rơi của coin
    
        // Add new variables for throwing physics
        private Vector2 initialVelocity;
        private float throwTime;
        private Vector2 startPosition;
        private bool isThrown;
        [Header("Throwing Physics")]
        [Range(1f, 20f)]
        public float throwGravity = 9.81f;

        // --- Performance: cache LayerMask & stagger raycast ---
        // Cache LayerMask một lần, tránh GetMask() mỗi frame
        private static LayerMask _groundMask;
        private static bool _groundMaskCached;

        // Mỗi coin check raycast ở frame khác nhau để trải đều tải
        // (stagger): check mỗi RAYCAST_INTERVAL frame, offset theo instanceId
        private const int RAYCAST_INTERVAL = 3;   // giảm xuống 2 nếu cần mượt hơn
        private int _raycastFrameOffset;           // offset riêng của từng coin
    
        private Animator _animator;

        // --- Timer-based Cooldown (thay thế coroutine WaitForSeconds, 0 GC alloc mỗi spawn) ---
        [Header("Pickup Cooldown Timers (Debug)")]
        [SerializeField] private float _playerPickupCooldown  = 0f;
        [SerializeField] private float _denizenPickupCooldown = 0f;
        [SerializeField] private float _enemyPickupCooldown   = 0f;

        // Cache Collider2D - tránh GetComponent trong Update
        private Collider2D _col;

        void Start()
        {
            fallSpeed = PrefabData.Instance.fallSpeed;
            pickupAbleCooldown = PrefabData.Instance.timeMaxFloatCoin;
            
            // Chỉ lấy animator nếu type là Coin, vì các Tool hiện tại không có Animator
            if (itemType == ItemType.Coin)
            {
                _animator = GetComponent<Animator>();
            }

            // Cache LayerMask một lần cho tất cả các instance (static)
            if (!_groundMaskCached)
            {
                _groundMask = Script.Utils.GameLayers.GroundMask;
                _groundMaskCached = true;
            }

            // Stagger: mỗi coin nhận 1 frame offset riêng để tải raycast trải đều
            // GetInstanceID() đảm bảo mỗi coin có offset khác nhau
            _raycastFrameOffset = Mathf.Abs(GetInstanceID()) % RAYCAST_INTERVAL;

            // Cache Collider2D để tránh GetComponent trong Update()
            _col = GetComponent<Collider2D>();

            // Khởi động lại timer cooldown nếu load game với pickupAble = false
            RestartCooldownsIfNeeded();
        }

        private void OnEnable()
        {
            ResetState();
            
            if (itemType != ItemType.Coin && Script.Manager.MobsManager.ToolManager.Instance != null)
            {
                Script.Manager.MobsManager.ToolManager.Instance.RegisterTool(this);
            }
        }

        /// <summary>
        /// Reset toàn bộ trạng thái của vật phẩm khi được tái sử dụng từ Pool.
        /// Đảm bảo không còn rác từ "kiếp trước".
        /// </summary>
        public void ResetState()
        {
            pickedBy = null;
            floating = false;
            isThrown = false;
            throwTime = 0f;
            
            // Mặc định cho phép nhặt (Spawner sẽ ghi đè nếu cần cooldown)
            playerPickupAble = true;
            denizensPickupAble = true;
            enemyPickupAble = false;

            // Reset timers: an toàn, không có side-effect như StopAllCoroutines()
            _playerPickupCooldown  = 0f;
            _denizenPickupCooldown = 0f;
            _enemyPickupCooldown   = 0f;
        }

        private void OnDisable()
        {
            if (itemType != ItemType.Coin && Script.Manager.MobsManager.ToolManager.Instance != null)
            {
                Script.Manager.MobsManager.ToolManager.Instance.UnregisterTool(this);
            }
        }

        /// <summary>
        /// Khởi động lại timer nếu đang cooldown SAU KHI LOAD GAME.
        /// KHÔNG set timer cho enemyPickupAble vì false là giá trị mặc định từ design,
        /// không phải trạng thái cooldown đang chạy.
        /// </summary>
        private void RestartCooldownsIfNeeded()
        {
            if (!playerPickupAble)   _playerPickupCooldown  = pickupAbleCooldown;
            if (!denizensPickupAble) _denizenPickupCooldown = pickupAbleCooldown;
            // enemyPickupAble = false là default design, KHÔNG cần timer restore
        }

        void Update()
        {
            // Cooldown timer luôn chạy dù floating hay không
            UpdateCooldowns();

            // Early-exit: khi đã chạm đất, không cần làm gì thêm về physics
            if (floating) return;

            // Stagger: chỉ chạy logic vật lý ở frame được phân công
            // Các coin khác nhau sẽ check ở các frame khác nhau → giảm raycast/frame
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
            // Chỉ vẽ debug ray trong Editor, không tốn tài nguyên khi build
            Debug.DrawRay(transform.position, Vector2.down * rayLength, Color.red);
#endif
        }

        /// <summary>
        /// Đếm ngược timer cooldown và bật lại pickupAble khi hết hạn.
        /// Khi hết hạn, toggle collider qua 1-frame coroutine để Unity re-fire OnTriggerEnter2D.
        /// Không dùng WaitForSeconds → không tạo GC alloc mỗi spawn.
        /// </summary>
        private void UpdateCooldowns()
        {
            bool anyExpired = false;

            if (_playerPickupCooldown > 0f)
            {
                _playerPickupCooldown -= Time.deltaTime;
                if (_playerPickupCooldown <= 0f)
                {
                    _playerPickupCooldown = 0f;
                    playerPickupAble = true;
                    anyExpired = true;
                }
            }

            if (_denizenPickupCooldown > 0f)
            {
                _denizenPickupCooldown -= Time.deltaTime;
                if (_denizenPickupCooldown <= 0f)
                {
                    _denizenPickupCooldown = 0f;
                    denizensPickupAble = true;
                    anyExpired = true;
                }
            }

            if (_enemyPickupCooldown > 0f)
            {
                _enemyPickupCooldown -= Time.deltaTime;
                if (_enemyPickupCooldown <= 0f)
                {
                    _enemyPickupCooldown = 0f;
                    enemyPickupAble = true;
                    anyExpired = true;
                }
            }

            // Toggle collider qua 1-frame coroutine để không trigger OnTriggerEnter2D ngay lập tức
            // trong cùng 1 frame (tránh cascade physics freeze).
            if (anyExpired && _col != null)
            {
                StartCoroutine(ToggleColliderNextFrame());
            }
        }

        /// <summary>
        /// Tắt collider 1 frame rồi bật lại → Unity re-fire OnTriggerEnter2D cho entity đứng trong vùng.
        /// Chỉ 1 alloc hiếm gặp (khi cooldown hết), không phải mỗi spawn.
        /// </summary>
        private IEnumerator ToggleColliderNextFrame()
        {
            if (_col != null) _col.enabled = false;
            yield return null; // Chờ 1 frame
            if (_col != null) _col.enabled = true;
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

        public void Falling()
        {
            // Dùng cached mask thay vì gọi lại Script.Utils.GameLayers mỗi lần
            RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, rayLength, _groundMask);

            if (hit.collider != null)
            {
                floating = true;
                StartRotate();
            }
            else
            {
                // Nhân thêm RAYCAST_INTERVAL để bù cho số frame bị bỏ qua
                transform.Translate(Vector2.down * fallSpeed * RAYCAST_INTERVAL * Time.deltaTime);
            }
        }

        public void InitializeThrow(Vector2 direction, float throwForce, float throwDistance)
        {
            isThrown = true;
            startPosition = transform.position;
            
            // Calculate initial velocity for desired distance
            float timeToReachDistance = Mathf.Sqrt((2 * throwDistance) / throwForce);
            float velocityX = throwDistance / timeToReachDistance;
            float velocityY = throwForce * timeToReachDistance / 2;

            initialVelocity = new Vector2(direction.x * velocityX, velocityY);
            throwTime = 0;
            floating = false;
        }

        private void ThrowMovement()
        {
            // Nhân thêm RAYCAST_INTERVAL để bù cho số frame bị bỏ qua
            throwTime += Time.deltaTime * RAYCAST_INTERVAL;
            
            float x = startPosition.x + initialVelocity.x * throwTime;
            float y = startPosition.y + (initialVelocity.y * throwTime) - (0.5f * throwGravity * throwTime * throwTime);
            
            transform.position = new Vector3(x, y, transform.position.z);

            // Dùng cached mask, tránh LayerMask.GetMask("Ground") mỗi frame
            RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, rayLength, _groundMask);
            if (hit.collider != null)
            {
                isThrown = false;
                floating = true;
                StartRotate();
            }
        }
        #endregion
    

        /// <summary>
        /// Đặt cooldown nhặt cho một loại đối tượng.
        /// Zero allocation — chỉ set float timer.
        /// </summary>
        public void CallFunctionCoolDown(string characterType)
        {
            switch (characterType)
            {
                case "Player":
                    playerPickupAble = false;
                    _playerPickupCooldown = pickupAbleCooldown;
                    break;
                case "Denizens":
                    denizensPickupAble = false;
                    _denizenPickupCooldown = pickupAbleCooldown;
                    break;
                case "Enemy":
                    enemyPickupAble = false;
                    _enemyPickupCooldown = pickupAbleCooldown;
                    break;
            }
        }

        //loại bỏ handpickup cũ vì không dùng
        
        #region Saveable Implementation
        [System.Serializable]
        public class PhysicalItemData
        {
            public float positionX;
            public float positionY;
            public bool floating;
            public string itemCategory; // Save enum type
        }
        
        public string GetPrefabID()
        {
            switch (itemType)
            {
                case ItemType.Coin: return "coin";
                case ItemType.Tool_Bow: return "bow";
                case ItemType.Tool_Hammer: return "hammer";
                default: return "coin";
            }
        }
        
        public object CaptureData()
        {
            return new PhysicalItemData
            {
                positionX = transform.position.x,
                positionY = transform.position.y,
                floating = floating,
                itemCategory = itemType.ToString() // Save enum type
            };
        }

        public void RestoreData(object data)
        {
            PhysicalItemData itemData = (PhysicalItemData)data;
            transform.position = new Vector3(itemData.positionX, itemData.positionY, 0);
            
            // Ép floating về false khi load để vật phẩm tự động rơi lại nếu đang ở trên không.
            // Nếu nó đã ở trên đất, logic Falling() sẽ set lại floating = true trong frame đầu tiên.
            floating = false;

            // Retrieve enum type
            if (System.Enum.TryParse(itemData.itemCategory, out ItemType parsedType))
            {
                itemType = parsedType;
            }

            // Dừng rotate để check lại ở Falling()
            StopRotate();
        }
        #endregion
    }
}