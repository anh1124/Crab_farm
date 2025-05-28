using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Script.Data_Model.Status
{
    [System.Serializable]
    public class VillageStatus : MonoBehaviour
    {
        // Enum Declaration
        public enum VillageState
        {
            Idle,                   // Wandering around kingdom
            MoveBackToTownCenter,   // Walking back to town center at night
            RunToGetTool,           // Running to get tools (highest priority)
            Fear,                   // Running away from danger
            DropCoinToPlayer        // Drop coin when seeing player
        }

        // Basic Settings
        [SerializeField] private float walkSpeed = 1.0f;
        [SerializeField] private float runSpeed = 3.5f;
        [SerializeField] private float currentSpeed;
        // Coin System
        [SerializeField] private int currentCoins = 0;
        [SerializeField] private int maxCoins = 6;

        // Properties
        public float WalkSpeed => walkSpeed;
        public float RunSpeed => runSpeed;
        public float CurrentSpeed => currentSpeed;
        public int CurrentCoins
        {
            get => currentCoins;
            set => currentCoins = Mathf.Clamp(value, 0, maxCoins);
        }
        public int MaxCoins => maxCoins;

        // Save Data Structure
        [System.Serializable]
        public class VillageData
        {
            public float positionX;
            public float positionY;
            public float scaleX;
            public int currentCoins;
        }

        public VillageData CaptureData(Transform transform)
        {
            return new VillageData
            {
                positionX = transform.position.x,
                positionY = transform.position.y,
                scaleX = transform.localScale.x,
                currentCoins = currentCoins
            };
        }

        public void RestoreData(VillageData data, Transform transform)
        {
            transform.position = new Vector3(data.positionX, data.positionY, 0);
            transform.localScale = new Vector3(data.scaleX, 1, 1);
            currentCoins = data.currentCoins;
        }
    }
}
