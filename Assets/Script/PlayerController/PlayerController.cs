using Script.Data_Model.Status;
using Script.Game.Item.Coin;
using UnityEngine;

namespace Script.PlayerController
{
    public class PlayerController : MonoBehaviour
    {
        public static PlayerController Instance { get; private set; }

        #region Helper script,component
        public CoinUser coinUser;
        private TriggerCheckHelper triggerCheckHelper;
      
        public Rigidbody2D rb;
        public PlayerStatus playerStatus;
        
        #endregion 
        public GameObject player;
        public int direction;
        public bool isGrounded;
        public float moveSpeed;

        public bool isActiveUpdate;
        
        public bool isAlive = true;
        
        private float _coinPressStartTime = 0f;
        private bool  _coinButtonHeld = false;
        private float _coinLogTimer = 0f;
        
        public GameObject currentStructureShowCoinSlot;
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }
        public void SetUpControllerPlayer()
        {
            GetPlayerCharactorComponent();
            isActiveUpdate = true;
        }
        
        public void GetPlayerCharactorComponent()
        {
            rb = player.GetComponent<Rigidbody2D>();
            triggerCheckHelper = player.GetComponent<TriggerCheckHelper>();
            playerStatus = player.GetComponent<PlayerStatus>();
            direction = 0;
            moveSpeed = playerStatus.moveSpeed;
        }
        
     
        
        private void Update()
        {
            if (isActiveUpdate)
            {
                UpdateStatus();
          
                currentStructureShowCoinSlot = triggerCheckHelper.nowStructureCollider2D;

            }
        }
        public void UpdateStatus()
        {
            //jumpForce = status.jumpForce;
            moveSpeed = playerStatus.moveSpeed;
            isGrounded = playerStatus.isGround;
        }
        
        
        private void FixedUpdate()
        {
            Movement(direction);
            //Jump();
            //FixedVelocityBug();
            UseCoinAction();
        }
        
        public void FixedVelocityBug()
        {
            /* hàm này được tạo ra do lỗi khi tạo collider méo 
             
            if (isGrounded && direction == 0)
            {
                //chuyeeen vận tốc của player thành 0
                Vector2 newVelocity = new Vector2(0, rb.velocity.y);
                rb.velocity = newVelocity;
            }
            */
        }

        public void Die()
        {
            isAlive = false;
            player.SetActive(false);
        }

        /*public void Jump()
        {
            if (isGrounded && jumpPress)
            {
                rb.AddForce(Vector2.up * jumpForce);
            }
        }*/

        #region moveAction

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

        public void MoveLeftDown()
        {
            direction = -1;
            
        }

        public void MoveLeftUp()
        {
            direction = 0;
            
        }

        public void MoveRightDown()
        {
            direction = 1;
        }

        public void MoveRightUp()
        {
            direction = 0;
        }

        /*public void MoveJumpUp()
        {
            jumpPress = false;
            
        }

        public void MoveJumpDown()
        {
            jumpPress = true;
            
        }*/
        #endregion
        public void UseCoinDownAction()
        {
            _coinButtonHeld = true;
            _coinPressStartTime = Time.time;
            _coinLogTimer = 0f; // Reset the timer when the button is pressed down
        }

        
        public void UseCoinUpAction()
        {
            _coinButtonHeld = false;

            float pressDuration = Time.time - _coinPressStartTime;
            if (pressDuration < 0.5f)
            {
                //Debug.LogWarning("dropCoin");
                if (playerStatus.Coin > 0)
                {
                    --playerStatus.Coin;
                    
                    Vector2 direction = new Vector2(player.transform.localScale.x, 0);
                    //dùng scalex của player để xác định nhân vật đang quay trái hay phải 
                    
                    Spawner.Instance.DropByPlayer(player.transform.position,1,direction);
                }
                else
                {
                    Debug.LogWarning("hết tiền rồi");
                }
               
            }
        }
        
        public void UseCoinAction()
        {
            if (_coinButtonHeld)
            {
                float pressDuration = Time.time - _coinPressStartTime;
                if (pressDuration >= 0.5f)
                {
                    if (Time.time - _coinLogTimer >= 0.5f)
                    {
                        _coinLogTimer = Time.time;

                        if (currentStructureShowCoinSlot != null)
                        {
                            coinUser = currentStructureShowCoinSlot.GetComponent<CoinUser>();
                            if (!coinUser.isReadyToUpgrade)
                            {
                                if (coinUser.collectedCoins <= coinUser.requiredCoins )//nếu số xu hiện tại ít hơn tổng xu có trong công trình hoặc player có xu 
                                {
                                    if (playerStatus.Coin > 0)
                                    {
                                        coinUser.InsertCoin(playerStatus.Coin , player);//truyền tham chiếu script coin của player vào 
                                        playerStatus.Coin--;
                                    }
                                    else
                                    {
                                        Debug.Log("đang nâng cấp nhưng không đủ tiền ");
                                    }
                                }
                            }
                            else
                            {
                                Debug.LogWarning("Công trình đã sẵn sàng nâng cấp");
                            }
                        }
                    }
                }
            }
        }

       
       
    }
}
