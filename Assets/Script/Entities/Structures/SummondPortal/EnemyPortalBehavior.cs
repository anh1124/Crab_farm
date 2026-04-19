using UnityEngine;
using Script.Manager.WorldManager;
using Script.Utils;
using Script.Manager;
using Script.Manager.GamePlayManager;
using System.Collections;
using System.Collections.Generic;
using Script.Game.Enemy;
using Script.Interfaces;
using Script.Game.Enemy.Core;
using Script.Data_Model.Interfaces;
using System;

namespace Script.Entities.Structures.SummondPortal
{
    [RequireComponent(typeof(EnemyPortalStatus))]
    [RequireComponent(typeof(Script.Utils.DamageFlashEffect))]
    [RequireComponent(typeof(Script.Audio.AudioEmitter))]
    public class EnemyPortalBehavior : MonoBehaviour, IDamageable, ISaveable
    {
        private EnemyPortalStatus status;
        private Script.Audio.AudioEmitter _audioEmitter;

        public bool IsReadyToSpawn 
        { 
            get 
            { 
                if (status == null) status = GetComponent<EnemyPortalStatus>();
                return status != null && status.isReadyToSpawn; 
            } 
        }

        [Header("Spawn Settings")]
        [Tooltip("Thời gian giãn cách giữa mỗi lần sinh quái")]
        public float spawnInterval = 2f;
        
        // Dữ liệu mảng quái vật nhận từ Manager khi đêm xuống
        private List<EnemyWaveData> enemyWaveData;
        private IEnemyPool _enemyPool;
        private IEnemyDataProvider _enemyDataProvider;
        private IGameTimeProvider _timeProvider;

        private PortalVisualAnimator visualAnimator;
        
        [Header("Camera Shake Settings")]
        [SerializeField] private int activationShakeCount = 3;
        [SerializeField] private float activationInterval = 0.15f;
        [SerializeField] private float activationTiltAngle = 5f;
        
        [Space]
        [SerializeField] private int spawnShakeCount = 2;
        [SerializeField] private float spawnIntervalRate = 0.2f; // Tên biến khác với spawnInterval (logic spawn)
        [SerializeField] private float spawnTiltAngle = 3f;

        private void Awake()
        {
            status = GetComponent<EnemyPortalStatus>();
            visualAnimator = GetComponentInChildren<PortalVisualAnimator>();
            _audioEmitter = GetComponent<Script.Audio.AudioEmitter>();
        }

        private void Start()
        {
            // Đăng ký vào EnemyPortalManager trong Start() vì đây là nơi đảm bảo rằng
            // tất cả Awake() đã chạy xong (kể cả EnemyPortalManager.Awake() set Instance).
            if (EnemyPortalManager.Instance != null)
            {
                GameObject townCenter = Utility.GetTownCenter();
                float centerPosX = townCenter != null ? townCenter.transform.position.x : 0f;

                bool isRight = transform.position.x > centerPosX;
                if (isRight)
                {
                    EnemyPortalManager.Instance.portalR = gameObject;
                    Debug.Log($"[EnemyPortalBehavior] Start: Tự đăng ký vào portalR tại {transform.position}");
                }
                else
                {
                    EnemyPortalManager.Instance.portalL = gameObject;
                    Debug.Log($"[EnemyPortalBehavior] Start: Tự đăng ký vào portalL tại {transform.position}");
                }

                // Nhớ lại nhận pool và dependencies từ Manager nếu chưa có (load từ Save)
                if (_enemyPool == null)
                {
                    EnemyPortalManager.Instance.InjectDependenciesIntoPortal(this);
                }
            }
            else
            {
                Debug.LogWarning("[EnemyPortalBehavior] EnemyPortalManager.Instance là NULL khi Start()! Portal sẽ không được đăng ký.");
            }

            // Khi load từ Save, nếu isReadyToSpawn đã = true → bật đồ họa sáng luôn
            if (status != null && status.isReadyToSpawn)
            {
                if (visualAnimator) visualAnimator.SetActive();
            }

            // Lắng nghe sự kiện chết từ Status
            status.OnDeath += OnDeathHandler;
        }

