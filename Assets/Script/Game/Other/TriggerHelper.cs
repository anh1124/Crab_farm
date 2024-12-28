using System;
using System.Collections;
using System.Collections.Generic;
using Script.Data_Model;
using UnityEngine;

public class TriggerHelper : MonoBehaviour
{
    //script này sinh ra để giúp cho việc nhét tiền trong playeycontroller.đừng sờ vào nó
    
    
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
