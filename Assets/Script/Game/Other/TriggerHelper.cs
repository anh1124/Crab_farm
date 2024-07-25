using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerHelper : MonoBehaviour
{
    public Collider2D structureCollider2D;
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Structure") )
        {
            structureCollider2D = other;
            //Debug.Log("Player entered structure trigger.");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Structure") )
        {
            structureCollider2D = null;
            //Debug.Log("Player exited structure trigger.");
        }
    }
}
