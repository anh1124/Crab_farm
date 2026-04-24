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

        [Header("Storm Invasion - Frontline")]
        [Tooltip("Có phải portal tuyến đầu không (do Manager set, index 0 sau sort)")]
        [SerializeField] private bool isFrontline = false;
        
        // [DEPRECATED] Logic mới dùng status.activeDelay thay cho các field này
        // [SerializeField] private int frontlineStartDay = -1;

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
                // Storm Invasion: Đăng ký vào danh sách quản lý mới
                EnemyPortalManager.Instance.RegisterPortal(this);

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
            
            // Tính toán thời gian di chuyển dựa trên khoảng cách từ TownCenter
            CalculateMoveDurationBasedOnDistance();
            
            Debug.Log($"[EnemyPortal] Cổng vừa xuất hiện tại {transform.position}! SpawnDay={status.spawnDay}. " +
                      $"Delay trong Status.activeDelay: {status.activeDelay} ngày (quản lý bởi Manager khi thành tuyến đầu).");

            // Storm Invasion: Manager sẽ xử lý delay cho portal tuyến đầu (index 0)
            // Không cần đăng ký CheckActivation cá nhân nữa
            Debug.Log($"[EnemyPortalBehavior] Portal tại {transform.position} được spawn. Manager sẽ xử lý delay cho tuyến đầu.");

            // Play dormant loop sound (demand system sẽ handle range)
            _audioEmitter?.PlaySound("Portal_Normal", new Script.Audio.PlaySoundConfig(loop: true));
        }

        /// <summary>
        /// [DEPRECATED - Storm Invasion] Logic này giờ trong TickDelay().
        /// Phương thức này giữ lại để tương thích nếu cần.
        /// </summary>
        private void CheckActivation()
        {
            // Storm Invasion: Không còn dùng - delay được quản lý trong status.activeDelay
            // Manager gọi TickDelay() để trừ delay
        }

        /// <summary>
        /// Được gọi từ EnemyPortalManager để đặt trạng thái tuyến đầu.
        /// Manager sẽ xử lý delay, chỉ cần set flag ở đây.
        /// </summary>
        public void SetFrontline(bool isFront)
        {
            isFrontline = isFront;
            Debug.Log($"[EnemyPortalBehavior] SetFrontline: portal tại {transform.position} isFrontline={isFrontline}");
        }

        /// <summary>
        /// Kiểm tra có phải tuyến đầu không.
        /// </summary>
        public bool IsFrontline() => isFrontline;

        /// <summary>
        /// Lấy status để kiểm tra activeDelay (cho Manager log debug).
        /// </summary>
        public EnemyPortalStatus GetStatus()
        {
            if (status == null) status = GetComponent<EnemyPortalStatus>();
            return status;
        }

        /// <summary>
        /// Reset status.activeDelay về giá trị mặc định.
        /// Được gọi từ Manager khi portal vừa lên index 0 (tuyến đầu).
        /// </summary>
        public void ResetToDefaultDelay()
        {
            // Lấy giá trị mặc định từ prefab (activeDelay ban đầu)
            // Giả sử activeDelay ban đầu là giá trị mong muốn
            if (status != null)
            {
                // Reset về 1 (giá trị mặc định cho portal tuyến đầu)
                status.activeDelay = 1;
                Debug.Log($"[EnemyPortalBehavior] Portal tại {transform.position} reset delay về {status.activeDelay} ngày.");
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
            // Note: Storm Invasion - Manager xử lý tập trung, không cần unsubscribe ở đây
        }

        /// <summary>
        /// Được gọi từ Manager để trừ delay.
        /// Chỉ portal tuyến đầu (index 0) mới được gọi.
        /// Portal tự quyết định active hay không dựa trên status.activeDelay.
        /// </summary>
        public void TickDelay()
        {
            Debug.Log($"[EnemyPortalBehavior] TickDelay được gọi cho portal tại {transform.position}. isFrontline={isFrontline}, isReadyToSpawn={status?.isReadyToSpawn}, activeDelay={status?.activeDelay}");
            
            // Chỉ xử lý nếu là tuyến đầu và chưa active
            if (!isFrontline) 
            {
                Debug.Log($"[EnemyPortalBehavior] Portal không phải tuyến đầu, bỏ qua TickDelay");
                return;
            }
            if (status == null)
            {
                Debug.LogError($"[EnemyPortalBehavior] status NULL! Không thể TickDelay");
                return;
            }
            if (status.isReadyToSpawn) 
            {
                Debug.Log($"[EnemyPortalBehavior] Portal đã ready, bỏ qua TickDelay");
                return;
            }

            // Trừ delay trong status
            status.activeDelay--;
            
            Debug.Log($"[EnemyPortalBehavior] Portal tại {transform.position} trừ delay: còn {status.activeDelay} ngày.");

            // Nếu đủ delay thì active
            if (status.activeDelay <= 0)
            {
                status.activeDelay = 0;
                ForceActivate();
            }
        }

        private Coroutine _moveCoroutine;
        // Lưu trạng thái di chuyển để có thể save/load
        private Vector3 _moveStartPos;
        private Vector3 _moveTargetPos;
        private float _moveElapsedTime;
        private float _moveTotalDuration;
        private bool _isMoving = false;

        /// <summary>
        /// Được gọi từ Manager để yêu cầu di chuyển.
        /// Portal tự quyết định có di chuyển hay không (nếu chưa active thì không di chuyển).
        /// Di chuyển mượt theo tốc độ cố định trong khoảng thời gian moveDuration.
        /// </summary>
        // Thời gian di chuyển cơ bản và tính toán dựa trên khoảng cách
        private float _baseMoveDuration = 5f;
        private float _calculatedMoveDuration = 5f;
        
        /// <summary>
        /// Tính toán thời gian di chuyển dựa trên khoảng cách từ TownCenter.
        /// Càng xa thì di chuyển càng lâu (+2s với mỗi 100f)
        /// </summary>
        private void CalculateMoveDurationBasedOnDistance()
        {
            var townCenter = WorldUtility.GetTownCenter();
            if (townCenter != null)
            {
                float distance = Mathf.Abs(transform.position.x - townCenter.transform.position.x);
                // Công thức: base + 2s cho mỗi 100f khoảng cách
                _calculatedMoveDuration = _baseMoveDuration + (distance / 100f) * 2f;
                
                Debug.Log($"[EnemyPortalBehavior] Tính toán thời gian di chuyển: Khoảng cách={distance:F1}f, Duration={_calculatedMoveDuration:F1}s");
            }
        }
        
        /// <summary>
        /// Lấy thời gian di chuyển đã tính toán (dùng cho Manager)
        /// </summary>
        public float GetCalculatedMoveDuration() => _calculatedMoveDuration;

        /// <summary>
        /// [NEW] Tính toán và trả về thời gian di chuyển dựa trên tham số từ Manager.
        /// Càng xa TownCenter thì di chuyển càng lâu.
        /// </summary>
        public float CalculateMoveDuration(float baseDuration, float increasePer100Distance)
        {
            var townCenter = WorldUtility.GetTownCenter();
            if (townCenter != null)
            {
                float distance = Mathf.Abs(transform.position.x - townCenter.transform.position.x);
                // Công thức: base + (distance/100) * increasePer100
                float calculatedDuration = baseDuration + (distance / 100f) * increasePer100Distance;
                
                Debug.Log($"[EnemyPortalBehavior] Tính toán duration: Distance={distance:F1}f, Base={baseDuration}s, Calculated={calculatedDuration:F1}s");
                return calculatedDuration;
            }
            return baseDuration;
        }

        public void TryMoveTowards(float targetX, float moveSpeed, float moveDuration)
        {
            // Sử dụng thời gian đã tính toán nếu không có giá trị tùy chỉnh
            if (moveDuration <= 0)
                moveDuration = _calculatedMoveDuration;
                
            // Chỉ di chuyển nếu đã active
            if (!status.isReadyToSpawn)
            {
                Debug.Log($"[EnemyPortalBehavior] Portal tại {transform.position} chưa active, không di chuyển.");
                return;
            }

            // Hủy coroutine cũ nếu đang chạy
            if (_moveCoroutine != null)
            {
                StopCoroutine(_moveCoroutine);
            }

            Vector3 pos = transform.position;
            bool isRight = pos.x > targetX;
            float direction = isRight ? -1f : 1f; // Right portal đi trái (giảm x), left đi phải (tăng x)
            float newX = pos.x + direction * moveSpeed * moveDuration;

            // Giới hạn không cho vượt qua TownCenter
            if (!isRight && newX > targetX) newX = targetX; // Left portal không vượt
            if (isRight && newX < targetX) newX = targetX;  // Right portal không vượt

            // Giới hạn khoảng cách tối thiểu (không cho vào quá gần)
            float minDistance = 15f;
            if (Mathf.Abs(newX - targetX) < minDistance)
            {
                newX = isRight ? targetX + minDistance : targetX - minDistance;
            }

            // Lưu trạng thái để có thể save/load
            _moveStartPos = transform.position;
            _moveTargetPos = new Vector3(newX, pos.y, pos.z);
            _moveTotalDuration = moveDuration;
            _moveElapsedTime = 0f;
            _isMoving = true;

            _moveCoroutine = StartCoroutine(MoveSmoothly());
            Debug.Log($"[EnemyPortalBehavior] Portal bắt đầu di chuyển từ x={pos.x:F1} đến x={newX:F1} trong {moveDuration}s");
        }

        private IEnumerator MoveSmoothly()
        {
            while (_moveElapsedTime < _moveTotalDuration)
            {
                _moveElapsedTime += Time.deltaTime;
                float t = _moveElapsedTime / _moveTotalDuration;
                // Smooth step interpolation
                t = t * t * (3f - 2f * t);
                transform.position = Vector3.Lerp(_moveStartPos, _moveTargetPos, t);
                yield return null;
            }

            transform.position = _moveTargetPos;
            _isMoving = false;
            _moveCoroutine = null;
            
            // Tính toán lại thời gian di chuyển cho lần sau (vì vị trí đã thay đổi)
            CalculateMoveDurationBasedOnDistance();
            
            Debug.Log($"[EnemyPortalBehavior] Portal đã di chuyển xong đến x={_moveTargetPos.x:F1}");
        }

        // --- IDamageable Implementation ---
        public void ReceiveDamage(float damage, GameObject instigator)
        {
            status.TakeDamage(damage);
        }

        // --- Wall Erosion (Bào mòn tường) ---
        private void OnCollisionEnter2D(Collision2D collision)
        {
            // Chỉ bào mòn khi đã active (đang spawn enemy)
            if (!status.isReadyToSpawn) return;
            
            // Kiểm tra va chạm với Wall
            if (collision.gameObject.CompareTag(Script.Utils.GameTags.Wall))
            {
                var wallBehavior = collision.gameObject.GetComponent<Script.Game.Structures.Wall_And_Stump.WallBehavior>();
                if (wallBehavior != null)
                {
                    Debug.Log($"[EnemyPortalBehavior] Portal tại {transform.position} bào mòn tường {collision.gameObject.name}!");
                    // Gây sát thương lớn để phá hủy wall ngay lập tức
                    wallBehavior.ReceiveDamage(9999f, gameObject);
                }
            }
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
            public float maxHp;
            public float x, y, z;
            public List<PendingWaveData> pendingWaves;
            // Storm Invasion: chỉ lưu isFrontline, delay được quản lý bởi status.activeDelay
            public bool isFrontline;
            // Movement state for save/load
            public bool isMoving;
            public float moveStartX, moveStartY, moveStartZ;
            public float moveTargetX, moveTargetY, moveTargetZ;
            public float moveElapsedTime;
            public float moveTotalDuration;
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
                maxHp = status.MaxHp,
                x = transform.position.x,
                y = transform.position.y,
                z = transform.position.z,
                pendingWaves = pending,
                isFrontline = this.isFrontline,
                // Lưu trạng thái di chuyển
                isMoving = _isMoving,
                moveStartX = _moveStartPos.x,
                moveStartY = _moveStartPos.y,
                moveStartZ = _moveStartPos.z,
                moveTargetX = _moveTargetPos.x,
                moveTargetY = _moveTargetPos.y,
                moveTargetZ = _moveTargetPos.z,
                moveElapsedTime = _moveElapsedTime,
                moveTotalDuration = _moveTotalDuration
            };
        }

        public void RestoreData(object data)
        {
            if (data is PortalSaveData saveData)
            {
                status.spawnDay = saveData.spawnDay;
                status.isReadyToSpawn = saveData.isReadyToSpawn;
                // Fallback nếu save data bị lỗi (maxHp <= 0) - dùng BASE_MAX_HP từ Manager
                status.MaxHp = saveData.maxHp > 0 
                    ? saveData.maxHp 
                    : (EnemyPortalManager.Instance?.basePortalMaxHp ?? EnemyPortalManager.BASE_MAX_HP);
                status.CurrentHp = saveData.currentHp;
                transform.position = new Vector3(saveData.x, saveData.y, saveData.z);

                // Restore Storm Invasion data
                this.isFrontline = saveData.isFrontline;

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

                // --- LOGIC PHỤC HỒI DI CHUYỂN ---
                if (saveData.isMoving && saveData.moveTotalDuration > 0)
                {
                    _moveStartPos = new Vector3(saveData.moveStartX, saveData.moveStartY, saveData.moveStartZ);
                    _moveTargetPos = new Vector3(saveData.moveTargetX, saveData.moveTargetY, saveData.moveTargetZ);
                    _moveElapsedTime = saveData.moveElapsedTime;
                    _moveTotalDuration = saveData.moveTotalDuration;
                    _isMoving = true;
                    
                    // Tiếp tục di chuyển từ thời điểm đã lưu
                    _moveCoroutine = StartCoroutine(ResumeMovement());
                    Debug.Log($"[EnemyPortalBehavior] Phục hồi và tiếp tục di chuyển từ x={transform.position.x:F1} đến x={_moveTargetPos.x:F1} (đã trôi qua {_moveElapsedTime:F1}/{_moveTotalDuration:F1}s)");
                }
            }
        }

        private IEnumerator ResumeMovement()
        {
            // Tiếp tục di chuyển từ thời điểm đã lưu
            while (_moveElapsedTime < _moveTotalDuration)
            {
                _moveElapsedTime += Time.deltaTime;
                float t = _moveElapsedTime / _moveTotalDuration;
                // Smooth step interpolation
                t = t * t * (3f - 2f * t);
                transform.position = Vector3.Lerp(_moveStartPos, _moveTargetPos, t);
                yield return null;
            }

            transform.position = _moveTargetPos;
            _isMoving = false;
            _moveCoroutine = null;
            Debug.Log($"[EnemyPortalBehavior] Portal đã di chuyển xong đến x={_moveTargetPos.x:F1}");
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

            // Storm Invasion: Không cần unsubscribe CheckActivation nữa vì Manager xử lý tập trung

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
                
                // [DEBUG] Cập nhật debug context trên EnemyWaveManager
                if (EnemyWaveManager.Instance != null)
                {
                    EnemyWaveManager.Instance.OnEnemySpawned(enemyData.power);
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