        /// <summary>
        /// Được gọi bởi EnemyPortalManager lúc Instaniate GameObject.
        /// </summary>
        public void Setup(IEnemyPool pool, IEnemyDataProvider dataProvider, IGameTimeProvider timeProvider)
        {
            _enemyPool = pool;
            _enemyDataProvider = dataProvider;
            _timeProvider = timeProvider;

            // Set FPS visual Animator thành Dormant (nghỉ)
            if (visualAnimator)
            {
                visualAnimator.SetDormant();
            }

            // Gọi shake màn hình khi portal lần đầu xuất hiện (dạng dormant)
            CameraShake.Instance?.RequestShake(transform.position, activationShakeCount, activationInterval, activationTiltAngle);

            // Ghi lại ngày sinh ra cổng
            status.spawnDay = _timeProvider != null ? _timeProvider.GetElapsedDays() : 0;
            Debug.Log($"[EnemyPortal] Cổng vừa xuất hiện tại {transform.position}! Sẽ kích hoạt vào ngày {(status.spawnDay + status.activeDelay)}.");

            // Đăng ký sự kiện kiểm tra mỗi giờ để xem cổng đã sẵn sàng chưa (check buổi sáng)
            if (_timeProvider != null)
            {
                _timeProvider.SubscribeToHourlyEvent(6, CheckActivation);
            }

            // Play dormant loop sound (demand system sẽ handle range)
            _audioEmitter?.PlaySound("Portal_Normal", new Script.Audio.PlaySoundConfig(loop: true));
        }

        private void CheckActivation()
        {
            if (status.isReadyToSpawn) return;

            int currentDay = _timeProvider != null ? _timeProvider.GetElapsedDays() : 0;
            // Đã sống đủ ngưỡng delay
            if (currentDay >= status.spawnDay + status.activeDelay)
            {
                ForceActivate();
            }
        }

        public void ForceActivate()
        {
            if (status == null) status = GetComponent<EnemyPortalStatus>();
            if (status.isReadyToSpawn) return; // Đã active rồi thì thôi

            status.isReadyToSpawn = true;
            Debug.Log($"[EnemyPortalBehavior] Cổng tại {transform.position} đã kích hoạt và sẵn sàng sinh quái!");

            // Stop dormant sound and register demand for activate sound
            // Demand system sẽ handle range - chỉ play khi player trong range
            _audioEmitter?.StopSound("Portal_Normal");
            _audioEmitter?.PlaySound("Portal_Activate", new Script.Audio.PlaySoundConfig(loop: true));

            // Kích hoạt Visual Animator tăng frame rate lên bùng cháy
            if (visualAnimator)
            {
                visualAnimator.SetActive();
            }

            // Shake camera mạnh khi portal chính thức kích hoạt
            CameraShake.Instance?.RequestShake(transform.position, activationShakeCount, activationInterval, activationTiltAngle);

            // Hủy đăng ký vì ngày nào sau đó cũng là Active rồi
            if (_timeProvider != null)
            {
                _timeProvider.UnsubscribeFromHourlyEvent(6, CheckActivation);
            }
        }

        // --- IDamageable Implementation ---
        public void ReceiveDamage(float damage, GameObject instigator)
        {
            status.TakeDamage(damage);
        }

        private void OnDeathHandler()
        {
            // Destruction logic is handled in OnDestroy and via this method
            Destroy(gameObject);
        }

        #region ISaveable Implementation

        [Serializable]
        public struct PortalSaveData
        {
            public int spawnDay;
            public bool isReadyToSpawn;
            public float currentHp;
            public float x, y, z;
        }

        public string GetPrefabID()
        {
            return "EnemyPortalPrefab_ID";
        }

        public object CaptureData()
        {
            return new PortalSaveData
            {
                spawnDay = status.spawnDay,
                isReadyToSpawn = status.isReadyToSpawn,
                currentHp = status.CurrentHp,
                x = transform.position.x,
                y = transform.position.y,
                z = transform.position.z
            };
        }

        public void RestoreData(object data)
        {
            if (data is PortalSaveData saveData)
            {
                status.spawnDay = saveData.spawnDay;
                status.isReadyToSpawn = saveData.isReadyToSpawn;
                status.CurrentHp = saveData.currentHp;
                transform.position = new Vector3(saveData.x, saveData.y, saveData.z);

                // --- LOGIC PHỤC HỒI BEHAVIOR ---
                // Sau khi load xong data, nếu portal đã ready -> kích hoạt visual ngay lập tức
                if (status.isReadyToSpawn && visualAnimator != null)
                {
                    visualAnimator.SetActive();

                    // Đăng ký demand cho Portal_Activate sound (demand system sẽ handle range)
                    _audioEmitter?.PlaySound("Portal_Activate", new Script.Audio.PlaySoundConfig(loop: true));
                }
                else if (!status.isReadyToSpawn && visualAnimator != null)
                {
                    // Nếu portal chưa active, đăng ký demand cho Portal_Normal sound
                    _audioEmitter?.PlaySound("Portal_Normal", new Script.Audio.PlaySoundConfig(loop: true));
                }
            }
        }

        #endregion

