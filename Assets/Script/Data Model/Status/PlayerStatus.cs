using Script.Data_Model.Interfaces;
using UnityEngine;

namespace Script.Data_Model.Status
{
    public class PlayerStatus : BaseStatus,IAttackable
    {
        public int AttackDame { get; set; }              // Attack power
        public float AttackSpeed { get; set; }      // Attack speed
    
        private void Start()
        {
            moveSpeed = PrefabData.Instance.playerSpeed;
        }
        public void Attack()
        {
        }
        public override void Die(GameObject whoDoDame)
        {
            base.Die(whoDoDame); 
        }
    }
}
