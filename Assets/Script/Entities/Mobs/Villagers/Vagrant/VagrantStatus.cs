using System;
using UnityEngine;

namespace Script.Data_Model.Status
{
    [System.Serializable]
    public class VagrantStatus : MonoBehaviour
    {
        // Enum Declaration
        public enum VagrantState
        {
            Fear,   // Sit down when seeing enemy
            Idle,   // stand
            GetCoin // Run quickly to coin position
        }

        // Movement Settings
        [SerializeField] private float idleSpeed = 1.0f;
        [SerializeField] private float runSpeed = 3.5f;
        [SerializeField] private float maxIdleWalkDistance = 3.0f;

        public float IdleSpeed
        {
            get => idleSpeed;
            set => idleSpeed = value;
        }

        public float RunSpeed
        {
            get => runSpeed;
            set => runSpeed = value;
        }

        public float MaxIdleWalkDistance
        {
            get => maxIdleWalkDistance;
            set => maxIdleWalkDistance = value;
        }

        // Detection Settings
        [SerializeField] private float roadToTarget = 0f;
        [SerializeField] private Vector2 targetPosition;
        [SerializeField] private GameObject coin;

        public float RoadToTarget
        {
            get => roadToTarget;
            set => roadToTarget = value;
        }

        public Vector2 TargetPosition
        {
            get => targetPosition;
            set => targetPosition = value;
        }

        public GameObject Coin
        {
            get => coin;
            set => coin = value;
        }

        // State and Tracking
        [SerializeField] private VagrantState currentState;
        [SerializeField] private Vector3 startPosition;
        [SerializeField] private Vector3 currentIdleTarget;
        [SerializeField] private bool isWalking;

        public VagrantState CurrentState
        {
            get => currentState;
            set => currentState = value;
        }

        public Vector3 StartPosition
        {
            get => startPosition;
            set => startPosition = value;
        }

        public Vector3 CurrentIdleTarget
        {
            get => currentIdleTarget;
            set => currentIdleTarget = value;
        }

        public bool IsWalking
        {
            get => isWalking;
            set
            {
                if (isWalking != value)
                {
                    isWalking = value;
                   
                }
            }
        }

        // Save Data Structure
        [System.Serializable]
        public class VagrantData
        {
            public float positionX;
            public float positionY;
            public float scaleX;
            public int stateValue;
            public float startPositionX;
        }

        private void Awake()
        {
            // Initialize default values
            currentState = VagrantState.Idle;
            roadToTarget = 0f;
            isWalking = false;
            //ebug.Log("[VagrantStatus] Initialized in Awake");
        }

        public VagrantData CaptureData(Transform transform)
        {
            return new VagrantData
            {
                positionX = transform.position.x,
                positionY = transform.position.y,
                scaleX = transform.localScale.x,
                stateValue = (int)currentState,
                startPositionX = startPosition.x
            };
        }

        public void RestoreData(VagrantData data, Transform transform)
        {
            transform.position = new Vector3(data.positionX, data.positionY, 0);
            transform.localScale = new Vector3(data.scaleX, 1, 1);
            currentState = (VagrantState)data.stateValue;
            startPosition = new Vector3(data.startPositionX, data.positionY, 0);
        }
    }
} 