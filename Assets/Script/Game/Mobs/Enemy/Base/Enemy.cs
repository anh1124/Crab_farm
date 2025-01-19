using System;
using Script.Game.Enemy.Interfaces;
using Unity.Mathematics;
using UnityEngine;

namespace Script.Game.Enemy.Base
{
    public class Enemy : MonoBehaviour, IDamageable, IMoveable
    {
        [field: SerializeField] public int maxHp { get; set; } = 100;
        public int CurrentHp { get; set; }
        public int MaxHp{ get; set; }
        public Rigidbody2D Rb { get; set; }
        public bool IsFacingRight { get; set; } = true;
        
        protected void Initialize()
        {
            CurrentHp = maxHp;
            Rb = GetComponent<Rigidbody2D>();
        }
        public void DoDamage(int damageAmount)
        {
            // do dame for wall or other mod
        }
        
        #region Health / Die Functions

       
        public void TakeDamage(int damageAmount, GameObject whoDoDame)
        {
            //add script show dame on screen
            Debug.Log(gameObject.name + "take dame " + damageAmount );
            if (damageAmount >= CurrentHp)
            {
                Die(whoDoDame);
            }
            else
            {
                CurrentHp -= damageAmount;
            }
        }

  

        public void Die(GameObject WhoDoDame)
        {
            //do animation die
            Destroy(gameObject);
        }

        #endregion    
        
        #region Movement Funcions
      
        public void MoveEnemy(Vector2 velocity)
        {
            Rb.velocity = velocity;
            CheckForLeftOrRightFacing(velocity);
        }

        public void CheckForLeftOrRightFacing(Vector2 velocity)
        {
            if (IsFacingRight && velocity.x < 0f)
            {
                var rotation = transform.rotation;
                Vector3 rotator = new Vector3(rotation.x, 180f , rotation.z);
                rotation = quaternion.Euler(rotator);
                transform.rotation = rotation;
                IsFacingRight = !IsFacingRight;
            }
            else if(!IsFacingRight && velocity.x >=0f)
            {
                var rotation = transform.rotation;
                Vector3 rotator = new Vector3(rotation.x, 0f , rotation.z);
                rotation = quaternion.Euler(rotator);
                transform.rotation = rotation;
                IsFacingRight = !IsFacingRight;
            }
        }
        
        #endregion

        #region Animation Trigger

        private void AnimationTriggerEvent(AnimationTriggerType triggerType)
        {
            //
        }

        public enum AnimationTriggerType
        {
            EnemyDamaged,
            PlayFootStepSound
        }
        
        #endregion
    }
}
