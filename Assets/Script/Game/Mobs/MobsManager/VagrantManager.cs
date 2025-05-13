using System.IO;
using UnityEngine;
using Script.Data_Model;
using Script.Game.Other;

namespace Script.Game.Mobs.MobsManager
{
    [System.Serializable]
    public class VagrantSpawnConfig
    {
        public float spawnDistanceFromWall = 3f;
        public int maxVagrants = 5;
        public float dailySpawnChance = 0.7f;
        public int minDailySpawnCount = 0;
        public int maxDailySpawnCount = 11;
        [Tooltip("Khoảng random vị trí x khi sinh vagrant quanh tường (theo đơn vị world)")]
        public float randomXRange = 2f;
    }

    public class VagrantManager : MonoBehaviour
    {
        public static VagrantManager Instance { get; private set; }

        [Header("Thiết lập sinh vagrant (đọc từ file JSON)")]
        public VagrantSpawnConfig config;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                LoadConfig();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void LoadConfig()
        {
            // Đọc file JSON từ Resources/DataConfig/Vagrant/VagrantSpawnConfig.json
            TextAsset configText = Resources.Load<TextAsset>("DataConfig/Vagrant/VagrantSpawnConfig");
            if (configText != null)
            {
                config = JsonUtility.FromJson<VagrantSpawnConfig>(configText.text);
                Debug.Log("[VagrantManager] Đã load cấu hình sinh vagrant từ JSON");
            }
            else
            {
                Debug.LogWarning("[VagrantManager] Không tìm thấy file cấu hình VagrantSpawnConfig.json, sử dụng giá trị mặc định");
                config = new VagrantSpawnConfig();
            }
        }

        private void Start()
        {
            if (GameTimeManager.Instance != null)
            {
                GameTimeManager.Instance.SubscribeToHourlyEvent(6, HandleDailyVagrantSpawn);
                Debug.LogWarning("Đã đăng ký sự kiện sinh vagrant hàng ngày vào lúc 6 giờ sáng");
            }
            else
            {
                Debug.LogError("Không tìm thấy GameTimeManager!");
            }
        }

        private void OnDestroy()
        {
            if (GameTimeManager.Instance != null)
            {
                GameTimeManager.Instance.UnsubscribeFromHourlyEvent(6, HandleDailyVagrantSpawn);
            }
        }

        /// <summary>
        /// Xử lý việc sinh vagrant hàng ngày vào lúc 6 giờ sáng
        /// </summary>
        private void HandleDailyVagrantSpawn()
        {
            Debug.Log("[VagrantManager] Bắt đầu kiểm tra sinh vagrant hàng ngày");
            
            // Chỉ sinh khi có town center
            if (BuildingManager.instance == null || BuildingManager.instance.CurrentTownCenter == null)
            {
                Debug.Log("[VagrantManager] Không tìm thấy town center, bỏ qua việc sinh vagrant");
                return;
            }
            
            // Kiểm tra xem có nên sinh vagrant dựa trên tỉ lệ
            if (Random.value > config.dailySpawnChance)
            {
                Debug.Log($"[VagrantManager] Không vượt qua kiểm tra ngẫu nhiên (tỉ lệ {config.dailySpawnChance*100}%), bỏ qua việc sinh vagrant");
                return;
            }

            // Xác định số lượng vagrant cần sinh
            int spawnCount = Random.Range(config.minDailySpawnCount, config.maxDailySpawnCount + 1);
            Debug.Log($"[VagrantManager] Số lượng sinh ngẫu nhiên: {spawnCount} vagrant");
        
            // Không sinh nếu sẽ vượt quá số lượng tối đa
            int currentVagrantCount = CountExistingVagrants();
            Debug.Log($"[VagrantManager] Số lượng vagrant hiện tại: {currentVagrantCount}/{config.maxVagrants}");
            
            if (currentVagrantCount >= config.maxVagrants)
            {
                Debug.Log("[VagrantManager] Đã đạt giới hạn vagrant tối đa, bỏ qua việc sinh");
                return;
            }

            // Giới hạn số lượng sinh để không vượt quá tối đa
            int originalSpawnCount = spawnCount;
            spawnCount = Mathf.Min(spawnCount, config.maxVagrants - currentVagrantCount);
            
            if (originalSpawnCount != spawnCount)
            {
                Debug.Log($"[VagrantManager] Điều chỉnh số lượng sinh từ {originalSpawnCount} xuống {spawnCount} để tránh vượt quá giới hạn");
            }
        
            if (spawnCount > 0)
            {
                // 50% cơ hội sinh bên phải, 50% bên trái
                bool spawnOnRight = Random.value > 0.5f;
                Debug.Log($"[VagrantManager] Sinh {spawnCount} vagrant ở phía {(spawnOnRight ? "phải" : "trái")}");
            
                // Sinh vagrant
                SpawnVagrants(spawnCount, spawnOnRight);
                Debug.Log("[VagrantManager] Hoàn thành việc sinh vagrant hàng ngày");
            }
            else
            {
                Debug.Log("[VagrantManager] Không có vagrant nào để sinh sau khi điều chỉnh");
            }
        }

