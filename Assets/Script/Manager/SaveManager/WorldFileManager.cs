using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Script.GUI_Controller.StartMenu;
using Script.Data_Model;
using Newtonsoft.Json;
using Script.Manager;
using Script.Manager.WorldManager;
using Script.Manager.GamePlayManager;

namespace Script.Save_Controller
{
    public class WorldFileManager : MonoBehaviour
    {
        // dùng trong gameplay scene
        public static WorldFileManager Instance { get; private set; }
        private TechState _techState;
        private EntitySaveManager _entitySaveManager;
        private LoadFromSave _loadFromSave;

        public void Setup(TechState tech, EntitySaveManager entitySave, LoadFromSave loadSave)
        {
            _techState = tech;
            _entitySaveManager = entitySave;
            _loadFromSave = loadSave;
        }

        public void Init()
        {
            // Path logic is already in Awake for legacy support, 
            // but we can move or re-verify it here if needed.
            Debug.Log("✅ WorldFileManager initialized via Init()");
        }
        public string saveRootPath;

        [Tooltip("thông tin thế giới hiện tạitại")]
        [SerializeField] public WorldInfo worldInfo;

        // Đường dẫn đến thế giới đang hoạt động
        private string activeWorldPath;
        
        // Event delegate khi worldInfo thay đổi
        public delegate void WorldInfoChangedHandler(WorldInfo info);
        public event WorldInfoChangedHandler OnWorldInfoChanged;
        
        //singeleton-get save path
        private void Awake()     
        {
            if (Instance == null)
            {
                Instance = this;
               
            }
            else
            {
                Destroy(gameObject); // Nếu đã tồn tại ingstance khác, hủy bản sao này
            }                     
            // Đường dẫn đến thư mục saves
            string rootPath = Application.persistentDataPath;
            saveRootPath = Path.Combine(rootPath, "saves");
        }
        


        /// <summary>
        /// Lưu đối tượng WorldInfo hiện tại vào file worldInfo.json
        /// </summary>
        /// <param name="worldName">Tên thế giới cần lưu</param>
        /// <returns>True nếu lưu thành công, False nếu thất bại</returns>
        public bool SaveCurrentWorldInfo(string worldName = null)
        {
            try
            {
                // Kiểm tra worldInfo tồn tại
                if (worldInfo == null)
                {
                    Debug.LogError("Không có WorldInfo để lưu");
                    return false;
                }

                // Sử dụng tên từ tham số nếu có, ngược lại sử dụng tên từ worldInfo
                string nameToSave = string.IsNullOrEmpty(worldName) ? worldInfo.Name : worldName;
                
                // Nếu vẫn không có tên hợp lệ
                if (string.IsNullOrEmpty(nameToSave))
                {
                    Debug.LogError("Tên thế giới không hợp lệ");
                    return false;
                }

                // Lấy đường dẫn thư mục
                string worldFolder = GetWorldFolderPath(nameToSave);
                
                // Nếu thư mục không tồn tại, tạo thư mục mới
                if (worldFolder == null)
                {
                    worldFolder = Path.Combine(saveRootPath, nameToSave);
                    Directory.CreateDirectory(worldFolder);
                    // Debug.Log($"Đã tạo thư mục mới cho thế giới: {nameToSave}");
                    
                    // Tạo thư mục map
                    Directory.CreateDirectory(Path.Combine(worldFolder, "map"));
                }

                // Cập nhật lại tên trong worldInfo nếu cần
                worldInfo.Name = nameToSave;
                
                // Cập nhật thời gian chơi cuối cùng (Iso 8601 string)
                worldInfo.LastPlayedDate = System.DateTime.Now.ToString("O");

                // Lưu vào file
                string worldInfoPath = Path.Combine(worldFolder, "worldInfo.json");
                string json = JsonConvert.SerializeObject(worldInfo, Formatting.Indented);
                File.WriteAllText(worldInfoPath, json);
                
                // Debug.Log($"Đã lưu WorldInfo vào: {worldInfoPath}");
                
                // Thông báo thay đổi
                OnWorldInfoChanged?.Invoke(worldInfo);
                
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Lỗi khi lưu WorldInfo: {ex.Message}");
                return false;
            }
        }

