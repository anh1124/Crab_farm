using System;
using Script.Game.Item.Coin;
using Script.GUI_Controller.PlayerGui;
using UnityEngine;
using UnityEngine.Serialization;


public class PlayerController : MonoBehaviour
    {
        public static PlayerController Instance { get; private set; }
        
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


        
        public CoinUser coinUser;

        private PlayerTriggerShowCoinSlotHelper playerTriggerShowCoinSlotHelper;
        public TriggerHelper triggerHelper;
        
        public GameObject player;
        public Rigidbody2D rb;
        public Status status;
       
        
        public int direction;
        //public bool jumpPress;
        //public int jumpForce;
        public bool isGrounded;
        public int moveSpeed;
        public bool isDie = false;
        
        private float _coinPressStartTime = 0f;
        private bool _coinButtonHeld = false;
        private float _coinLogTimer = 0f;
        
        public GameObject currentStructureShowCoinSlot;
        
        private void Start()
        {
           
            rb = gameObject.GetComponentInChildren<Rigidbody2D>();
            player = rb.gameObject;
            triggerHelper = player.GetComponent<TriggerHelper>();
            playerTriggerShowCoinSlotHelper = player.GetComponent<PlayerTriggerShowCoinSlotHelper>();
            
            status = player.GetComponent<Status>();
            direction = 0;
            //jumpPress= false;
            
            //jumpForce = status.jumpForce;
            moveSpeed = status.moveSpeed;

        }

        public void UpdateStatus()
        {
            //jumpForce = status.jumpForce;
            moveSpeed = status.moveSpeed;
            isGrounded = status.isGround;

        }
        
        private void Update()
        {
            UpdateStatus();
          
            currentStructureShowCoinSlot = playerTriggerShowCoinSlotHelper.nowStructureCollider2D;
            //caapj nhaajt ground check
            //cap nhật player chét chua

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
            isDie = true;
            Destroy(player);
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
                if (status.coin > 0)
                {
                    --status.coin;
                    
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
                                    if (status.coin > 0)
                                    {
                                        coinUser.InsertCoin(status.coin , player);//truyền tham chiếu script coin của player vào 
                                        status.coin--;
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
