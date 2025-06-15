using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Script.GUI_Controller.StartMenu;
using Script.Data_Model;
using Newtonsoft.Json;

namespace Script.Save_Controller
{
    public class WorldFileManager : MonoBehaviour
    {
        public static WorldFileManager Instance { get; private set; }
        public string saveRootPath;

        [Tooltip("thông tin thế giới hiện tạitại")]
        [SerializeField] public WorldInfo worldInfo;
        
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
        
        //new new world list for chosse save
        public List<WorldInfo> WorldInfos { get; private set; } // Danh sách lưu trữ các đối tượng WorldInfo

        #region save file edit

        //tạo thư mục saves
        public void CreateSavesFolderIfNotExists()
        {
            // Tạo thư mục saves nếu chưa tồn tại
            if (!Directory.Exists(saveRootPath))
            {
                Directory.CreateDirectory(saveRootPath);
                // Debug.Log("Created saves directory at: " + saveRootPath);
            }
            else
            {
                // Debug.Log("Saves directory already exists at: " + saveRootPath);
            }
        }

        // Hàm tạo thư mục lưu trữ mới cho một thế giới và tạo file JSON từ WorldInfo
        public bool CreateWorldSave(string worldName, string seedValue)
        {
            //kiểm tra hợp lệ của thế giới

            #region kiểm tra hợp lệ của input world

            if (string.IsNullOrEmpty(worldName))
            {
                Debug.LogError("Tên thế giới để trống nên không thể tạo được");
                return false;
            }

            int seed;
            if (!int.TryParse(seedValue, out seed))
            {
                seed = 0; // Nếu không chuyển đổi được, gán seed là 0
                Debug.LogWarning("seed nhập vào không thể chuyển đổi,được đặt mặc định là 0");
            }

            string worldPath = Path.Combine(saveRootPath, worldName);

            // Kiểm tra và tạo tên mới nếu thư mục đã tồn tại
            while (Directory.Exists(worldPath))
            {
                Debug.LogError("Tên thế giới đã tồn tại,không thể tạo được");
                return false;
            }

            #endregion

            

            // Tạo thư mục world
            Directory.CreateDirectory(worldPath);
            // Ghi thông tin WorldInfo vào file JSON
            this.worldInfo = SaveWorldInfoToJson(worldName, seed, worldPath);
            
            // Thông báo rằng worldInfo đã thay đổi
            OnWorldInfoChanged?.Invoke(worldInfo);
    
            // Debug.Log($"Created new world save directories for: {worldName}");
            
            
            //cái hàm này phải bỏ vào chỗ khác
            // Tạo thư mục con 'map' bên trong world để lưu trữ dữ liệu bản đồ
            Directory.CreateDirectory(Path.Combine(worldPath, "map"));
            
            return true;
        }
        
        #endregion