        //hàm này là hàm save chính
        public void SaveCurrentMap()
        {
            if (_entitySaveManager != null)
            {
                _entitySaveManager.SaveAllContainer();
            }
            else if (EntitySaveManager.Instance != null)
            {
                EntitySaveManager.Instance.SaveAllContainer();
            }

            var cm = FindObjectOfType<ChunkManager>();
            if (cm != null)
            {
                 cm.SaveMapToJson(GetActiveWorldPath());
            }
            
            SaveGameTime();
            
            if (worldInfo != null && _techState != null)
            {
                SaveTechStatus(
                    _techState.TechFireArrow,
                    _techState.TechPenetrating,
                    _techState.TechForge,
                    _techState.TechAdhesive
                );
            }
        }
        public void SaveGameTime()
        {
            // Kiểm tra xem worldInfo có tồn tại không
            if (worldInfo == null)
            {
                Debug.LogWarning("Không có dữ liệu WorldInfo để lưu thời gian.");
                return;
            }

            // Cập nhật thời gian hiện tại từ GameTimeManager vào worldInfo
            var gtm = FindObjectOfType<GameTimeManager>();
            if (gtm != null)
            {
                worldInfo.GameHour = gtm.GameHour;
                worldInfo.GameMinute = gtm.GameMinute;
                worldInfo.GameDay = gtm.GameDay;
                worldInfo.GameMonth = gtm.GameMonth;
                worldInfo.GameYear = gtm.GameYear;
            }
            
            // Lưu extraPower từ EnemyWaveManager (Storm Invasion)
            if (EnemyWaveManager.Instance != null)
            {
                worldInfo.ExtraPower = EnemyWaveManager.Instance.GetExtraPower();
            }

            // Lấy đường dẫn tới file worldInfo.json
            string worldFolder = GetWorldFolderPath(worldInfo.Name);
            if (worldFolder == null)
            {
                Debug.LogError("Không tìm thấy folder của world để lưu thời gian.");
                return;
            }
            string worldInfoPath = Path.Combine(worldFolder, "worldInfo.json");

            // Cập nhật last played date mỗi khi save
            worldInfo.LastPlayedDate = System.DateTime.Now.ToString("O");

            // Serialize và ghi file JSON
            string json = JsonConvert.SerializeObject(worldInfo, Formatting.Indented);
            File.WriteAllText(worldInfoPath, json);
            // Debug.Log("Game time đã được lưu vào " + worldInfoPath);
            
            // Thông báo rằng worldInfo đã thay đổi
            OnWorldInfoChanged?.Invoke(worldInfo);
        }
        public void SaveTechStatus(bool techFireArrow, bool techPenetrating, bool techForge, bool techAdhesive)
        {
            if (worldInfo == null) return;

            // Sync to worldInfo for file saving
            worldInfo.TechFireArrow = techFireArrow;
            worldInfo.TechPenetrating = techPenetrating;
            worldInfo.TechForge = techForge;
            worldInfo.TechAdhesive = techAdhesive;

            // Also ensure techState is synced if updated from elsewhere
            if (_techState != null)
            {
                _techState.TechFireArrow = techFireArrow;
                _techState.TechPenetrating = techPenetrating;
                _techState.TechForge = techForge;
                _techState.TechAdhesive = techAdhesive;
            }

            string worldFolder = GetWorldFolderPath(worldInfo.Name);
            if (worldFolder == null) return;

            string worldInfoPath = Path.Combine(worldFolder, "worldInfo.json");
            worldInfo.LastPlayedDate = System.DateTime.Now.ToString("O");

            string json = JsonConvert.SerializeObject(worldInfo, Formatting.Indented);
            File.WriteAllText(worldInfoPath, json);
            
            OnWorldInfoChanged?.Invoke(worldInfo);
        }
        
