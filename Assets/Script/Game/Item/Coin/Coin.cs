using System;
using System.Collections;
using Script.Data_Model;
using UnityEngine;

namespace Script.Game.Item.Coin
{
    public class Coin : MonoBehaviour
    {
        public bool floating;
        public float rayLength = 0.6f;  // Độ dài của raycast
        public int fallSpeed =10 ;  // Tốc độ rơi của coin
    
    
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
        }

        void Update()
        {
            if (!floating)
            {
                Falling();
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
             if (other.CompareTag("Denizens"))
            {
                HandleDenizensPickup(other);
            }
        }

        public void HandlePlayerPickup(GameObject player)
        {
            if (playerPickupAble)
            {
                StopAllCoroutines();
                BaseStatus playerStatus = player.GetComponent<BaseStatus>();
                playerStatus.AddCoin(gameObject); // This method should handle coin destruction
            }
        }

        private void HandleDenizensPickup(Collider2D denizens)
        {
            if (denizensPickupAble)
            {
                StopAllCoroutines();
                BaseStatus denizensStatus = denizens.GetComponent<BaseStatus>();
                denizensStatus.AddCoin(gameObject); // This method should handle coin destruction
            }
        }
    }
}