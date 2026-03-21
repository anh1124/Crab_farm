using UnityEngine;
using UnityEngine.Serialization;

namespace Script.Data_Model.Status
{
    [System.Serializable]
    public class TownCenterStatus : MonoBehaviour
    {
        
        public int tier;
        public int upgradeCost;
        public bool readyToUpgrade;
        public int buildProgress;
        public int buildProgressMax;

        [Header("Health")]
        public float maxHp = 500f;
        [SerializeField] private float _currentHp = 500f;

        public event System.Action<float, float> OnHpChanged;

        public float currentHp
        {
            get => _currentHp;
            set
            {
                _currentHp = Mathf.Clamp(value, 0, maxHp);
                OnHpChanged?.Invoke(_currentHp, maxHp);
            }
        }

        [ContextMenu("Trừ 100 HP")]
        public void Deduct100HP()
        {
            currentHp -= 100f;
            Debug.Log($"[TownCenterStatus] Đã trừ 100 HP. HP hiện tại: {currentHp}/{maxHp}");
        }
    }
}


