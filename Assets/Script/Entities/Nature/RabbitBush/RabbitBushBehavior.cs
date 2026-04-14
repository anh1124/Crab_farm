using System;
using System.Collections.Generic;
using Script.Data_Model.Interfaces;
using Script.Manager.NatureManager;
using Script.Manager.SaveManager;
using UnityEngine;

namespace Script.Game.Other.Nature
{
    public class RabbitBushBehavior : MonoBehaviour, ISaveable
    {
        [Serializable]
        public class RabbitBushSaveData
        {
            public string bushID;
            public float positionX;
            public float positionY;
            public bool isLeftSide;
        }

        [Header("Settings")]
        [SerializeField] private int maxRabbitCapacity = 5;
        [SerializeField] private int nightSpawnMin = 1;
        [SerializeField] private int nightSpawnMax = 3;

        [Header("Debug")]
        [SerializeField] private bool isDebug = false;

        [Header("Read Only")]
        [SerializeField] private string _bushID;

        private readonly List<RabbitBehavior> _rabbits = new List<RabbitBehavior>();
        private bool _isLeftSide;
        private int _rabbitsToSpawnThisNight = 0;
        private bool _isLoadedFromSave = false;

        // ─── Public Read-Only Access ─────────────────────────────────────────────
        // Không trả về List gốc để tránh caller sửa từ bên ngoài.
        public IReadOnlyList<RabbitBehavior> Rabbits => _rabbits.AsReadOnly();
        public int RabbitCount => _rabbits.Count;
        public bool IsAtCapacity => _rabbits.Count >= maxRabbitCapacity;

        private void Awake()
        {
            if (string.IsNullOrEmpty(_bushID))
            {
                _bushID = Guid.NewGuid().ToString();
            }

#if UNITY_EDITOR
            if (isDebug) Debug.Log($"[RabbitBushBehavior] Awake — BushID: {_bushID}");
#endif

            // Đăng ký sớm để Rabbit trong Start() có thể tìm thấy Bush này
            RegisterWithManager();
        }

        private void Start()
        {
#if UNITY_EDITOR
            if (isDebug) Debug.Log($"[RabbitBushBehavior] Start — LoadedFromSave: {_isLoadedFromSave}");
#endif
        }

        private void OnDestroy()
        {
            UnregisterFromManager();
        }

        // ─── Side Management ─────────────────────────────────────────────────────

        public void SetSide(bool isLeft)
        {
            _isLeftSide = isLeft;

#if UNITY_EDITOR
            if (isDebug) Debug.Log($"[RabbitBushBehavior] SetSide — IsLeft: {isLeft}");
#endif
        }

        public void SetLoadedFromSave()
        {
            _isLoadedFromSave = true;
        }

        public string GetBushID() => _bushID;

        // ─── Manager Registration ─────────────────────────────────────────────────

        private void RegisterWithManager()
        {
            if (RabbitBushManager.Instance != null)
            {
                RabbitBushManager.Instance.RegisterBush(this, _isLeftSide);
            }
            else
            {
                Debug.LogError("[RabbitBushBehavior] RabbitBushManager.Instance is null!");
            }
        }

        private void UnregisterFromManager()
        {
#if UNITY_EDITOR
            if (isDebug) Debug.Log($"[RabbitBushBehavior] UnregisterFromManager — BushID: {_bushID}");
#endif
            if (RabbitBushManager.Instance != null)
            {
                RabbitBushManager.Instance.UnregisterBush(this);
            }
        }

        // ─── Rabbit Spawning ──────────────────────────────────────────────────────

        #region Rabbit Spawning

        public void StartNightlyRabbitSpawn()
        {
            // Số Rabbit sẽ spawn tối nay được xác định tại đây
            _rabbitsToSpawnThisNight = UnityEngine.Random.Range(nightSpawnMin, nightSpawnMax);

#if UNITY_EDITOR
            if (isDebug) Debug.Log($"[RabbitBushBehavior] StartNightlyRabbitSpawn — BushID: {_bushID}, RabbitsToSpawn: {_rabbitsToSpawnThisNight}");
#endif

            // Spawn con đầu tiên ngay lập tức
            SpawnRabbit();
            _rabbitsToSpawnThisNight--;
        }

