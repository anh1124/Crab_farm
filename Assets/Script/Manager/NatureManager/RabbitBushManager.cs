using System.Collections.Generic;
using System.Linq;
using Script.Data_Model;
using Script.Game.Other.Nature;
using Script.Manager.SaveManager;
using Script.Manager.WorldManager;
using Script.Utils;
using UnityEngine;

namespace Script.Manager.NatureManager
{
    public class RabbitBushManager : MonoBehaviour
    {
        public static RabbitBushManager Instance { get; private set; }

        // ─── Giờ đăng ký sự kiện (có thể chỉnh trong Inspector) ──────────────────
        [Header("Time Settings (Hour of Day 0-23)")]
        [SerializeField] private int hourSpawnBush    = 0;  // Nửa đêm: spawn bush mới
        [SerializeField] private int hourFirstSpawn   = 5;  // 5 giờ sáng: thỏ đầu tiên
        [SerializeField] private int hourSecondSpawn  = 6;  // 6 giờ sáng: thỏ thứ hai
        [SerializeField] private int hourThirdSpawn   = 7;  // 7 giờ sáng: thỏ thứ ba

        // ─── Khoảng cách sinh cây bụi ────────────────────────────────────────────
        [Header("Bush Spawn Settings")]
        [SerializeField] private int maxBushesPerSide = 2;
        [SerializeField] private float wallOffset     = 10f; // Khoảng cách buffer tính từ tường

        [Header("Debug")]
        [SerializeField] private bool isDebug = false;

        // ─── Nội bộ: dùng Dictionary để tránh lặp Left/Right ─────────────────────
        // true = Left, false = Right
        private readonly Dictionary<bool, List<RabbitBushBehavior>> _bushes = new Dictionary<bool, List<RabbitBushBehavior>>
        {
            { true,  new List<RabbitBushBehavior>() },
            { false, new List<RabbitBushBehavior>() }
        };

        private GameTimeManager _gameTimeManager;
        private Script.Game.Mobs.MobsManager.GuardManager _guardManager;
        private bool _isInitialized = false;

        // ─── Lifecycle ────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
#if UNITY_EDITOR
            if (isDebug) Debug.Log("[RabbitBushManager] Start — Calling Initialize");
#endif
            Initialize();
        }

        private void OnDestroy()
        {
#if UNITY_EDITOR
            if (isDebug) Debug.Log("[RabbitBushManager] OnDestroy — Unsubscribing from events");
#endif
            UnsubscribeTimeEvents();
        }

        // ─── Initialization ───────────────────────────────────────────────────────

        public void Initialize()
        {
            if (_isInitialized) return;

            _gameTimeManager = FindObjectOfType<GameTimeManager>();
            _guardManager    = FindObjectOfType<Script.Game.Mobs.MobsManager.GuardManager>();

            if (_gameTimeManager != null)
            {
                _gameTimeManager.SubscribeToHourlyEvent(hourSpawnBush,   OnMidnight);
                _gameTimeManager.SubscribeToHourlyEvent(hourFirstSpawn,  OnFirstSpawnHour);
                _gameTimeManager.SubscribeToHourlyEvent(hourSecondSpawn, OnSecondSpawnHour);
                _gameTimeManager.SubscribeToHourlyEvent(hourThirdSpawn,  OnThirdSpawnHour);

#if UNITY_EDITOR
                if (isDebug) Debug.Log("[RabbitBushManager] Registered hourly events with GameTimeManager.");
#endif
            }
            else
            {
                Debug.LogError("[RabbitBushManager] GameTimeManager not found!");
            }

            _isInitialized = true;
            Debug.Log("[RabbitBushManager] Initialized.");
        }

        private void UnsubscribeTimeEvents()
        {
            if (_gameTimeManager == null) return;
            _gameTimeManager.UnsubscribeFromHourlyEvent(hourSpawnBush,   OnMidnight);
            _gameTimeManager.UnsubscribeFromHourlyEvent(hourFirstSpawn,  OnFirstSpawnHour);
            _gameTimeManager.UnsubscribeFromHourlyEvent(hourSecondSpawn, OnSecondSpawnHour);
            _gameTimeManager.UnsubscribeFromHourlyEvent(hourThirdSpawn,  OnThirdSpawnHour);
        }

        // ─── Time Events ──────────────────────────────────────────────────────────

        #region Time Events

        private void OnMidnight()
        {
#if UNITY_EDITOR
            if (isDebug) Debug.Log($"[RabbitBushManager] OnMidnight — L:{GetCount(true)}/{maxBushesPerSide}, R:{GetCount(false)}/{maxBushesPerSide}");
#endif
            SpawnBushRandom();
        }

