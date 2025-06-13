using System;
using UnityEngine;
using Script.Data_Model.Interfaces;
using Script.Data_Model.Status;
using Script.Game.Item;
using Script.Game.Structures.TownCenter;
using Script.Game.Mobs.Subjects;
using Script.Save_Controller;
using Script.Game.Mobs.MobsManager; // Added for Spawner
using Script.Data_Model; // Added for PrefabData
using Script.Game.Item.Coin; // Added for CoinItem
using Script.Game.Enemy.Interfaces;

namespace Script.Game.Mobs.Subjects
{
    public class ArcherController : MonoBehaviour, ISaveable, IDamageable
    {
        private ArcherStatus status;
        public bool isGuard = false;
        public bool isLeftFlank = false;
        // [SerializeField] private float maxHuntingDistance = 10f; // Removed as SetRandomHuntingTarget is removed
        // private Vector3 currentHuntingTarget; // Removed as SetRandomHuntingTarget is removed

        [Header("Coin & Transformation")]
        [SerializeField] private int currentCoins = 0;
        [SerializeField] private int maxCoins = 5;
        [SerializeField] private int maxHp = 50;
        [SerializeField] private int currentHp = 50;

        // IDamageable implementation
        public bool HasCoinShield => currentCoins > 0;
        public bool HasHp => true;
        public int CurrentCoin { get => currentCoins; set => currentCoins = value; }
        public int MaxCoin { get => maxCoins; set => maxCoins = value; }
        public int CurrentHp { get => currentHp; set => currentHp = value; }
        public int MaxHp { get => maxHp; set => maxHp = value; }

        private void Awake()
        {
            gameObject.tag = "Archer";
            status = GetComponent<ArcherStatus>();
            if (status == null)
            {
                Debug.LogWarning($"{gameObject.name} does not have ArcherStatus, adding one.");
                status = gameObject.AddComponent<ArcherStatus>();
            }
        }

        private void Update()
        {
            switch (status.CurrentState)
            {
                case ArcherStatus.ArcherState.Idle:
                    HandleIdleState();
                    break;
                case ArcherStatus.ArcherState.Hunting:
                    HandleHuntingState();
                    break;
                case ArcherStatus.ArcherState.Patrol:
                    HandlePatrolState();
                    break;
                case ArcherStatus.ArcherState.HoldingWall:
                    HandleHoldingWallState();
                    break;
                case ArcherStatus.ArcherState.AttackingFromWall:
                    HandleAttackingFromWallState();
                    break;
                case ArcherStatus.ArcherState.RetreatingToInnerWall:
                    HandleRetreatingToInnerWallState();
                    break;
                case ArcherStatus.ArcherState.ReturningToBase:
                    HandleReturningToBaseState();
                    break;
            }
        }

        private void HandleIdleState()
        {
            if (!isGuard)
            {
                status.CurrentState = ArcherStatus.ArcherState.Hunting;
            }
        }

        private void HandleHuntingState()
        {
            // Logic cho trạng thái Hunting
            GameObject nearestEnemy = FindNearestHuntableEnemy();
            if (nearestEnemy != null)
            {
                // Nếu tìm thấy kẻ địch, di chuyển đến và chuyển trạng thái tấn công
                status.CurrentState = ArcherStatus.ArcherState.AttackingFromWall; // Tạm thời dùng AttackingFromWall cho logic tấn công
                status.CurrentSpeed = status.RunSpeed; // Chạy nhanh đến kẻ địch
                Utility.MoveTowardsTarget(gameObject, nearestEnemy.transform.position, status.CurrentSpeed);
            }
            else
            {
                // Nếu không có kẻ địch, Archer dừng lại
                status.CurrentSpeed = 0f; 
            }
        }

        private void HandlePatrolState()
        {
            // Logic cho trạng thái Patrol (tạm thời để trống)
        }

        private void HandleHoldingWallState()
        {
            // Logic cho trạng thái HoldingWall (tạm thời để trống)
        }

        private void HandleAttackingFromWallState()
        {
            // Logic cho trạng thái AttackingFromWall (tạm thời để trống)
        }

        private void HandleRetreatingToInnerWallState()
        {
            // Logic cho trạng thái RetreatingToInnerWall (tạm thời để trống)
        }

        private void HandleReturningToBaseState()
        {
            // Logic cho trạng thái ReturningToBase (tạm thời để trống)
        }

        public string GetPrefabID()
        {
            return "archer";
        }

        public object CaptureData()
        {
            return status.CaptureData(transform);
        }

        public void RestoreData(object data)
        {
            var archerData = (ArcherStatus.ArcherData)data;
            status.RestoreData(archerData, transform);
        }

        public void DropCoins(int amount)
        {
            Debug.Log($"{gameObject.name} dropped {amount} coins.");
            for (int i = 0; i < amount; i++)
            {
                GameObject coinPrefab = PrefabData.Instance.coinPrefab;
                if (coinPrefab != null)
                {
                    GameObject coinInstance = Instantiate(coinPrefab, transform.position, Quaternion.identity);
                    Coin coinScript = coinInstance.GetComponent<Coin>();
                    if (coinScript != null)
                    {
                        // Sử dụng InitializeThrow để mô phỏng việc ném đồng xu
                        Vector2 randomDirection = new Vector2(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(0.5f, 1f)).normalized;
                        float throwForce = 5f; // Lực ném mặc định
                        float throwDistance = 2f; // Khoảng cách ném mặc định
                        coinScript.InitializeThrow(randomDirection, throwForce, throwDistance);
                    }
                }
            }
        }
        
        private GameObject FindNearestHuntableEnemy()
        {
            GameObject nearestEnemy = null;
            float minDistance = Mathf.Infinity;

            // Tìm kiếm tất cả các đối tượng có tag "Enemy"
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
                foreach (GameObject enemy in enemies)
                {
                    float distance = Vector3.Distance(transform.position, enemy.transform.position);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        nearestEnemy = enemy;
                }
            }
            return nearestEnemy;
        }

        // Removed FindNearestCoin() as per user instruction
        // Removed CheckAndCollectCoins() as per user instruction
        // Removed TransformToVillager() as per user instruction, moving to Utility.cs

        #region IDamageable Implementation
        public bool TakeDamage(int damage, GameObject whoDoDame)
        {
            if (HasCoinShield && CurrentCoin > 0)
            {
                CurrentCoin -= 1;
                if (CurrentCoin < 0) CurrentCoin = 0;
                DropCoins(1);
                if (CurrentCoin == 0)
                {
                    Utility.TransformToVillager(gameObject, 0);
                    return false;
                }
                return true;
            }
            if (HasHp)
            {
                CurrentHp -= damage;
                if (CurrentHp <= 0)
                {
                    Die(whoDoDame);
                    return false;
                }
                return true;
            }
            return true;
        }
        public void Die(GameObject whoDoDame)
        {
            Debug.Log($"{gameObject.name} đã chết bởi {whoDoDame?.name ?? "unknown"}");
            Destroy(gameObject);
        }
        #endregion
    }
} 