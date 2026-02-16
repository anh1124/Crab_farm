using Script.Game.Player;
using UnityEngine;
using Script.Game.PlayerCharacter;
using Script.Game.Item.Coin;

namespace Script.Controller.PlayerController
{
    /// <summary>
    /// di chuyển đơn giản,chỉ có 2 hướng trái phải
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        public static PlayerController Instance { get; private set; }

        [Header("Player Reference")]
        [Tooltip("Reference tới Player GameObject (sẽ được gán khi spawn)")]
        public GameObject player;
        
        [Tooltip("Reference tới PlayerBehavior (tự đăng ký khi player spawn)")]
        public PlayerBehavior playerBehavior;
        public Rigidbody2D rb;
        public int direction;
        public bool isGrounded;//vài skill dùng biến này(game chưa có nhảynhảy)
        public float moveSpeed;
        public bool isActiveUpdate;
        public bool isAlive = true;
        public bool canMove = true; // Cho phép khóa di chuyển
        public bool damageAlly = false; // allow damaging ally for testing
        
        [Header("Skill System")]
        [Tooltip("Kéo GameObject container rỗng (child của Player) vào đây. Hitbox của skill sẽ được spawn vào đây.")]
        public Transform skillColliderContainer;

        // Coin-use related
        public CoinUser coinUser;
        private float coinPressStartTime = 0f;
        private bool coinButtonHeld = false;
        private float coinLogTimer = 0f;
        public GameObject currentStructureShowCoinSlot;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                //lỗi singleton
                Debug.LogError("PlayerController already exists");
                //Destroy(gameObject);
            }


        }

        public void SetUpControllerPlayer()
        {
            rb = player.GetComponent<Rigidbody2D>();
            playerBehavior = player.GetComponent<PlayerBehavior>();
            isActiveUpdate = true;
        }

        private void Update()
        {
            if (isActiveUpdate)
            {
                // Lấy tốc độ di chuyển trực tiếp từ player.status
                moveSpeed = playerBehavior.status.moveSpeed;
                // TODO: cập nhật grounded theo GroundCheck/physics nếu cần
                isGrounded = playerBehavior != null && playerBehavior.HasHp;
                
                // Lấy currentStructureShowCoinSlot từ currentInteractable của PlayerBehavior
                if (playerBehavior != null && playerBehavior.currentInteractable != null)
                {
                    // Cast về MonoBehaviour để lấy gameObject
                    if (playerBehavior.currentInteractable is MonoBehaviour mb)
                    {
                        currentStructureShowCoinSlot = mb.gameObject;
                    }
                }
                else
                {
                    currentStructureShowCoinSlot = null;
                }
            }
        }

        private void FixedUpdate()
        {
            Movement(direction);
            UseCoinAction();
        }

        public void Movement(int direction)
        {
            if (isGrounded && canMove)
            {
                if (direction == -1)
                {
                    rb.velocity = Vector2.left * moveSpeed;
                }
                if (direction == 1)
                {
                    rb.velocity = Vector2.right * moveSpeed;
                }
            }
        }

        #region button press methods
        public void MoveLeftDown() => direction = -1;
        public void MoveLeftUp() => direction = 0;
        public void MoveRightDown() => direction = 1;
        public void MoveRightUp() => direction = 0;

        // Hooks for UI coin button
        public void UseCoinDownAction()
        {
            coinButtonHeld = true;
            coinPressStartTime = Time.time;
            coinLogTimer = 0f;
        }
        public void UseCoinUpAction()
        {
            coinButtonHeld = false;

            float pressDuration = Time.time - coinPressStartTime;
            if (pressDuration < 0.5f)
            {
                if (playerBehavior != null && playerBehavior.Coin > 0)
                {
                    playerBehavior.Coin -= 1;
                    Vector2 directionVec = new Vector2(player.transform.localScale.x, 0);
                    Spawner.Instance.DropByPlayer(player.transform.position, 1, directionVec);
                }
                else
                {
                    Debug.LogWarning("hết tiền rồi");
                }
            }
        }

        private void UseCoinAction()
        {
            if (!coinButtonHeld) return;

            float pressDuration = Time.time - coinPressStartTime;
            if (pressDuration >= 0.5f)
            {
                if (Time.time - coinLogTimer >= 0.5f)
                {
                    coinLogTimer = Time.time;

                    if (currentStructureShowCoinSlot != null)
                    {
                        coinUser = currentStructureShowCoinSlot.GetComponent<CoinUser>();
                        if (coinUser != null && !coinUser.isReadyToUpgrade)
                        {
                            if (coinUser.collectedCoins <= coinUser.requiredCoins)
                            {
                                if (playerBehavior != null && playerBehavior.Coin > 0)
                                {
                                    coinUser.InsertCoin(playerBehavior.Coin, player);
                                    playerBehavior.Coin--;
                                }
                                else
                                {
                                    Debug.Log("đang nâng cấp nhưng không đủ tiền ");
                                }
                            }
                        }
                        else if (coinUser != null && coinUser.isReadyToUpgrade)
                        {
                            Debug.LogWarning("Công trình đã sẵn sàng nâng cấp");
                        }
                    }
                }
            }
        }

        #endregion
        
        //hàm dưới se duoc goi boi playerbehavior
        public void Die()
        {
            isAlive = false;
            player.SetActive(false);
        }
    }
}
