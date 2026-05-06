using UnityEngine;
using Script.Manager.WorldManager;
using Script.Data_Model;
using System.Collections.Generic;
using Script.Entities.Structures.SummondPortal;
using Script.Game.Enemy;

namespace Script.Manager.GamePlayManager
{
    public partial class EnemyPortalManager : MonoBehaviour
    {
        public static EnemyPortalManager Instance { get; private set; }

        // Constant cho base HP - dùng làm fallback và reference
        public const float BASE_MAX_HP = 100f;

        private IEnemyPool _enemyPool;
        private IEnemyDataProvider _enemyDataProvider;
        private IGameTimeProvider _timeProvider;
        private bool _isSubscribedToTime = false;

        [Header("Time Manager Settings")]
        [Tooltip("Tự động đăng ký với TimeManager. Đặt false cho tutorial scene.")]
        public bool autoRegisterTimeManager = true;

        public void Setup(IEnemyPool pool, IEnemyDataProvider dataProvider, IGameTimeProvider timeProvider)
        {
            Debug.Log("[EnemyPortalManager] Setup() được gọi");
            _enemyPool = pool;
            _enemyDataProvider = dataProvider;
            _timeProvider = timeProvider;

            // Kiểm tra autoRegisterTimeManager hoặc Tutorial
            if (!autoRegisterTimeManager || Utility.IsTutorialScene())
            {
                Debug.Log("[EnemyPortalManager] Tutorial scene or autoRegisterTimeManager=false detected. Skipping TimeManager registration.");
                return;
            }

            // Đăng ký ngay khi Setup() được gọi để tránh race condition với Start()
            Debug.Log($"[EnemyPortalManager] Setup: timeProvider={(_timeProvider != null)}, _isSubscribedToTime={_isSubscribedToTime}");
            
            if (_timeProvider != null && !_isSubscribedToTime)
            {
                int currentDay = _timeProvider.GetElapsedDays();
                int currentHour = _timeProvider.GameHour;
                
                Debug.Log("[EnemyPortalManager] Bắt đầu đăng ký hourly events...");
                
                _timeProvider.SubscribeToHourlyEvent(6, SpawnAvailablePortal);
                Debug.Log("[EnemyPortalManager] Đã đăng ký SpawnAvailablePortal(6)");
                
                _timeProvider.SubscribeToHourlyEvent(6, ProcessFrontlineDelays);
                Debug.Log("[EnemyPortalManager] Đã đăng ký ProcessFrontlineDelays(6)");
                
                _timeProvider.SubscribeToHourlyEvent(19, MovePortalsTowardsTownCenter);
                Debug.Log("[EnemyPortalManager] Đã đăng ký MovePortalsTowardsTownCenter(19)");
                
                _isSubscribedToTime = true;
                
                Debug.Log($"[EnemyPortalManager] === SETUP OK | Day {currentDay} Hour {currentHour}:00 ===");
                Debug.Log($"[EnemyPortalManager] Tất cả hourly events đã đăng ký thành công!");
            }
            else if (_timeProvider == null)
            {
                Debug.LogError("[EnemyPortalManager] KHÔNG THỂ ĐĂNG KÝ! timeProvider là null!");
            }
            else
            {
                Debug.LogWarning($"[EnemyPortalManager] Bỏ qua đăng ký vì _isSubscribedToTime={_isSubscribedToTime}");
            }
        }

        // Storm Invasion: Danh sách quản lý tất cả portal theo bên
        [Header("Storm Invasion - Portal Lists")]
        [SerializeField] private List<EnemyPortalBehavior> leftPortals = new List<EnemyPortalBehavior>();
        [SerializeField] private List<EnemyPortalBehavior> rightPortals = new List<EnemyPortalBehavior>();

        // Legacy properties - trả về portal tuyến đầu (index 0) hoặc null
        public GameObject portalL => leftPortals.Count > 0 ? leftPortals[0].gameObject : null;
        public GameObject portalR => rightPortals.Count > 0 ? rightPortals[0].gameObject : null;

        // Settings for spawning
        [Header("Spawn Settings")]
        public float minSpawnDistance = 30f;
        public float maxSpawnDistance = 50f;

        [Header("Storm Invasion - Movement")]
        [Tooltip("Tốc độ di chuyển của portal về phía TownCenter mỗi đêm")]
        public float portalMoveSpeed = 3f;
        [Tooltip("Thời gian di chuyển cơ bản (giây)")]
        public float basePortalMoveDuration = 3f;
        [Tooltip("Thời gian tăng thêm mỗi 100f khoảng cách (giây)")]
        public float durationIncreasePer100Distance = 2f;
        [Tooltip("Khoảng cách spawn portal phía sau (so với portal gần nhất)")]
        public float backPortalSpawnOffset = 30f;
        [Tooltip("Số portal tối đa mỗi bên")]
        public int maxPortalsPerSide = 3;

        [Header("Portal HP Settings")]
        [Tooltip("Máu tối đa cơ bản của portal khi spawn")]
        public float basePortalMaxHp = 100f;
        [Tooltip("Lượng máu tăng thêm mỗi ngày")]
        public float hpIncreasePerDay = 20f;

        [Header("Rescue Settings")]
        [Tooltip("Khoảng cách đẩy player vào trong vương quốc khi bị kẹt ngoài cổng")]
        public float rescuePlayerOffset = 5f;

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
            // Kiểm tra autoRegisterTimeManager
            if (!autoRegisterTimeManager)
            {
                Debug.Log("[EnemyPortalManager] autoRegisterTimeManager = false, không đăng ký TimeManager trong Start()");
                return;
            }

            // Nếu Setup() chưa được gọi (ví dụ: scene test đơn lẻ), fallback tự tìm
            if (!_isSubscribedToTime)
            {
                var tp = FindObjectOfType<GameTimeManager>();
                if (tp != null)
                {
                    _timeProvider = tp;
                    _timeProvider.SubscribeToHourlyEvent(6, SpawnAvailablePortal);
                    _timeProvider.SubscribeToHourlyEvent(6, ProcessFrontlineDelays);
                    _timeProvider.SubscribeToHourlyEvent(19, MovePortalsTowardsTownCenter);
                    _isSubscribedToTime = true;
                    Debug.LogWarning("[EnemyPortalManager] Start() fallback: auto-found timeProvider và đăng ký đủ 3 events (6h spawn, 6h delay, 19h move).");
                }
                else
                {
                    Debug.LogWarning("[EnemyPortalManager] Start(): Không tìm thấy GameTimeManager. Portal sẽ không tự sinh!");
                }
            }
        }

        public void InjectDependenciesIntoPortal(EnemyPortalBehavior portal)
        {
            if (portal != null)
            {
                portal.Setup(_enemyPool, _enemyDataProvider, _timeProvider);
                Debug.Log($"[EnemyPortalManager] Đã tiêm lại dependencies cho portal tại {portal.transform.position} (Load từ save).");
            }
        }

        private void OnDestroy()
        {
            if (_timeProvider != null && _isSubscribedToTime)
            {
                _timeProvider.UnsubscribeFromHourlyEvent(6, SpawnAvailablePortal);
                _timeProvider.UnsubscribeFromHourlyEvent(6, ProcessFrontlineDelays);
                _timeProvider.UnsubscribeFromHourlyEvent(19, MovePortalsTowardsTownCenter);
            }
        }
    }
}
