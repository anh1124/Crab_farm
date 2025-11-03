using System;
using UnityEngine;
using Script.Data_Model.Interfaces;
using Script.Data_Model.Status;
using Script.Game.Item;
using Script.Game.Structures.TownCenter;
using Script.Save_Controller;
using Script.Game.Mobs.MobsManager; 
using Script.Data_Model; 
using Script.Game.Item.Coin; 
using Script.Game.Enemy.Interfaces;
using Script.Interfaces;

namespace Script.Game.Mobs.Subjects
{
    // Renamed from ArcherController to ArcherBehavior to reflect Behavior + Status architecture
    public class ArcherBehavior : MonoBehaviour, ISaveable, IHealth, ICoinShield
    {
        private ArcherStatus status;
        public bool isGuard = false;
        public bool isLeftFlank = false;

        [Header("Coin & Transformation")] [SerializeField]
        private int currentCoins = 0;

        [SerializeField] private int maxCoins = 5;
        [SerializeField] private int maxHp = 50;
        [SerializeField] private int currentHp = 50;

        // IDamageable implementation
        public bool HasCoinShield => currentCoins > 0;

        public bool ConsumeCoinShield() // được DamageHandler gọi
        {
            if (!HasCoinShield) return false;
            CurrentCoin -= 1;
            if (CurrentCoin < 0) CurrentCoin = 0;
            DropCoins(1);
            if (CurrentCoin == 0)
            {
                Utility.TransformToVillager(gameObject, 0);
            }

            return true;
        }

        public bool HasHp => true;

        public int CurrentCoin
        {
            get => currentCoins;
            set => currentCoins = value;
        }

        public int MaxCoin
        {
            get => maxCoins;
            set => maxCoins = value;
        }

        public int CurrentHp
        {
            get => currentHp;
            set => currentHp = value;
        }

        public int MaxHp
        {
            get => maxHp;
            set => maxHp = value;
        }

        public void TakeDamage(int amount)
        {
            CurrentHp = Mathf.Max(0, CurrentHp - amount);
        }

        public void Die()
        {
            throw new NotImplementedException();
        }

        public void Heal(int amount)
        {
            CurrentHp = Mathf.Min(MaxHp, CurrentHp + amount);
        }

        public bool IsDead => CurrentHp <= 0;

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
            GameObject nearestEnemy = FindNearestHuntableEnemy();
            if (nearestEnemy != null)
            {
                status.CurrentState = ArcherStatus.ArcherState.AttackingFromWall;
                status.CurrentSpeed = status.RunSpeed;
                Utility.MoveTowardsTarget(gameObject, nearestEnemy.transform.position, status.CurrentSpeed);
            }
            else
            {
                status.CurrentSpeed = 0f;
            }
        }

        private void HandlePatrolState()
        {
        }

        private void HandleHoldingWallState()
        {
        }

        private void HandleAttackingFromWallState()
        {
        }

        private void HandleRetreatingToInnerWallState()
        {
        }

        private void HandleReturningToBaseState()
        {
        }

        public string GetPrefabID() => "archer";
        public object CaptureData() => status.CaptureData(transform);

        public void RestoreData(object data)
        {
            var archerData = (ArcherStatus.ArcherData)data;
            status.RestoreData(archerData, transform);
        }

        public void DropCoins(int amount)
        {
            for (int i = 0; i < amount; i++)
            {
                GameObject coinPrefab = PrefabData.Instance.coinPrefab;
                if (coinPrefab != null)
                {
                    GameObject coinInstance = Instantiate(coinPrefab, transform.position, Quaternion.identity);
                    Coin coinScript = coinInstance.GetComponent<Coin>();
                    if (coinScript != null)
                    {
                        Vector2 randomDirection =
                            new Vector2(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(0.5f, 1f))
                                .normalized;
                        float throwForce = 5f;
                        float throwDistance = 2f;
                        coinScript.InitializeThrow(randomDirection, throwForce, throwDistance);
                    }
                }
            }
        }

        private GameObject FindNearestHuntableEnemy()
        {
            GameObject nearestEnemy = null;
            float minDistance = Mathf.Infinity;
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
        // marker interface declaration moved out of this class if needed elsewhere
    }
}

   

