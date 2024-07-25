using System;
using Script.GUI_Controller.PlayerGui;
using UnityEngine;


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

        
        public GameObject coin;
        public Collider2D currentStructureCollider;
        
        private void Start()
        {
           
            rb = gameObject.GetComponentInChildren<Rigidbody2D>();
            player = rb.gameObject;
            triggerHelper = player.GetComponent<TriggerHelper>();
            
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
            currentStructureCollider = triggerHelper.structureCollider2D;
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
                    rb.transform.Translate( Vector2.left * moveSpeed * Time.deltaTime);
                
                }
                if (direction == 1)
                {
                    rb.transform.Translate( Vector2.right * moveSpeed * Time.deltaTime);
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
                Debug.LogWarning("dropCoin");
                --status.coin;
            }
        }

        /*public void UseCoinAction()
        {
            if (_coinButtonHeld)
            {
                float pressDuration = Time.time - _coinPressStartTime;
                if (pressDuration >= 0.5f)
                {
                    if (Time.time - _coinLogTimer >= 0.5f)
                    {
                        Debug.Log("use Coin");
                        _coinLogTimer = Time.time; // Update the timer
                    }
                }
            }
        }*/
        
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

                        if (currentStructureCollider != null)
                        {
                            TownCentrer townCentrer = currentStructureCollider.GetComponent<TownCentrer>();
                            if (!townCentrer.readyToUpgrade)
                            {
                                if (townCentrer.currentCoinCount <= townCentrer.coinQuantity)
                                {
                                    townCentrer.InsertCoin(status , player);//truyền tham chiếu script coin của player vào 
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
