using System;
using System.Collections;
using Script.Data_Model;
using Script.Data_Model.Interfaces;
using UnityEngine;

using Script.Game.Item;

namespace Script.Game.Item.Coin
{
    public enum ItemType { Coin, Tool_Bow, Tool_Hammer, Tool_Scythe }

    public class PhysicalItem : MonoBehaviour, ISaveable, IPickingUpMyItem
    {
        public ItemType itemType = ItemType.Coin;
        public GameObject pickedBy = null;
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
        public int fallSpeed =10 ;  // Tốc độ rơi của coin
    
        // Add new variables for throwing physics
        private Vector2 initialVelocity;
        private float throwTime;
        private Vector2 startPosition;
        private bool isThrown;
        [Header("Throwing Physics")]
        [Range(1f, 20f)]
        public float throwGravity = 9.81f;
    
        private Animator _animator;
        //private float groundCheckRadius = 0.1f; // Bán kính kiểm tra mặt đất
        // // Pickup abilities
       
        private Coroutine _cooldownCoroutine;

    
        void Start()
        {
            fallSpeed = PrefabData.Instance.fallSpeed;
            pickupAbleCooldown = PrefabData.Instance.timeMaxFloatCoin;
            
            // Chỉ lấy animator nếu type là Coin, vì các Tool hiện tại không có Animator
            if (itemType == ItemType.Coin)
            {
                _animator = GetComponent<Animator>();
            }
            
            // Khởi động lại coroutine sau khi load game
            RestartCooldownsIfNeeded();
        }

        private void OnEnable()
        {
            // Reset người nhặt mỗi khi object được bật (chống lỗi object pooling)
            pickedBy = null;

            
            if (itemType != ItemType.Coin && Script.Manager.MobsManager.ToolManager.Instance != null)
            {
                Script.Manager.MobsManager.ToolManager.Instance.RegisterTool(this);
            }
        }

        private void OnDisable()
        {
            if (itemType != ItemType.Coin && Script.Manager.MobsManager.ToolManager.Instance != null)
            {
                Script.Manager.MobsManager.ToolManager.Instance.UnregisterTool(this);
            }
        }

        /// <summary>
        /// Khởi động lại các coroutine cooldown nếu các trạng thái pickupAble là false sau khi load game
        /// Được gọi từ Start() và có thể gọi sau khi restore data
        /// </summary>
        private void RestartCooldownsIfNeeded()
        {
            // Khởi động lại coroutine cho player nếu cần
            if (!playerPickupAble)
            {
                StartCoroutine(Cooldown("Player"));
            }
            
            // Khởi động lại coroutine cho denizens nếu cần
            if (!denizensPickupAble)
            {
                StartCoroutine(Cooldown("Denizens"));
            }
            
            // Khởi động lại coroutine cho enemy nếu cần
            if (!enemyPickupAble)
            {
                StartCoroutine(Cooldown("Enemy"));
            }
        }

        void Update()
        {
            if (!floating)
            {
                if (isThrown)
                {
                    ThrowMovement();
                }
                else
                {
                    Falling();
                }
            }
            // Vẽ raycast trong Scene View
            Debug.DrawRay(transform.position, Vector2.down * rayLength, Color.red);

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
            // Vẽ raycast hướng xuống với chiều dài rayLength
            RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, rayLength, LayerMask.GetMask("Ground"));

            // Kiểm tra va chạm mặt đất dựa trên kết quả của raycast
            if (hit.collider != null)
            {
                // Dừng di chuyển xuống khi va chạm mặt đất
                floating = true;
                StartRotate();
            }
            else
            {
                // Di chuyển coin xuống dưới với tốc độ fallSpeed nếu không chạm đất
                StopRotate();
                transform.Translate(Vector2.down * fallSpeed * Time.deltaTime);
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
            throwTime += Time.deltaTime;
            
            // Calculate new position using projectile motion equations
            float x = startPosition.x + initialVelocity.x * throwTime;
            float y = startPosition.y + (initialVelocity.y * throwTime) - (0.5f * throwGravity * throwTime * throwTime);
            
            transform.position = new Vector3(x, y, transform.position.z);

            // Check for ground collision
            RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, rayLength, LayerMask.GetMask("Ground"));
            if (hit.collider != null)
            {
                isThrown = false;
                floating = true;
                StartRotate();
            }
        }
        #endregion
    
       

        public void CallFunctionCoolDown(string characterType)
        {
            StartCoroutine(Cooldown(characterType));
        }

        private IEnumerator Cooldown(string characterType)
        {
            yield return new WaitForSeconds(pickupAbleCooldown);
            if (characterType == "Player")
            {
                playerPickupAble = true;
            }
            else if (characterType == "Denizens")
            {
                denizensPickupAble = true;
            }
            else if (characterType == "Enemy")
            {
                enemyPickupAble = true;
            }

            // Tắt rồi bật lại Collider để Unity re-fire OnTriggerEnter2D
            // cho tất cả các denizen đang đứng trong vùng trigger.
            // Nếu không làm điều này, denizen đã vào trigger trước khi cooldown xong
            // sẽ không bao giờ nhặt được item.
            var col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.enabled = false;
                yield return null; // Chờ 1 frame để Unity process OnTriggerExit2D
                col.enabled = true; // → Ngay lập tức Unity fire OnTriggerEnter2D lại
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
            floating = itemData.floating;

            // Retrieve enum type
            if (System.Enum.TryParse(itemData.itemCategory, out ItemType parsedType))
            {
                itemType = parsedType;
            }

            // Khôi phục animation trạng thái
            if (floating)
                StartRotate();
            else
                StopRotate();
        }
        #endregion
    }
}