        private void OnFirstSpawnHour()
        {
#if UNITY_EDITOR
            if (isDebug) Debug.Log("[RabbitBushManager] OnFirstSpawnHour — StartNightlyRabbitSpawn");
#endif
            ExecuteOnAllBushes(b => b.StartNightlyRabbitSpawn());
        }

        private void OnSecondSpawnHour()
        {
#if UNITY_EDITOR
            if (isDebug) Debug.Log("[RabbitBushManager] OnSecondSpawnHour — SpawnRabbit");
#endif
            ExecuteOnAllBushes(b => b.SpawnRabbit());
        }

        private void OnThirdSpawnHour()
        {
#if UNITY_EDITOR
            if (isDebug) Debug.Log("[RabbitBushManager] OnThirdSpawnHour — SpawnRabbit");
#endif
            ExecuteOnAllBushes(b => b.SpawnRabbit());
        }

        /// <summary>
        /// Thực thi action trên tất cả Bush (cả 2 bên).
        /// Dùng ToList() để tránh lỗi "Collection was modified" trong khi iterate.
        /// </summary>
        private void ExecuteOnAllBushes(System.Action<RabbitBushBehavior> action)
        {
            foreach (var bush in _bushes[true].ToList())
            {
                if (bush != null) action(bush);
            }
            foreach (var bush in _bushes[false].ToList())
            {
                if (bush != null) action(bush);
            }
        }

        #endregion

        // ─── Bush Management ──────────────────────────────────────────────────────

        #region Bush Management

        public void RegisterBush(RabbitBushBehavior bush, bool isLeft)
        {
            if (bush == null) return;

            var list = _bushes[isLeft];
            if (!list.Contains(bush))
            {
                list.Add(bush);
#if UNITY_EDITOR
                if (isDebug) Debug.Log($"[RabbitBushManager] RegisterBush — ID:{bush.GetBushID()}, IsLeft:{isLeft}, Count:{list.Count}");
#endif
            }
        }

        public void UnregisterBush(RabbitBushBehavior bush)
        {
            if (bush == null) return;

            _bushes[true].Remove(bush);
            _bushes[false].Remove(bush);

#if UNITY_EDITOR
            if (isDebug) Debug.Log($"[RabbitBushManager] UnregisterBush — ID:{bush.GetBushID()}");
#endif
        }

        public int GetBushCountForSide(bool isLeft) => _bushes[isLeft].Count;

        private int GetCount(bool isLeft) => _bushes[isLeft].Count;

        public RabbitBushBehavior GetBushByID(string bushID)
        {
            if (string.IsNullOrEmpty(bushID)) return null;

            foreach (var kvp in _bushes)
            {
                foreach (var bush in kvp.Value)
                {
                    if (bush != null && bush.GetBushID() == bushID)
                    {
#if UNITY_EDITOR
                        if (isDebug) Debug.Log($"[RabbitBushManager] GetBushByID:{bushID} — Found (isLeft:{kvp.Key})");
#endif
                        return bush;
                    }
                }
            }

#if UNITY_EDITOR
            if (isDebug) Debug.Log($"[RabbitBushManager] GetBushByID:{bushID} — Not found.");
#endif
            return null;
        }

        /// <summary>
        /// Spawn cây bụi ở bên ngẫu nhiên có chỗ trống — ưu tiên bên được chọn, fallback sang bên còn lại.
        /// </summary>
        private void SpawnBushRandom()
        {
            bool leftNeedsBush  = GetCount(true)  < maxBushesPerSide;
            bool rightNeedsBush = GetCount(false) < maxBushesPerSide;

            if (!leftNeedsBush && !rightNeedsBush)
            {
#if UNITY_EDITOR
                if (isDebug) Debug.Log("[RabbitBushManager] SpawnBushRandom — Both sides full.");
#endif
                return;
            }

            bool preferLeft = UnityEngine.Random.value < 0.5f;

            if (preferLeft)
            {
                SpawnBushForSide(leftNeedsBush ? true : false);
            }
            else
            {
                SpawnBushForSide(rightNeedsBush ? false : true);
            }
        }