        private void OnDestroy()
        {
            // AudioEmitter là component trên cùng GameObject, tự handle cleanup trong OnDestroy của nó
            // Không cần gọi _audioEmitter?.StopAll() ở đây

            // Báo Manager clear biến Left/Right của nó
            if (EnemyPortalManager.Instance)
            {
                EnemyPortalManager.Instance.OnPortalDestroyed(this);
            }

            // Dọn dẹp event nếu nó chết lúc đang chờ
            if (!status.isReadyToSpawn && _timeProvider != null)
            {
                _timeProvider.UnsubscribeFromHourlyEvent(6, CheckActivation);
            }

            // Chỉ log khi đây là phá hủy thật trong game,
            // không log khi hệ thống save đang dọn container để load lại (RemoveAllChildObjects).
            if (!EntitySaveManager.IsLoading)
            {
                Debug.Log($"[EnemyPortalBehavior] Cổng tại {transform.position} đã bị phá hủy.");
            }
        }

        #region Spawning Logic (Hợp nhất từ SummondPortal)

        public void StartSpawning(List<EnemyWaveData> waveData)
        {
            // Lấy danh sách enemy từ tham số truyền vào
            enemyWaveData = waveData;
            
            if (enemyWaveData != null)
            {
                Debug.Log($"[EnemyPortalBehavior - {gameObject.name}] Nhận lệnh xuất quân: {enemyWaveData.Count} loại enemy khác nhau.");
                StartCoroutine(SpawnEnemies());
            }
        }

        private IEnumerator SpawnEnemies()
        {
            // Kiểm tra xem thực tế có con quái nào sẽ đẻ ra không trước khi rung màn hình
            int totalToSpawn = 0;
            if (enemyWaveData != null)
            {
                foreach (var wave in enemyWaveData)
                {
                    if (wave.enemyData.enemyPrefab != null && wave.count > 0)
                        totalToSpawn += wave.count;
                }
            }

            if (totalToSpawn > 0)
            {
                // Shake camera một lần duy nhất trước đợt spawn của portal này
                CameraShake.Instance?.RequestShake(transform.position, spawnShakeCount, spawnIntervalRate, spawnTiltAngle);
            }

            int totalSpawned = 0;
            foreach (var enemyData in enemyWaveData)
            {
                for (int i = 0; i < enemyData.count; i++)
                {
                    // Kiểm tra prefab trước khi spawn để tránh lỗi null ref
                    if (enemyData.enemyData.enemyPrefab == null)
                    {
                       // Debug.LogWarning($"[EnemyPortalBehavior] Bỏ qua spawn {enemyData.enemyData.enemyName} vì prefab bị NULL .");
                        continue;
                    }

                    SpawnEnemy(enemyData.enemyData);
                    totalSpawned++;
                    Debug.Log($"[EnemyPortalBehavior - {gameObject.name}] >> Rớt ra quái thứ {totalSpawned}: {enemyData.enemyData.enemyName}");
                    
                    yield return new WaitForSeconds(spawnInterval);
                }
            }
            
            Debug.Log($"[EnemyPortalBehavior - {gameObject.name}] Hoàn thành wave. Đã spawn tổng cộng {totalSpawned} con quái.");
        }

        private void SpawnEnemy(TotalEnemyList.EnemyDataBase enemyData)
        {
            // Auto-recover pool nếu chưa được inject (ví dụ: portal được load từ Save, chưa qua Setup())
            if (_enemyPool == null)
            {
                _enemyPool = FindObjectOfType<Script.Game.Enemy.EnemyPool>();
                if (_enemyPool == null)
                {
                    Debug.LogError("[EnemyPortalBehavior] _enemyPool is NULL and cannot be found in scene! Cannot spawn.");
                    return;
                }
                Debug.LogWarning("[EnemyPortalBehavior] _enemyPool was null — auto-recovered from scene. Portal was likely loaded from save without Setup().");
            }

            if (enemyData.enemyPrefab == null)
            {
                Debug.LogError($"[EnemyPortalBehavior] enemyPrefab của '{enemyData.enemyName}' bị null! Kiểm tra lại TotalEnemyList Inspector.");
                return;
            }

            GameObject enemy = _enemyPool.GetEnemy(enemyData.enemyName);
            if (enemy != null)
            {
                enemy.transform.position = gameObject.transform.position;
                enemy.transform.rotation = Quaternion.identity;

                EnemyAIController controller = enemy.GetComponent<EnemyAIController>();
                if (controller != null)
                {
                    controller.ResetEnemy(gameObject);
                }
                else
                {
                    Debug.LogWarning($"[EnemyPortalBehavior] {enemyData.enemyName} thiếu Component EnemyAIController!");
                }
            }
            else
            {
                Debug.LogWarning($"[EnemyPortalBehavior] Pool không còn đủ '{enemyData.enemyName}' để spawn!");
            }
        }

        #endregion
    }
}
