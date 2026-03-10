using System;
using System.Collections;
using System.Collections.Generic;
using Script.Data_Model;
using UnityEngine;

public class TriggerHelper : MonoBehaviour
{
    //script này dùng để kiểm tra trigger với layer thuộc list layer playerCoinUserInteractable
    
    
    public Collider2D structureCollider2D;
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Kiểm tra nếu tag của collider nằm trong danh sách tags
        if (PrefabData.Instance.playerCoinUserInteractable.Contains(other.tag))
        {
            structureCollider2D = other;
            // Debug.Log("Player entered structure trigger.");
        }
    }
    private void OnTriggerExit2D(Collider2D other)
    {
        // Kiểm tra nếu tag của collider nằm trong danh sách tags
        if (PrefabData.Instance.playerCoinUserInteractable.Contains(other.tag))
        {
            structureCollider2D = null;
            // Debug.Log("Player exited structure trigger.");
        }
    }
    
}
