using System;
using System.Collections;
using Script.Data_Model;
using Script.Data_Model.Interfaces;
using UnityEngine;

namespace Script.Game.Item.Coin
{
    public enum ItemCategory { Coin, Tool_Bow, Tool_Hammer, Tool_Scythe }

    public class PhysicalItem : MonoBehaviour, ISaveable
    {
        [Header("Item Config")]
        public ItemCategory itemCategory = ItemCategory.Coin;
        public string prefabID = "coin";

        [Header("Tool State (Only if Category != Coin)")]
        [Tooltip("True when a villager has picked up this tool")]
        public bool picked = false;
        [Tooltip("Reference to the villager who picked up this tool")]
        public GameObject pickedBy;

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
        public bool playerPickupAble = true;
        public bool denizensPickupAble  = true;
        public bool enemyPickupAble = false;
        public float pickupAbleCooldown = 3f;
        private Coroutine _cooldownCoroutine;
    
   
    
    
        void Start()
        {
            fallSpeed = PrefabData.Instance.fallSpeed;
            pickupAbleCooldown = PrefabData.Instance.timeMaxFloatCoin;
            
            // Chỉ lấy animator nếu category là Coin, vì các Tool hiện tại không có Animator
            if (itemCategory == ItemCategory.Coin)
            {
                _animator = GetComponent<Animator>();
            }
            
            // Khởi động lại coroutine sau khi load game
            RestartCooldownsIfNeeded();
        }

        private void OnEnable()
        {
            if (itemCategory != ItemCategory.Coin && Script.Manager.MobsManager.ToolManager.Instance != null)
            {
                Script.Manager.MobsManager.ToolManager.Instance.RegisterTool(this);
            }
        }

        private void OnDisable()
        {
            if (itemCategory != ItemCategory.Coin && Script.Manager.MobsManager.ToolManager.Instance != null)
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

    
        //cái này phải sửa lại,nhỡ đâu player drop xong detizen nhặt rồi player lại drop 

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
        }
        

        // Removed OnTriggerEnter2D so LootPickupTrigger strictly manages pick up interactions.

        public void HandlePlayerPickup(GameObject player)
        {
            //Debug.Log($"[Coin] HandlePlayerPickup called for {player.name}, playerPickupAble: {playerPickupAble}");
            
            if (!playerPickupAble)
            {
                //Debug.Log($"[Coin] Player pickup blocked - playerPickupAble is false");
                return;
            }
            
            StopAllCoroutines();
            var coinable = player.GetComponent<ICoinable>();
            if (coinable != null)
            {
                //Debug.Log($"[Coin] Calling ICoinable.PickUpCoin on player {player.name}");
                coinable.PickUpCoin(gameObject);
            }
            else
            {
                Debug.LogError($"[Coin] Player {player.name} does not implement ICoinable.");
            }
        }

        private void HandleDenizensPickup(Collider2D denizens)
        {
           // Debug.Log($"[Coin] HandleDenizensPickup called for {denizens.name}, denizensPickupAble: {denizensPickupAble}");
            
            if (!denizensPickupAble)
            {
                //Debug.Log($"[Coin] Pickup blocked - denizensPickupAble is false");
                return;
            }
            
            StopAllCoroutines();
            var coinable = denizens.GetComponent<ICoinable>();
            if (coinable != null)
            {
                //Debug.Log($"[Coin] Calling ICoinable.PickUpCoin on {denizens.name}");
                coinable.PickUpCoin(gameObject);
            }
            else
            {
                Debug.LogWarning($"[PhysicalItem] Denizen {denizens.name} does not implement ICoinable.");
            }
        }

        private void HandleEnemyPickup(GameObject enemy)
        {
            if (!enemyPickupAble) return;
            
            StopAllCoroutines();
            var coinable = enemy.GetComponent<ICoinable>();
            if (coinable != null)
            {
                coinable.PickUpCoin(gameObject);
            }
        }
        
        #region Saveable Implementation
        [System.Serializable]
        public class PhysicalItemData
        {
            public float positionX;
            public float positionY;
            public bool floating;
            public string itemCategory; // Save enum type
            public bool picked;
        }
        
        public string GetPrefabID()
        {
            return prefabID;
        }
        
        public object CaptureData()
        {
            return new PhysicalItemData
            {
                positionX = transform.position.x,
                positionY = transform.position.y,
                floating = floating,
                itemCategory = itemCategory.ToString(), // Save enum type
                picked = picked
            };
        }

        public void RestoreData(object data)
        {
            PhysicalItemData itemData = (PhysicalItemData)data;
            transform.position = new Vector3(itemData.positionX, itemData.positionY, 0);
            floating = itemData.floating;

            // Retrieve enum type
            if (System.Enum.TryParse(itemData.itemCategory, out ItemCategory parsedType))
            {
                itemCategory = parsedType;
            }
            picked = itemData.picked;

            // Khôi phục animation trạng thái
            if (floating)
                StartRotate();
            else
                StopRotate();
        }
        #endregion
    }
}