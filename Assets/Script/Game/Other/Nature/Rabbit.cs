using System;
using Script.Data_Model;
using UnityEngine;
namespace Script.Game.Other.Nature
{
    public class Rabbit: MonoBehaviour
    {
        /*
         * thỏ chỉ có 2 animation là die và move,
         */
        /*
         *tag LootMob
         *layer Mobs
         */
        public int rabbitHp;
        public int rabbitSpeed;
        public int rabbitDieReward;

        public GameObject rabbitBush;
        private void Start()
        {
            rabbitHp = PrefabData.Instance.rabbitHp;
            rabbitSpeed = PrefabData.Instance.rabbitSpeed;
            rabbitDieReward = PrefabData.Instance.rabbitDieReward;
            
        }

        public void takeDame()
        {
            
        }
    }
}