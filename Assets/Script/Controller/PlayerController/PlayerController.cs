using Script.Game.Player;
using UnityEngine;
using Script.Controller;
using Script.Game.PlayerCharacter;
using Script.Game.Item.Coin;
using Script.Game.PlayerCharacter.Skills;
using Script.Audio;


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

        /// <summary>Single source of truth cho Skill. UI và hệ thống khác đọc qua đây.</summary>
        public PlayerSkillManager SkillManager { get; private set; }

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
            else if (Instance != this)
            {
                // Lỗi singleton: có nhiều hơn 1 PlayerController trong scene
                Debug.LogError("[PlayerController] Multiple instances detected! Destroying duplicate.");
                Destroy(gameObject);
                return;
            }
        }

        public void SetUpControllerPlayer()
        {
            rb = player.GetComponent<Rigidbody2D>();
            
            // ✅ QUAN TRỌNG: Bật nội suy để Player di chuyển mượt mà giữa các khung hình vật lý.
            // Điều này loại bỏ hiện tượng "nhảy cóc" vị trí, giúp Camera và Parallax hết giật.
            if (rb != null)
            {
                rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            }

            playerBehavior = player.GetComponent<PlayerBehavior>();
            SkillManager = player.GetComponent<PlayerSkillManager>();
            isActiveUpdate = true;

            // Tự động gắn PlayerAudioZone nếu chưa có
            if (player.GetComponent<PlayerAudioZone>() == null)
            {
                player.AddComponent<PlayerAudioZone>();
                Debug.Log("[PlayerController] Đã tự động gắn PlayerAudioZone lên player");
            }

            // Set player transform cho AudioManager (PlayerAudioZone sẽ làm việc này trong Start)
            if (Script.Audio.AudioManager.Instance != null)
            {
                Script.Audio.AudioManager.Instance.PlayerTransform = player.transform;
            }

            // Chuyển AudioListener sang player pivot (cho mono/stereo hoạt động đúng)
            if (Script.Audio.AudioListenerManager.Instance != null)
            {
                Script.Audio.AudioListenerManager.Instance.SetListenerToPlayer(player.transform);
            }
        }

        private void Update()
        {
            if (isActiveUpdate)
            {
                // ✅ Reset direction mặc định
                direction = 0;

                // ✅ Đọc Input trong Update để đảm bảo phản hồi tức thì và mượt mà
                if (InputManager.Instance != null)
                {
                    if (InputManager.Instance.MoveLeft) direction -= 1;
                    if (InputManager.Instance.MoveRight) direction += 1;
                    
                    // Debug log khi có input nhưng player không di chuyển (chỉ hiện khi có phím nhấn)
                    if (InputManager.Instance.MoveLeft || InputManager.Instance.MoveRight)
                    {
                        // Debug.Log($"[PlayerController] Input Detected: Dir={direction}, isGrounded={isGrounded}, canMove={canMove}, speed={moveSpeed}");
                    }
                }

                if (playerBehavior != null)
                {
                    // Lấy tốc độ di chuyển trực tiếp từ player.status
                    moveSpeed = playerBehavior.status.moveSpeed;
                    isGrounded = playerBehavior.HasHp;
                    
                    if (playerBehavior.currentInteractable != null)
                    {
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
                else
                {
                    isGrounded = false;
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
                if (rb != null)
                {
                    rb.velocity = new Vector2(direction * moveSpeed, rb.velocity.y);
                }
                else
                {
                    Debug.LogError("[PlayerController] Rigidbody2D is MISSING on player!");
                }
            }
            else if (direction != 0)
            {
                // Log cảnh báo lý do tại sao không di chuyển được dù có input
                Debug.LogWarning($"[PlayerController] Movement Blocked: direction={direction}, isGrounded={isGrounded}, canMove={canMove}, HasRB={(rb != null)}");
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
            Debug.Log("[PlayerController] UseCoinDownAction");
            coinButtonHeld = true;
            coinPressStartTime = Time.time;
            coinLogTimer = 0f;
        }
        public void UseCoinUpAction()
        {
            Debug.Log("[PlayerController] UseCoinUpAction");
            coinButtonHeld = false;

            float pressDuration = Time.time - coinPressStartTime;
            if (pressDuration < 0.5f)
            {
                if (playerBehavior != null && playerBehavior.Coin > 0)
                {
                    playerBehavior.SubtractCoin(1);
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
                    
                    if (playerBehavior != null)
                    {
                        var interaction = playerBehavior.GetComponent<PlayerInteraction>();
                        if (interaction != null) interaction.LogCurrentInteractables();
                    }

                    if (currentStructureShowCoinSlot != null)
                    {
                        coinUser = currentStructureShowCoinSlot.GetComponent<CoinUser>();
                        if (coinUser != null)
                        {
                            bool success = coinUser.ReceiveCoin(player);
                            if (!success)
                            {
                                Debug.Log("Không thể nạp xu (công trình đã đầy/không nhận hoặc player hết tiền/đang bận)");
                            }
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

            // Chuyển AudioListener về camera khi player chết
            if (Script.Audio.AudioListenerManager.Instance != null)
            {
                Script.Audio.AudioListenerManager.Instance.SetListenerToCamera();
            }
        }
    }
}
