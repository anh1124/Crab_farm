using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Coin : MonoBehaviour
{
    public bool floating;
    private Animator animator;
    public float rayLength = 0.7f;  // Độ dài của raycast
    public float fallSpeed = 7f;  // Tốc độ rơi của coin
    public float groundCheckRadius = 0.1f; // Bán kính kiểm tra mặt đất

    public bool pickupAble;

    void Start()
    {
        pickupAble = true;
        // Gán animator cho component Animator của object này
        animator = GetComponent<Animator>();
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
        animator.Play("Stay");
    }

    public void StartRotate()
    {
        // Chơi animation "Rotate"
        animator.Play("Rotate");
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

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (pickupAble)
            {
                Status status = other.GetComponent<Status>();
                status.AddCoin(gameObject);
            }
        }
    }
}