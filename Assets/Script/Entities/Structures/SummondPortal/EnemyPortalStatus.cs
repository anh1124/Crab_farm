using System;
using UnityEngine;
using Script.Game.Enemy.Interfaces;
using Script.Data_Model.Interfaces;

namespace Script.Entities.Structures.SummondPortal
{
    public class EnemyPortalStatus : MonoBehaviour, IHealth, ISaveable
    {
        [Header("Portal States")]
        public bool isReadyToSpawn = false;

        [Tooltip("Số ngày cần để cổng được kích hoạt (mặc định = 1)")]
        public int activeDelay = 1;
        
        [Tooltip("Ngày mà cổng được sinh ra")]
        public int spawnDay;

        [Header("Health")]
        [SerializeField] private int maxHp = 1000;
        [SerializeField] private int currentHp;

        public int CurrentHp 
        { 
            get => currentHp; 
            set => currentHp = Mathf.Clamp(value, 0, MaxHp); 
        }

        public int MaxHp 
        { 
            get => maxHp; 
            set => maxHp = Mathf.Max(1, value); 
        }

        private void Awake()
        {
            CurrentHp = MaxHp;
        }

        #region IHealth Implementation

        public void TakeDamage(int amount)
        {
            if (CurrentHp <= 0) return;

            CurrentHp -= amount;
            Debug.Log($"[EnemyPortalStatus] {gameObject.name} took {amount} damage. HP: {CurrentHp}/{MaxHp}");

            if (CurrentHp <= 0)
            {
                Die();
            }
        }

        public void Heal(int amount)
        {
            CurrentHp += amount;
        }

        public void Die()
        {
            Destroy(gameObject);
        }

        #endregion

        #region ISaveable Implementation

        [Serializable]
        public struct PortalSaveData
        {
            public int spawnDay;
            public bool isReadyToSpawn;
            public int currentHp;
            public float x, y, z;
        }

        public string GetPrefabID()
        {
            return "EnemyPortalPrefab_ID"; // Modify if you have a specific ID system
        }

        public object CaptureData()
        {
            return new PortalSaveData
            {
                spawnDay = this.spawnDay,
                isReadyToSpawn = this.isReadyToSpawn,
                currentHp = this.CurrentHp,
                x = transform.position.x,
                y = transform.position.y,
                z = transform.position.z
            };
        }

        public void RestoreData(object data)
        {
            if (data is PortalSaveData saveData)
            {
                this.spawnDay = saveData.spawnDay;
                this.isReadyToSpawn = saveData.isReadyToSpawn;
                this.CurrentHp = saveData.currentHp;
                transform.position = new Vector3(saveData.x, saveData.y, saveData.z);
            }
        }

        #endregion
    }
}
