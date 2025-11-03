using Script.Game.Item.Coin;
using Script.Game.Player;
using UnityEngine;

namespace Script.Controller.PlayerController
{
    /// <summary>
    /// di chuyển đơn giản,chỉ có 2 hướng trái phải
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        public static PlayerController Instance { get; private set; }

        [Header("References")]
        public Rigidbody2D rb;
        public PlayerBehavior playerBehavior;
        public GameObject player;
        public int direction;
        public bool isGrounded;//vài skill dùng biến này(game chưa có nhảynhảy)
        public float moveSpeed;
        public bool isActiveUpdate;
        public bool isAlive = true;
        public bool damageAlly = false; // allow damaging ally for testing

        // Coin-use related
        public CoinUser coinUser;
        private TriggerCheckHelper triggerCheckHelper;
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
            triggerCheckHelper = player.GetComponent<TriggerCheckHelper>();
            isActiveUpdate = true;
        }

        private void Update()
        {
            if (isActiveUpdate)
            {
                moveSpeed = playerBehavior.MoveSpeed;
                // TODO: cập nhật grounded theo GroundCheck/physics nếu cần
                isGrounded = playerBehavior != null && playerBehavior.HasHp;
                if (triggerCheckHelper != null)
                {
                    currentStructureShowCoinSlot = triggerCheckHelper.nowStructureCollider2D;
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
            if (isGrounded)
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