        /// <summary>
        /// Đếm số lượng vagrant hiện có trong cảnh
        /// </summary>
        private int CountExistingVagrants()
        {
            if (SetUpContainer.Instance == null || SetUpContainer.Instance.wanderersContainer == null)
            {
                return 0;
            }

            return SetUpContainer.Instance.GetObjectsFromContainerList(SetUpContainer.Instance.wanderersContainer).Count;
        }

        /// <summary>
        /// Sinh một số lượng vagrant tại vị trí ngẫu nhiên hoặc vị trí cụ thể
        /// </summary>
        public void SpawnVagrants(int count, bool? isRight = null, Vector3? customPosition = null)
        {
            if (PrefabData.Instance == null || PrefabData.Instance.vagrant == null)
            {
                Debug.LogError("Không tìm thấy prefab vagrant!");
                return;
            }

            if (count <= 0)
            {
                return;
            }

            for (int i = 0; i < count; i++)
            {
                Vector3 spawnPosition;
            
                if (customPosition.HasValue)
                {
                    // Sử dụng vị trí được cung cấp nếu có
                    spawnPosition = customPosition.Value;
                }
                else
                {
                    // Sử dụng vị trí tường ngẫu nhiên dựa trên isRight
                    spawnPosition = DetermineSpawnPosition(isRight);
                
                    // Nếu không tìm thấy vị trí hợp lệ, bỏ qua lần sinh này
                    if (spawnPosition == Vector3.zero)
                    {
                        Debug.LogError("vị trí sinh vagrant không hợp lệ hoặc lỗi- bỏ qua");
                        continue;
                    }
                }
            
                // Sinh vagrant
                GameObject vagrantObj = SetUpContainer.Instance.InstanceAndMoveObjectToContainer(
                    PrefabData.Instance.vagrant, 
                    spawnPosition
                );
            
                if (vagrantObj != null)
                {
                    Debug.Log($"Đã sinh vagrant tại vị trí {spawnPosition}");
                }
            }
        }
    
        /// <summary>
        /// Xác định vị trí sinh vagrant dựa trên vị trí của tường
        /// </summary>
        private Vector3 DetermineSpawnPosition(bool? isRight = null)
        {
            // Mặc định ngẫu nhiên một bên nếu không được chỉ định
            bool spawnOnRight = isRight ?? (Random.value > 0.5f);
        
            // Tìm bức tường xa nhất ở phía được chỉ định
            GameObject farthestWall = SetUpContainer.Instance.FindFarthestWall(spawnOnRight);
        
            if (farthestWall == null)
            {
                Debug.LogWarning("Không tìm thấy bức tường nào để xác định vị trí sinh vagrant");
                return Vector3.zero;
            }
        
            Vector3 wallPosition = farthestWall.transform.position;
            Vector3 townCenterPos = BuildingManager.instance.CurrentTownCenter.transform.position;
        
            // Xác định hướng (X dương hoặc âm) dựa trên vị trí tương đối với town center
            float directionMultiplier = spawnOnRight ? 1 : -1;
        
            // Random vị trí x quanh tường trong khoảng randomXRange
            float randomOffset = Random.Range(-config.randomXRange, config.randomXRange);
        
            // Tính toán vị trí sinh bên ngoài tường, có random x
            Vector3 spawnPosition = new Vector3(
                wallPosition.x + (directionMultiplier * config.spawnDistanceFromWall) + randomOffset,
                0, // Giữ trên mặt đất
                wallPosition.z
            );
        
            return spawnPosition;
        }
    }
} 