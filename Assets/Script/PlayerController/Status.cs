using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Status : MonoBehaviour
{
    public int hp;
    public int jumpForce;
    public int moveSpeed;

    public bool isGround;
    
    public LayerMask groundLayer;
    public Collider2D groundCheckCollider2D;//is trigger
    private void Update()
    {
        GroundCheck();
    }
    public void GroundCheck()
    {
        // Sử dụng OverlapCircle để kiểm tra sự va chạm với lớp groundLayer
        var bounds = groundCheckCollider2D.bounds;
        isGround = Physics2D.OverlapCircle(bounds.center, bounds.extents.x, groundLayer) != null;
        
    }
}