        public void SpawnRabbit()
        {
#if UNITY_EDITOR
            if (isDebug) Debug.Log($"[RabbitBushBehavior] SpawnRabbit — BushID: {_bushID}, Rabbits: {_rabbits.Count}/{maxRabbitCapacity}");
#endif

            if (IsAtCapacity)
            {
#if UNITY_EDITOR
                if (isDebug) Debug.Log("[RabbitBushBehavior] Max rabbit capacity reached.");
#endif
                return;
            }

            Vector2 spawnPosition = new Vector2(transform.position.x, 0f);
            GameObject rabbitObj = Spawner.Instance?.SpawnRabbit(spawnPosition);

            if (rabbitObj == null)
            {
                Debug.LogError("[RabbitBushBehavior] Failed to spawn rabbit via Spawner!");
                return;
            }

            RabbitBehavior rabbit = rabbitObj.GetComponent<RabbitBehavior>();
            if (rabbit == null)
            {
                Debug.LogError("[RabbitBushBehavior] Spawned rabbit doesn't have RabbitBehavior!");
                Destroy(rabbitObj);
                return;
            }

            // Inject trực tiếp — Rabbit không cần tự tìm Bush qua Manager
            rabbit.SetBushID(_bushID);
            RegisterRabbit(rabbit);

#if UNITY_EDITOR
            if (isDebug) Debug.Log($"[RabbitBushBehavior] Spawned & registered rabbit. BushID: {_bushID}");
#endif
        }

        public void RabbitDied(RabbitBehavior rabbit)
        {
            if (rabbit == null) return;

            // Dùng bản sao tạm (ToList không cần thiết vì chỉ Remove, không iterate)
            if (_rabbits.Contains(rabbit))
            {
                _rabbits.Remove(rabbit);

#if UNITY_EDITOR
                if (isDebug) Debug.Log($"[RabbitBushBehavior] Rabbit died. Remaining: {_rabbits.Count}");
#endif
            }
        }

        public void RegisterRabbit(RabbitBehavior rabbit)
        {
            if (rabbit == null || _rabbits.Contains(rabbit)) return;

            _rabbits.Add(rabbit);
            rabbit.SetBush(this);

#if UNITY_EDITOR
            if (isDebug) Debug.Log($"[RabbitBushBehavior] Rabbit registered. Total: {_rabbits.Count}");
#endif
        }

        /// <summary>
        /// Alias dùng khi load từ save — tránh nhầm lẫn ý nghĩa với RegisterRabbit thông thường.
        /// </summary>
        public void RabbitLoaded(RabbitBehavior rabbit) => RegisterRabbit(rabbit);

        public void DestroyBush()
        {
#if UNITY_EDITOR
            if (isDebug) Debug.Log($"[RabbitBushBehavior] DestroyBush — BushID: {_bushID}, Rabbits: {_rabbits.Count}");
#endif
            // Sao chép danh sách ra trước khi xóa để tránh lỗi "Collection was modified"
            var snapshot = new List<RabbitBehavior>(_rabbits);
            _rabbits.Clear();

            foreach (var rabbit in snapshot)
            {
                if (rabbit != null) rabbit.Die();
            }

            Destroy(gameObject);
        }

        #endregion

        // ─── Debug Context ────────────────────────────────────────────────────────

        #region Debug Context

#if UNITY_EDITOR
        /// <summary>Context function for testing rabbit spawning (Editor only)</summary>
        [ContextMenu("Debug/Spawn 1 Rabbit")]
        public void DebugContext_Spawn1Rabbit()
        {
            if (isDebug) Debug.Log($"[RabbitBushBehavior] DebugContext_Spawn1Rabbit — BushID: {_bushID}");
            SpawnRabbit();
        }

        /// <summary>Context function for testing 3 rabbit spawning (Editor only)</summary>
        [ContextMenu("Debug/Spawn 3 Rabbits")]
        public void DebugContext_Spawn3Rabbits()
        {
            if (isDebug) Debug.Log($"[RabbitBushBehavior] DebugContext_Spawn3Rabbits — BushID: {_bushID}");
            for (int i = 0; i < 3; i++) SpawnRabbit();
        }
#endif

        #endregion

        // ─── Save/Load ────────────────────────────────────────────────────────────

        #region Save/Load

        public string GetPrefabID()
        {
#if UNITY_EDITOR
            if (isDebug) Debug.Log("[RabbitBushBehavior] GetPrefabID — rabbit_bush");
#endif
            return "rabbit_bush";
        }

        public object CaptureData()
        {
#if UNITY_EDITOR
            if (isDebug) Debug.Log($"[RabbitBushBehavior] CaptureData — BushID: {_bushID}");
#endif
            return new RabbitBushSaveData
            {
                bushID = _bushID,
                positionX = transform.position.x,
                positionY = transform.position.y,
                isLeftSide = _isLeftSide
            };
        }

        public void RestoreData(object data)
        {
            if (data is RabbitBushSaveData saveData)
            {
                _bushID = saveData.bushID;
                transform.position = new Vector3(saveData.positionX, saveData.positionY, 0f);
                _isLeftSide = saveData.isLeftSide;
                _isLoadedFromSave = true;

#if UNITY_EDITOR
                if (isDebug) Debug.Log($"[RabbitBushBehavior] RestoreData — BushID: {_bushID}, Position: {transform.position}");
#endif
            }
        }

        #endregion
    }
}
