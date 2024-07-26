using System.Collections;
using UnityEngine;

namespace Script.Game.Item.Coin
{
    public class Coin : MonoBehaviour
    {
        public bool floating;
        public float rayLength =1;  // Độ dài của raycast
        public float fallSpeed =10 ;  // Tốc độ rơi của coin
    
    
        private Animator _animator;
        private float groundCheckRadius = 0.1f; // Bán kính kiểm tra mặt đất
        // // Pickup abilities
        public bool playerPickupAble = true;
        public bool denizensPickupAble  = true;
        public float pickupAbleCooldown = 3f;
        private Coroutine _cooldownCoroutine;
    
   
    
    
        void Start()
        {
            
            // Gán animator cho component Animator của object này
            _animator = GetComponent<Animator>();
        }

        void Update()
        {
            if (!floating)
            {
                Falling();
            }
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
            // Vẽ raycast hướng xuống
            RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, rayLength);
        
            // Vẽ raycast trong Scene View
            Debug.DrawRay(transform.position, Vector2.down * rayLength, Color.red);

            // Kiểm tra va chạm mặt đất bằng OverlapCircle
            bool isGrounded = Physics2D.OverlapCircle(transform.position - new Vector3(0, rayLength / 2, 0), groundCheckRadius, LayerMask.GetMask("Ground"));

            if (isGrounded)
            {
                // Dừng di chuyển xuống
                floating = true;
                StartRotate();
            }
            else
            {
                // Di chuyển coin xuống dưới với tốc độ fallSpeed
                StopRotate();
                transform.Translate(Vector2.down * fallSpeed * Time.deltaTime);
            }
        }

    
       

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
            if (other.CompareTag("Player"))
            {
                HandlePlayerPickup(other);
            }
            else if (other.CompareTag("Denizens"))
            {
                HandleDenizensPickup(other);
            }
        }

        private void HandlePlayerPickup(Collider2D player)
        {
            if (playerPickupAble)
            {
                StopAllCoroutines();
                Status playerStatus = player.GetComponent<Status>();
                playerStatus.AddCoin(gameObject); // This method should handle coin destruction
            }
        }

        private void HandleDenizensPickup(Collider2D denizens)
        {
            if (denizensPickupAble)
            {
                StopAllCoroutines();
                Status denizensStatus = denizens.GetComponent<Status>();
                denizensStatus.AddCoin(gameObject); // This method should handle coin destruction
            }
        }
    }
}