        /// <summary>
        /// Spawn một cây bụi ở phía được chọn (Left = true, Right = false).
        /// Dùng WorldUtility.GetHuntingBounds để lấy phạm vi, tránh tự tính toán.
        /// </summary>
        private void SpawnBushForSide(bool isLeft)
        {
            if (_guardManager == null)
            {
                Debug.LogError("[RabbitBushManager] GuardManager not found! Cannot spawn bush.");
                return;
            }

            var bounds = WorldUtility.GetHuntingBounds(_guardManager, isLeft);

            // Điều chỉnh phạm vi để tránh spawn sát tường
            float minSpawnX = isLeft ? bounds.end            : bounds.start + wallOffset;
            float maxSpawnX = isLeft ? bounds.start - wallOffset : bounds.end;

            if (minSpawnX >= maxSpawnX)
            {
                Debug.LogError($"[RabbitBushManager] Invalid spawn range: min={minSpawnX}, max={maxSpawnX}. Check hunting bounds or wallOffset.");
                return;
            }

            float posX = UnityEngine.Random.Range(minSpawnX, maxSpawnX);
            Vector3 spawnPosition = new Vector3(posX, 0f, 0f);

            // Xác minh vị trí hợp lệ qua Utility — không tự viết lại logic kiểm tra
            bool isInHuntArena = WorldUtility.IsInHuntingZone(spawnPosition.x, _guardManager, isLeft);
            if (!isInHuntArena)
            {
                Debug.LogError($"[RabbitBushManager] Position {spawnPosition} is NOT in hunt arena. Aborting spawn.");
                return;
            }

            var townCenter = WorldUtility.GetTownCenter();
            if (townCenter != null)
            {
                bool actualIsLeft = spawnPosition.x < townCenter.transform.position.x;
                if (actualIsLeft != isLeft)
                {
                    Debug.LogError($"[RabbitBushManager] Side mismatch: target={isLeft}, actual={actualIsLeft}. Aborting.");
                    return;
                }
            }

            if (Spawner.Instance == null)
            {
                Debug.LogError("[RabbitBushManager] Spawner.Instance is null!");
                return;
            }

            GameObject bushObj = Spawner.Instance.SpawnRabbitBush(spawnPosition);
            if (bushObj == null)
            {
                Debug.LogError("[RabbitBushManager] Failed to spawn bush via Spawner!");
                return;
            }

            RabbitBushBehavior bush = bushObj.GetComponent<RabbitBushBehavior>();
            if (bush == null)
            {
                Debug.LogError("[RabbitBushManager] Spawned object has no RabbitBushBehavior!");
                Destroy(bushObj);
                return;
            }

            bush.SetSide(isLeft);

#if UNITY_EDITOR
            if (isDebug) Debug.Log($"[RabbitBushManager] Spawned {(isLeft ? "LEFT" : "RIGHT")} bush at {spawnPosition}, ID: {bush.GetBushID()}");
#endif
        }

        #endregion

        // ─── Kingdom Zone Validation ──────────────────────────────────────────────

        #region Kingdom Zone Validation

        public void NotifyKingdomZoneChanged()
        {
#if UNITY_EDITOR
            if (isDebug) Debug.Log("[RabbitBushManager] NotifyKingdomZoneChanged — Validating all bush positions");
#endif
            ValidateAllBushPositions();
        }

        private void ValidateAllBushPositions()
        {
            ValidateSide(true);
            ValidateSide(false);
        }

        /// <summary>
        /// Xóa Bush ở một phía nếu vị trí của nó đã rơi vào Safe Zone sau khi vương quốc mở rộng.
        /// Tách thành method riêng để tránh lặp code giữa Left và Right.
        /// </summary>
        private void ValidateSide(bool isLeft)
        {
            var list = _bushes[isLeft];
            for (int i = list.Count - 1; i >= 0; i--)
            {
                var bush = list[i];
                if (bush == null)
                {
                    list.RemoveAt(i);
                    continue;
                }

                if (WorldUtility.IsInKingdomSafeZone(bush.transform.position.x))
                {
#if UNITY_EDITOR
                    if (isDebug) Debug.Log($"[RabbitBushManager] Bush at {bush.transform.position} entered safe zone — destroying.");
#endif
                    bush.DestroyBush();
                    list.RemoveAt(i);
                }
            }
        }

        #endregion

        // ─── Debug Context ────────────────────────────────────────────────────────

        #region Debug Context

#if UNITY_EDITOR
        [ContextMenu("Debug/Spawn Bush (Left)")]
        public void DebugContext_SpawnBushLeft()
        {
            Debug.Log("[RabbitBushManager] DebugContext_SpawnBushLeft");
            SpawnBushForSide(true);
        }

        [ContextMenu("Debug/Spawn Bush (Right)")]
        public void DebugContext_SpawnBushRight()
        {
            Debug.Log("[RabbitBushManager] DebugContext_SpawnBushRight");
            SpawnBushForSide(false);
        }

        [ContextMenu("Debug/Spawn Bush (Random)")]
        public void DebugContext_SpawnBushRandom()
        {
            Debug.Log("[RabbitBushManager] DebugContext_SpawnBushRandom");
            SpawnBushRandom();
        }
#endif

        #endregion
    }
}
