using System;
using Script.Data_Model;
using UnityEngine;
using UnityEngine.Serialization;

namespace Script.Game.Other.Nature
{
    public class Rabbit : MonoBehaviour
    {
        /*
         * thỏ chỉ có 2 animation là die và move,
         */
        /*
         *tag LootMob
         *layer Mobs
         */
        public int hp;
        public int speed;
        public int rabbitDieReward;

        public GameObject rabbitBush;

        private void Start()
        {
            hp = PrefabData.Instance.rabbitHp;
            speed = PrefabData.Instance.rabbitSpeed;
            rabbitDieReward = PrefabData.Instance.rabbitDieReward;
        }
        
        public void StayNearBush()
        {
            if (rabbitBush == null) return;

            float distanceFromBush = Vector2.Distance(transform.position, rabbitBush.transform.position);

            // Nếu khoảng cách lớn hơn 5, giữ thỏ ở gần bụi cỏ
            if (distanceFromBush > PrefabData.Instance.maxDistanceFromBush)
            {
                Vector2 directionToBush = (rabbitBush.transform.position - transform.position).normalized;
                transform.position = (Vector2)rabbitBush.transform.position - directionToBush * 5f;
            }
        }

        public void takeDame()
        {
        }
    }
}