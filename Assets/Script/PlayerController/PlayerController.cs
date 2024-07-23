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
        
        public GameObject player;
        public Rigidbody2D rb;
        public Status status;
       
        
        public int direction;
        //public bool jumpPress;
        public bool isGrounded;
        
        //public int jumpForce;
        public int moveSpeed;
        
        public bool isDie = false;

        private bool useCoin = false;
        private float coinPressStartTime = 0f;
        private bool coinButtonHeld = false;
        private float coinLogTimer = 0f;

        private void Start()
        {
           
            rb = gameObject.GetComponentInChildren<Rigidbody2D>();
            player = rb.gameObject;

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
        public void UseCoinDownAction()
        {
            useCoin = true;
            coinButtonHeld = true;
            coinPressStartTime = Time.time;
            coinLogTimer = 0f; // Reset the timer when the button is pressed down
        }

        public void UseCoinUpAction()
        {
            useCoin = false;
            coinButtonHeld = false;

            float pressDuration = Time.time - coinPressStartTime;
            if (pressDuration < 0.5f)
            {
                Debug.LogWarning("dropCoin");
            }
        }

        public void UseCoinAction()
        {
            if (coinButtonHeld)
            {
                float pressDuration = Time.time - coinPressStartTime;
                if (pressDuration >= 0.5f)
                {
                    if (Time.time - coinLogTimer >= 0.5f)
                    {
                        Debug.Log("use Coin");
                        coinLogTimer = Time.time; // Update the timer
                    }
                }
            }
        }
    }
