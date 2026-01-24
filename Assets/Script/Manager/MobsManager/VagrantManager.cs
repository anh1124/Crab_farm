using System.IO;
using UnityEngine;
using Script.Data_Model;
using Script.Game.Other;
using System.Collections.Generic;
using Script.Data_Model.Status;
using Script.Manager;
using Script.Manager.Building;
using Script.Manager.WorldManager;
using Script.Game.Mobs.Subjects;

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
        
        // Tỉ lệ sinh vagrant hiện tại (mặc định = dailySpawnChance, tăng khi sinh thất bại)
        private float currentSpawnChance;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                LoadConfig();
                currentSpawnChance = config.dailySpawnChance; // Khởi tạo tỉ lệ ban đầu
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
               // Debug.Log("[VagrantManager] Đã load cấu hình sinh vagrant từ JSON");
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
                //Debug.LogWarning("Đã đăng ký sự kiện sinh vagrant hàng ngày vào lúc 6 giờ sáng");
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
            Debug.Log($"[VagrantManager] Bắt đầu kiểm tra sinh vagrant hàng ngày (tỉ lệ hiện tại: {currentSpawnChance*100}%)");
            
            // Chỉ sinh khi có town center
            if (BuildingManager.instance == null || BuildingManager.instance.currentTownCenter == null)
            {
                Debug.Log("[VagrantManager] Không tìm thấy town center, bỏ qua việc sinh vagrant");
                // Không tăng tỉ lệ sinh khi chưa có town center
                return;
            }
            
            // Kiểm tra tier của town center, chỉ sinh vagrant khi tier > 0
            int townCenterLevel = BuildingManager.instance.currentTownCenter.GetComponent<TownCenterStatus>().tier;
            if (townCenterLevel <= 0)
            {
                Debug.Log($"[VagrantManager] Town center tier ({townCenterLevel}) chưa đủ để sinh vagrant (cần tier > 0), bỏ qua");
                // Không tăng tỉ lệ khi town center chưa đạt tier yêu cầu
                return;
            }
            
            // Đếm số vagrant hiện tại trước khi sinh
            int currentVagrantCount = CountExistingVagrants();
            Debug.Log($"[VagrantManager] Số lượng vagrant hiện tại: {currentVagrantCount}/{config.maxVagrants}");
            
            // Không sinh nếu đã đạt số lượng tối đa
            if (currentVagrantCount >= config.maxVagrants)
            {
                Debug.Log("[VagrantManager] Đã đạt giới hạn vagrant tối đa, bỏ qua việc sinh");
                return;
            }
            
            // Kiểm tra xem có nên sinh vagrant dựa trên tỉ lệ hiện tại
            if (Random.value > currentSpawnChance)
            {
                Debug.Log($"[VagrantManager] Không vượt qua kiểm tra ngẫu nhiên (tỉ lệ {currentSpawnChance*100}%), bỏ qua việc sinh vagrant");
                // Tăng tỉ lệ sinh cho lần sau do không sinh được
                IncreaseSpawnChance();
                return;
            }

            // Xác định số lượng vagrant cần sinh dựa trên chính sách giới hạn chặt chẽ hơn
            int spawnCount;
            
            // Giới hạn số lượng sinh tùy theo tier của town center
            int maxAllowedPerDay = Mathf.Min(2, townCenterLevel); // Mặc định tối đa 2, không quá tier town center
            
            // Giới hạn số lượng còn trống
            int remainingSlots = config.maxVagrants - currentVagrantCount;
            
            // Số lượng mặc định
            spawnCount = Mathf.Min(maxAllowedPerDay, remainingSlots);
            
            // Nếu tỉ lệ sinh đạt 100% và qua kiểm tra 50%, có thể sinh thêm 1 nếu còn chỗ trống
            bool bonusVagrant = false;
            if (Mathf.Approximately(currentSpawnChance, 1.0f) && Random.value < 0.5f && spawnCount < remainingSlots)
            {
                spawnCount += 1;
                bonusVagrant = true;
                Debug.Log("[VagrantManager] Tỉ lệ sinh đạt 100% và vượt qua kiểm tra 50% - sinh thêm 1 vagrant");
            }
            
            Debug.Log($"[VagrantManager] Số lượng sinh: {spawnCount} vagrant (max/ngày: {maxAllowedPerDay}, còn trống: {remainingSlots}, bonus: {bonusVagrant})");
            
            if (spawnCount <= 0)
            {
                Debug.Log("[VagrantManager] Không thể sinh vagrant do không còn chỗ trống");
                return;
            }
            
            // 50% cơ hội sinh bên phải, 50% bên trái
            bool spawnOnRight = Random.value > 0.5f;
            Debug.Log($"[VagrantManager] Sinh {spawnCount} vagrant ở phía {(spawnOnRight ? "phải" : "trái")}");
            
            // Sinh vagrant
            SpawnVagrants(spawnCount, spawnOnRight);
        }
        
        /// <summary>
        /// Tăng tỉ lệ sinh vagrant khi không sinh được
        /// </summary>
        private void IncreaseSpawnChance()
        {
            currentSpawnChance += 0.3f;
            if (currentSpawnChance > 1.0f)
            {
                currentSpawnChance = 1.0f;
            }
            Debug.Log($"[VagrantManager] Tăng tỉ lệ sinh vagrant cho lần sau lên {currentSpawnChance*100}%");
        }
        
        /// <summary>
        /// Reset tỉ lệ sinh vagrant về mặc định sau khi sinh thành công
        /// </summary>
        private void ResetSpawnChance()
        {
            currentSpawnChance = config.dailySpawnChance;
            Debug.Log($"[VagrantManager] Reset tỉ lệ sinh vagrant về mặc định: {currentSpawnChance*100}%");
        }

        /// <summary>
        /// Đếm số lượng vagrant hiện có trong cảnh
        /// </summary>
        private int CountExistingVagrants()
        {
            if (EntitySaveManager.Instance == null || EntitySaveManager.Instance.wanderersContainer == null)
            {
                return 0;
            }

            // Lấy tất cả đối tượng từ container
            List<GameObject> objects = EntitySaveManager.Instance.GetObjectsFromContainerList(EntitySaveManager.Instance.wanderersContainer);
            
            // Đếm chỉ các đối tượng có tag "Vagrant"
            int vagrantCount = 0;
            foreach (GameObject obj in objects)
            {
                if (obj != null && obj.CompareTag("Vagrant"))
                {
                    vagrantCount++;
                }
            }
            
            Debug.Log($"[VagrantManager] Đếm số lượng vagrant: {vagrantCount} (tổng số đối tượng trong container: {objects.Count})");
            
            return vagrantCount;
        }

        /// <summary>
        /// Sinh một số lượng vagrant tại vị trí ngẫu nhiên hoặc vị trí cụ thể
        /// </summary>
        public void SpawnVagrants(int count, bool? isRight = null, Vector3? customPosition = null)
        {
            if (PrefabData.Instance == null || PrefabData.Instance.denizen == null)
            {
                Debug.LogError("Không tìm thấy denizen trong PrefabData!");
                return;
            }

            if (count <= 0)
            {
                return;
            }
            
            // Đếm số vagrant thực sự được sinh
            int successfulSpawns = 0;

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
                        Debug.LogError("Vị trí sinh vagrant không hợp lệ do không tìm thấy tường - bỏ qua");
                        continue;
                    }
                }

                // Force Y position to 0 to ensure spawning on ground
                spawnPosition.y = 0;
            
                // Sinh denizen và gán role Vagrant
                GameObject vagrantObj = EntitySaveManager.Instance.InstanceAndMoveObjectToContainer(
                    PrefabData.Instance.denizen,
                    spawnPosition
                );
                if (vagrantObj != null)
                {
                    // Đặt tag trước để re-parent ngay lập tức vào container đúng
                    vagrantObj.tag = "Vagrant";
                    EntitySaveManager.Instance?.MoveObjectToContainer(vagrantObj);

                    var controller = vagrantObj.GetComponent<DenizenController>();
                    if (controller == null)
                    {
                        controller = vagrantObj.AddComponent<DenizenController>();
                    }
                    // ChangeRole về Vagrant (no-op nếu mặc định), đảm bảo ApplyRole chạy và đồng bộ trạng thái
                    controller.ChangeRole(NPCRole.Vagrant);
                }
            
                if (vagrantObj != null)
                {
                    Debug.Log($"Đã sinh vagrant tại vị trí {spawnPosition}");
                    successfulSpawns++;
                }
            }
            
            // Kiểm tra lại số vagrant thực sự sinh ra
            if (successfulSpawns <= 0 && count > 0)
            {
                Debug.LogWarning("[VagrantManager] Không thể sinh vagrant nào! Giữ nguyên tỉ lệ sinh cho lần sau.");
                // Không reset tỉ lệ vì thực tế không sinh được vagrant nào
                HandleDailyVagrantSpawn_NoSuccessfulSpawns();
            }
            else if (successfulSpawns > 0)
            {
                Debug.Log($"[VagrantManager] Đã sinh thành công {successfulSpawns}/{count} vagrant");
                // Đã sinh thành công ít nhất 1 vagrant, reset tỉ lệ
                ResetSpawnChance();
            }
        }
        
        // Phương thức được gọi khi không sinh được vagrant nào mặc dù đã cố gắng
        private void HandleDailyVagrantSpawn_NoSuccessfulSpawns()
        {
            // Giữ nguyên tỉ lệ sinh cho lần sau
            Debug.LogWarning($"[VagrantManager] Giữ nguyên tỉ lệ sinh vagrant ở mức: {currentSpawnChance*100}% cho lần sau");
        }
    
        /// <summary>
        /// Xác định vị trí sinh vagrant dựa trên vị trí của tường
        /// </summary>
        private Vector3 DetermineSpawnPosition(bool? isRight = null)
        {
            // Mặc định ngẫu nhiên một bên nếu không được chỉ định
            bool spawnOnRight = isRight ?? (Random.value > 0.5f);
        
            // Tìm bức tường xa nhất ở phía được chỉ định
            GameObject farthestWall = EntitySaveManager.Instance.FindFarthestWall(spawnOnRight);
        
            if (farthestWall == null)
            {
                Debug.LogWarning("Không tìm thấy bức tường nào để xác định vị trí sinh vagrant");
                return Vector3.zero;
            }
        
            Vector3 wallPosition = farthestWall.transform.position;
            Vector3 townCenterPos = BuildingManager.instance.currentTownCenter.transform.position;
        
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
            
            Debug.Log($"[VagrantManager] Xác định vị trí sinh vagrant: {spawnPosition}, gần tường tại: {wallPosition}");
        
            return spawnPosition;
        }
    }
} 