        // Hàm tạo file JSON từ đối tượng WorldInfo
        private WorldInfo SaveWorldInfoToJson(string worldName, int seed, string worldPath)
        {
            WorldInfo newWorldInfo = new WorldInfo(
                name: worldName,
                creationDate: System.DateTime.Now,
                seed: seed,
                gameHour: 0,
                gameMinute: 0,
                gameDay: 0,
                gameMonth: 0,
                gameYear: 0,
                techFireArrow: false,
                techPenetrating: false,
                techForge: false,
                techAdhesive: false
            );

            string worldInfoPath = Path.Combine(worldPath, "worldInfo.json");

            string worldInfoJson = JsonConvert.SerializeObject(newWorldInfo, Formatting.Indented);

            File.WriteAllText(worldInfoPath, worldInfoJson);
            // Debug.Log("World info saved to: " + worldInfoPath);
            
            return newWorldInfo;
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
            var declaringType = new System.Diagnostics.StackTrace().GetFrame(1).GetMethod().DeclaringType;
            if (declaringType != null)
            {
                string callingScript = declaringType.Name;
                // Debug.Log($"{callingScript} save map");
            }

            // Các logic save game
            EntitySaveManager.Instance.SaveAllContainer();
            ChunkManager.Instance.SaveMapToJson(PlayerPrefs.GetString("SaveFilePath", ""));
            SaveGameTime();
            
            // Sử dụng worldInfo trực tiếp thay vì PrefabData
            if (worldInfo != null)
            {
                SaveTechStatus(
                    worldInfo.TechFireArrow,
                    worldInfo.TechPenetrating,
                    worldInfo.TechForge,
                    worldInfo.TechAdhesive
                );
            }
            else
            {
                Debug.LogWarning("worldInfo là null, không thể lưu trạng thái tech.");
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
            worldInfo.GameHour = GameTimeManager.Instance.GameHour;
            worldInfo.GameMinute = GameTimeManager.Instance.GameMinute;
            worldInfo.GameDay = GameTimeManager.Instance.GameDay;
            worldInfo.GameMonth = GameTimeManager.Instance.GameMonth;
            worldInfo.GameYear = GameTimeManager.Instance.GameYear;

            // Lấy đường dẫn tới file worldInfo.json
            string worldFolder = GetWorldFolderPath(worldInfo.Name);
            if (worldFolder == null)
            {
                Debug.LogError("Không tìm thấy folder của world để lưu thời gian.");
                return;
            }
            string worldInfoPath = Path.Combine(worldFolder, "worldInfo.json");

            // Serialize và ghi file JSON
            string json = JsonConvert.SerializeObject(worldInfo, Formatting.Indented);
            File.WriteAllText(worldInfoPath, json);
            // Debug.Log("Game time đã được lưu vào " + worldInfoPath);
            
            // Thông báo rằng worldInfo đã thay đổi
            OnWorldInfoChanged?.Invoke(worldInfo);
        }
        public void SaveTechStatus(bool techFireArrow, bool techPenetrating, bool techForge, bool techAdhesive)
        {
            // Kiểm tra xem worldInfo có tồn tại không
            if (worldInfo == null)
            {
                Debug.LogWarning("Không có dữ liệu WorldInfo để lưu trạng thái tech.");
                return;
            }

            // Update tech status
            worldInfo.TechFireArrow = techFireArrow;
            worldInfo.TechPenetrating = techPenetrating;
            worldInfo.TechForge = techForge;
            worldInfo.TechAdhesive = techAdhesive;

            // Get path to worldInfo.json
            string worldFolder = GetWorldFolderPath(worldInfo.Name);
            if (worldFolder == null)
            {
                Debug.LogError("Không tìm thấy folder của world để lưu trạng thái tech.");
                return;
            }
            string worldInfoPath = Path.Combine(worldFolder, "worldInfo.json");

            // Serialize and write JSON file
            string json = JsonConvert.SerializeObject(worldInfo, Formatting.Indented);
            File.WriteAllText(worldInfoPath, json);
            // Debug.Log("Trạng thái tech đã được lưu vào " + worldInfoPath);
            
            // Thông báo rằng worldInfo đã thay đổi
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
                
                // Debug.Log($"Đã load thông tin thế giới: {worldName}");
                
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
        
        #region StartMenuController use

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
        // Hàm kiểm tra xem một save có tồn tại hay không
        public void DeleteWorldSave(string worldName)
        {
            string worldPath = Path.Combine(saveRootPath, worldName);

            if (Directory.Exists(worldPath))
            {
                Directory.Delete(worldPath, true); // Xóa thư mục và tất cả các tệp tin bên trong
                // Debug.Log($"Deleted world save directory for: {worldName}");
            }
            else
            {
                Debug.LogError($"World save directory does not exist: {worldName}");
            }
        }
        // Hàm lấy danh sách các thư mục trong root và chuyển đổi thành danh sách các đối tượng WorldInfo và hiển thị chúng lên
        public void LoadWorldInfosToChooseSavePanel()
        {
            WorldInfos = new List<WorldInfo>();

            if (Directory.Exists(saveRootPath))
            {
                string[] directories = Directory.GetDirectories(saveRootPath);

                foreach (string dir in directories)
                {
                    string worldInfoPath = Path.Combine(dir, "worldInfo.json");

                    // Kiểm tra xem file worldInfo.json có tồn tại trong thư mục không
                    if (File.Exists(worldInfoPath))
                    {
                        try
                        {
                            // Đọc nội dung của file worldInfo.json
                            string json = File.ReadAllText(worldInfoPath);

                            // Parse JSON để tạo đối tượng WorldInfo
                            WorldInfo worldInfo = JsonConvert.DeserializeObject<WorldInfo>(json);

                            // Thêm đối tượng vào danh sách WorldInfos
                            if (worldInfo != null)
                            {
                                WorldInfos.Add(worldInfo);
                                // Debug.Log($"Loaded WorldInfo: {worldInfo.Name}");
                            }
                        }
                        catch (System.Exception ex)
                        {
                            Debug.LogError($"Failed to load WorldInfo from {worldInfoPath}. Error: {ex.Message}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"worldInfo.json not found in directory: {dir}");
                    }
                }

                // Debug.Log($"Number of world saves loaded: {WorldInfos.Count}");
            }
            else
            {
                Debug.LogError("Save root path does not exist.");
            }
        }
        public int CountWorldArr()
        {
            return WorldInfos.Count;
        }
        // Hàm trả về đối tượng WorldInfo theo chỉ số mảng
        public WorldInfo GetWorldInfoByIndex(int index)
        {
            if (index >= 1 && index <= WorldInfos.Count)
            {
                WorldInfo selectedWorldInfo = WorldInfos[index - 1]; // index = 1 tương đương vị trí mảng = 0
                
                // Có thể chọn gán trực tiếp vào worldInfo nếu muốn
                // worldInfo = selectedWorldInfo;  // Uncomment nếu muốn gán trực tiếp
                
                return selectedWorldInfo;
            }
            else
            {
                Debug.LogError("GetWorldInfoByIndex: Index out of range");
                return null;
            }
        }
        
        /// <summary>
        /// Hàm này kết hợp việc chọn một thế giới từ danh sách và load thông tin đầy đủ từ file.
        /// Sau khi load, nó sẽ đồng bộ tech vào PrefabData để sẵn sàng cho gameplay.
        /// </summary>
        /// <param name="index">Chỉ số của thế giới trong danh sách</param>
        /// <returns>True nếu thành công, False nếu thất bại</returns>
        public bool SelectAndLoadWorldInfo(int index)
        {
            WorldInfo selectedInfo = GetWorldInfoByIndex(index);
            if (selectedInfo != null)
            {
                // Load the full world info from file
                WorldInfo loadedInfo = LoadWorldInfo(selectedInfo.Name);
                if (loadedInfo != null)
                {
                    // Đồng bộ các thông tin tech vào PrefabData
                    SyncTechToPrefabData();
                    
                    // Debug.Log($"Đã chọn và load thế giới: {loadedInfo.Name}");
                    return true;
                }
            }
            
            Debug.LogError("Không thể chọn và load thế giới với index: " + index);
            return false;
        }
        #endregion
    }
}
