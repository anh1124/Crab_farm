using System;
using System.Collections;
using Script.Data_Model;
using Script.Data_Model.Interfaces;
using UnityEngine;

namespace Script.Game.Item.Coin
{
    public class Coin : MonoBehaviour, ISaveable
    {
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
        public float pickupAbleCooldown = 3f;
        private Coroutine _cooldownCoroutine;
    
   
    
    
        void Start()
        {
            fallSpeed = PrefabData.Instance.fallSpeed;
            pickupAbleCooldown = PrefabData.Instance.timeMaxFloatCoin;
            // Gán animator cho component Animator của object này
            _animator = GetComponent<Animator>();
            
            // Khởi động lại coroutine sau khi load game
            RestartCooldownsIfNeeded();
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
            // Chơi animation "Stay"
            _animator.Play("Stay");
        }

        public void StartRotate()
        {
            // Chơi animation "Rotate"
            _animator.Play("Rotate");
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
        }
        

        private void OnTriggerEnter2D(Collider2D other)
        {
            /*if (other.CompareTag("Player"))
            {
                HandlePlayerPickup(other);
            }*/
             if (other.CompareTag("Denizen"))
            {
                HandleDenizensPickup(other);
            }
        }

        public void HandlePlayerPickup(GameObject player)
        {
            if (!playerPickupAble) return;
            StopAllCoroutines();
            var coinable = player.GetComponent<ICoinable>();
            if (coinable != null)
            {
                coinable.PickUpCoin(gameObject);
            }
            else
            {
                Debug.LogWarning($"[Coin] Player {player.name} does not implement ICoinable.");
            }
        }

        private void HandleDenizensPickup(Collider2D denizens)
        {
            if (!denizensPickupAble) return;
            StopAllCoroutines();
            var coinable = denizens.GetComponent<ICoinable>();
            if (coinable != null)
            {
                coinable.PickUpCoin(gameObject);
            }
            else
            {
                Debug.LogWarning($"[Coin] Denizen {denizens.name} does not implement ICoinable.");
            }
        }
        
        #region Save System Implementation
        [System.Serializable]
        public class CoinData
        {
            public float positionX;
            public float positionY;
            public bool floating;
            public bool playerPickupAble;
            public bool denizensPickupAble;
        }
        
        public string GetPrefabID()
        {
            return "coin";
        }
        
        public object CaptureData()
        {
            return new CoinData
            {
                positionX = transform.position.x,
                positionY = transform.position.y,
                floating = this.floating,
                playerPickupAble = this.playerPickupAble,
                denizensPickupAble = this.denizensPickupAble
            };
        }

        public void RestoreData(object data)
        {
            CoinData coinData = (CoinData)data;
            transform.position = new Vector3(coinData.positionX, coinData.positionY, 0);
            floating = coinData.floating;
            playerPickupAble = coinData.playerPickupAble;
            denizensPickupAble = coinData.denizensPickupAble;
            
            // Khởi tạo animation dựa trên trạng thái floating
            if (_animator == null)
                _animator = GetComponent<Animator>();
                
            if (floating)
                StartRotate();
            else
                StopRotate();
        }
        #endregion
    }
}