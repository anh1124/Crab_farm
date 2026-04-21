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
        public struct PendingWaveData
        {
            public string enemyName;
            public int count;
        }

        [Serializable]
        public struct PortalSaveData
        {
            public int spawnDay;
            public bool isReadyToSpawn;
            public float currentHp;
            public float x, y, z;
            public List<PendingWaveData> pendingWaves;
        }

        public string GetPrefabID()
        {
            return "EnemyPortalPrefab_ID";
        }

        public object CaptureData()
        {
            List<PendingWaveData> pending = new List<PendingWaveData>();
            if (enemyWaveData != null)
            {
                foreach (var wave in enemyWaveData)
                {
                    if (wave.count > 0 && wave.enemyData != null)
                    {
                        pending.Add(new PendingWaveData
                        {
                            enemyName = wave.enemyData.enemyName,
                            count = wave.count
                        });
                    }
                }
            }

            return new PortalSaveData
            {
                spawnDay = status.spawnDay,
                isReadyToSpawn = status.isReadyToSpawn,
                currentHp = status.CurrentHp,
                x = transform.position.x,
                y = transform.position.y,
                z = transform.position.z,
                pendingWaves = pending
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

                // --- LOGIC PHỤC HỒI PENDING WAVES ---
                if (saveData.pendingWaves != null && saveData.pendingWaves.Count > 0)
                {
                    var totalList = UnityEngine.Object.FindObjectOfType<TotalEnemyList>();
                    if (totalList != null)
                    {
                        totalList.InitializeEnemyData(); // Đảm bảo list data đã sẵn sàng
                        enemyWaveData = new List<EnemyWaveData>();
                        
                        foreach (var pending in saveData.pendingWaves)
                        {
                            var info = totalList.GetEnemyDataByName(pending.enemyName);
                            if (info != null)
                            {
                                enemyWaveData.Add(new EnemyWaveData
                                {
                                    enemyData = info,
                                    count = pending.count
                                });
                            }
                        }

                        if (enemyWaveData.Count > 0 && status.isReadyToSpawn)
                        {
                            Debug.Log($"[EnemyPortalBehavior] Phục hồi và tiếp tục đẻ {enemyWaveData.Count} loại quái dở dang từ file tải.");
                            StartCoroutine(SpawnEnemies());
                        }
                    }
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
            if (waveData == null) return;
            
            // Tạo bản copy độc lập để lưu trữ và trừ lùi trong lúc spawn 
            // Tránh vạ lây nếu nhiều portal dùng chung reference List (bị gọi chung từ Manager)
            enemyWaveData = new List<EnemyWaveData>();
            foreach(var w in waveData)
            {
                enemyWaveData.Add(new EnemyWaveData {
                    enemyData = w.enemyData,
                    count = w.count
                });
            }
            
            if (enemyWaveData.Count > 0)
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
            
            // Dùng vòng lặp while để có thể chủ động trừ count trong object thực tế
            while (enemyWaveData != null && enemyWaveData.Count > 0)
            {
                var wave = enemyWaveData[0];
                if (wave.count > 0)
                {
                    if (wave.enemyData != null && wave.enemyData.enemyPrefab != null)
                    {
                        SpawnEnemy(wave.enemyData);
                        totalSpawned++;
                        Debug.Log($"[EnemyPortalBehavior - {gameObject.name}] >> Rớt ra quái thứ {totalSpawned}: {wave.enemyData.enemyName}");
                    }
                    
                    wave.count--; // Trừ dần để CaptureData lấy được số lượng thực tế còn lại
                    yield return new WaitForSeconds(spawnInterval);
                }
                else
                {
                    // Loại quái này đã đẻ xong, vứt khỏi queue
                    enemyWaveData.RemoveAt(0);
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