        /// <summary>
        /// Hàm này đọc thông tin từ file worldInfo.json và gán vào biến worldInfo,
        /// giúp truy cập dữ liệu thế giới trực tiếp thông qua biến này.
        /// </summary>
        /// <param name="worldName">Tên thế giới cần load</param>
        /// <returns>Đối tượng WorldInfo đã load, hoặc null nếu không thể load</returns>
        public WorldInfo LoadWorldInfo(string worldName)
        {
            string worldFolder = GetWorldFolderPath(worldName);
            if (worldFolder == null)
            {
                Debug.LogError($"Không tìm thấy thư mục thế giới: {worldName}");
                return null;
            }

            string worldInfoPath = Path.Combine(worldFolder, "worldInfo.json");
            if (!File.Exists(worldInfoPath))
            {
                Debug.LogError($"Không tìm thấy file worldInfo.json trong thư mục: {worldFolder}");
                return null;
            }

            try
            {
                // Đọc nội dung từ file
                string json = File.ReadAllText(worldInfoPath);
                
                // Deserialize JSON thành đối tượng WorldInfo
                WorldInfo loadedInfo = JsonConvert.DeserializeObject<WorldInfo>(json);
                
                // Gán vào biến worldInfo
                worldInfo = loadedInfo;

                // Sync to techState
                if (_techState != null && worldInfo != null)
                {
                    _techState.TechFireArrow = worldInfo.TechFireArrow;
                    _techState.TechPenetrating = worldInfo.TechPenetrating;
                    _techState.TechForge = worldInfo.TechForge;
                    _techState.TechAdhesive = worldInfo.TechAdhesive;
                }
                
                // Load extraPower vào EnemyWaveManager (Storm Invasion)
                if (EnemyWaveManager.Instance != null)
                {
                    EnemyWaveManager.Instance.SetExtraPower(worldInfo.ExtraPower);
                    Debug.Log($"[WorldFileManager] Loaded ExtraPower: {worldInfo.ExtraPower}");
                }
                
                // Thông báo rằng worldInfo đã thay đổi
                OnWorldInfoChanged?.Invoke(worldInfo);
                
                return worldInfo;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Lỗi khi load WorldInfo từ {worldInfoPath}: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Hàm này giúp reload lại thông tin thế giới từ file, hữu ích khi cần
        /// cập nhật lại worldInfo sau khi có thay đổi trên đĩa.
        /// </summary>
        /// <returns>True nếu reload thành công, False nếu thất bại</returns>
        public bool ReloadWorldInfo()
        {
            if (worldInfo == null)
            {
                Debug.LogWarning("Không có WorldInfo nào để reload.");
                return false;
            }

            return LoadWorldInfo(worldInfo.Name) != null;
        }
        
        /// <summary>
        /// Đồng bộ hóa dữ liệu tech từ worldInfo sang PrefabData để sử dụng trong gameplay.
        /// Gọi hàm này sau khi load thế giới để cập nhật trạng thái tech cho game.
        /// </summary>
        public void SyncTechToPrefabData()
        {
            // Phương thức này không còn cần thiết vì PrefabData không còn chứa các biến tech
            // Thay vào đó, các component khác sẽ đọc trực tiếp từ WorldInfo thông qua sự kiện OnWorldInfoChanged
            
            if (worldInfo == null)
            {
                Debug.LogWarning("Không thể đồng bộ tech: worldInfo là null");
                return;
            }
            
            // Thông báo cho tất cả các thành phần quan tâm rằng worldInfo đã được tải
            OnWorldInfoChanged?.Invoke(worldInfo);
            
            //Debug.Log("Đã thông báo cho các thành phần về dữ liệu tech hiện tại");
        }
        
        /// <summary>
        /// Đồng bộ hóa dữ liệu tech và lưu vào file.
        /// Gọi hàm này khi có thay đổi tech trong gameplay để cập nhật và lưu lại.
        /// </summary>
        public void SyncAndSaveTech()
        {
            if (worldInfo == null)
            {
                Debug.LogWarning("Không thể đồng bộ tech: worldInfo là null");
                return;
            }
            
            // Thông báo rằng worldInfo đã thay đổi
            OnWorldInfoChanged?.Invoke(worldInfo);
            
            // Lưu thay đổi vào file
            SaveTechStatus(
                worldInfo.TechFireArrow,
                worldInfo.TechPenetrating,
                worldInfo.TechForge, 
                worldInfo.TechAdhesive
            );
            
            // Debug.Log("Đã lưu thông tin tech vào worldInfo");
        }
        
        /// <summary>
        /// Phương thức công khai để thông báo rằng worldInfo đã thay đổi,
        /// có thể được gọi từ các lớp khác
        /// </summary>
        public void NotifyWorldInfoChanged()
        {
            if (worldInfo != null)
            {
                OnWorldInfoChanged?.Invoke(worldInfo);
                // Debug.Log("Đã thông báo worldInfo thay đổi");
            }
            else
            {
                Debug.LogWarning("Không thể thông báo thay đổi vì worldInfo là null");
            }
        }
        
      

        //hàm này trả về path của fodel có tên được truyền vào trong /saves/ 
        public string GetWorldFolderPath(string worldName)
        {
            // Xác định đường dẫn của thư mục world dựa trên tên của nó
            string worldFolderPath = Path.Combine(saveRootPath, worldName);

            // Kiểm tra xem thư mục có tồn tại không
            if (Directory.Exists(worldFolderPath))
            {
                return worldFolderPath; // Trả về đường dẫn nếu thư mục tồn tại
            }
            else
            {
                Debug.LogWarning($"World folder '{worldName}' does not exist at path: {worldFolderPath}");
                return null; // Trả về null nếu không tìm thấy thư mục
            }
        }


        #region Active World Path Management

        /// <summary>
        /// Thiết lập đường dẫn thế giới đang hoạt động từ PlayerPrefs
        /// </summary>
        public void LoadActiveWorldPath()
        {
            activeWorldPath = PlayerPrefs.GetString("ActiveWorldPath", "");
        }

        /// <summary>
        /// Lấy đường dẫn thế giới đang hoạt động
        /// </summary>
        public string GetActiveWorldPath()
        {
            return activeWorldPath;
        }

        /// <summary>
        /// Thiết lập đường dẫn thế giới đang hoạt động
        /// </summary>
        public void SetActiveWorldPath(string path)
        {
            activeWorldPath = path;
            PlayerPrefs.SetString("ActiveWorldPath", path);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Kiểm tra xem đường dẫn thế giới đang hoạt động có hợp lệ không (tồn tại thư mục)
        /// </summary>
        public bool IsActiveWorldPathValid()
        {
            return !string.IsNullOrEmpty(activeWorldPath) && Directory.Exists(activeWorldPath);
        }

        /// <summary>
        /// Load world info và map từ thế giới đang hoạt động
        /// </summary>
        /// <returns>True nếu load thành công, False nếu thất bại</returns>
        public bool LoadActiveWorldData()
        {
            if (!IsActiveWorldPathValid()) return false;

            LoadFromSave loader = _loadFromSave != null ? _loadFromSave : LoadFromSave.Instance;
            if (loader == null) return false;

            bool worldInfoLoaded = loader.LoadWorldInfoFromSave(activeWorldPath);
            if (!worldInfoLoaded) return false;

            bool mapLoaded = loader.LoadMapFromSave(activeWorldPath);
            return mapLoaded;
        }


        /// <summary>
        /// Hủy bỏ việc load game và quay về menu
        /// </summary>
        public void CancelGameLoadAndReturnToMenu()
        {
            Debug.LogError("Game loading failed, returning to main menu");
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenuScene");
        }

        /// <summary>
        /// Kiểm tra tính hợp lệ của ActiveWorldPath và load data nếu thành công,
        /// hoặc hủy load nếu thất bại
        /// </summary>
        public bool ValidateAndLoadActiveWorld()
        {
            LoadActiveWorldPath(); // Đảm bảo activeWorldPath được load từ PlayerPrefs

            if (IsActiveWorldPathValid())
            {
                return LoadActiveWorldData();
            }
            else
            {
                CancelGameLoadAndReturnToMenu();
                return false;
            }
        }

        #endregion
    }
}
