using System;

using UnityEngine;

namespace Art.Resources.Character.PlayerCharactor.Spear_Man
{
    public class SpearManAnimationController : MonoBehaviour
    {
        
        public Rigidbody2D rb;
        public bool isGround;
       
        private PlayerController _playerController;
        public Status status;

        private void Start()
        {
            status = gameObject.GetComponent<Status>();
        }

        private void Update()
        {
            
            UpdateStatus();
        }

        public void UpdateStatus()
        {
            isGround = status.isGround  ;
        }
        

      
    }
}
