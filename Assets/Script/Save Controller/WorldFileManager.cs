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
                Debug.Log("Created saves directory at: " + saveRootPath);
            }
            else
            {
                Debug.Log("Saves directory already exists at: " + saveRootPath);
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
            SaveWorldInfoToJson(worldName, seed, worldPath);
    
            Debug.Log($"Created new world save directories for: {worldName}");
            
            
            //cái hàm này phải bỏ vào chỗ khác
            // Tạo thư mục con 'map' bên trong world để lưu trữ dữ liệu bản đồ
            Directory.CreateDirectory(Path.Combine(worldPath, "map"));
            
            return true;
        }
        
        #endregion

        
        

        
        // Hàm tạo file JSON từ đối tượng WorldInfo
        private void SaveWorldInfoToJson(string worldName, int seed, string worldPath)
        {
            WorldInfo worldInfo = new WorldInfo(
                name: worldName,
                creationDate: System.DateTime.Now,
                seed: seed,
                gameHour: 0,
                gameMinute: 0,
                gameDay: 0,
                gameMonth: 0,
                gameYear: 0
            );

            string worldInfoPath = Path.Combine(worldPath, "worldInfo.json");

            string worldInfoJson = JsonConvert.SerializeObject(worldInfo, Formatting.Indented);

            File.WriteAllText(worldInfoPath, worldInfoJson);
            Debug.Log("World info saved to: " + worldInfoPath);
        }
        
        //hàm này là hàm save chính 
        public void SaveCurrentMap()
        {
            var declaringType = new System.Diagnostics.StackTrace().GetFrame(1).GetMethod().DeclaringType;
            if (declaringType != null)
            {
                string callingScript = declaringType.Name;
                Debug.Log($"{callingScript} save map");
            }

            // Các logic save game
            EntitySaveManager.Instance.SaveAllContainer();
            ChunkManager.Instance.SaveMapToJson(PlayerPrefs.GetString("SaveFilePath", ""));
            SaveGameTime();
        }
        public void SaveGameTime()
        {
            // Giả sử bạn đã có đối tượng WorldInfo hiện hành
            WorldInfo worldInfo = LoadFromSave.Instance?.worldInfo;
            if (worldInfo == null)
            {
                Debug.LogWarning("Không có dữ liệu WorldInfo để lưu thời gian.");
                return;
            }

            // Lấy thời gian hiện tại từ GameTimeManager
            worldInfo.GameHour = GameTimeManager.Instance.GameHour;
            worldInfo.GameMinute = GameTimeManager.Instance.GameMinute;
            worldInfo.GameDay = GameTimeManager.Instance.GameDay;
            worldInfo.GameMonth = GameTimeManager.Instance.GameMonth;
            worldInfo.GameYear = GameTimeManager.Instance.GameYear;

            // Lấy đường dẫn tới file worldInfo.json (giả sử tên world được lưu trong worldInfo.Name)
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
            Debug.Log("Game time đã được lưu vào " + worldInfoPath);
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
                Debug.Log($"Deleted world save directory for: {worldName}");
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
                                Debug.Log($"Loaded WorldInfo: {worldInfo.Name}");
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

                Debug.Log($"Number of world saves loaded: {WorldInfos.Count}");
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
                WorldInfo worldInfo = WorldInfos[index - 1]; // index = 1 tương đương vị trí mảng = 0
                return worldInfo;
            }
            else
            {
                Debug.LogError("GetWorldInfoByIndex: Index out of range");
                return null;
            }
        }
        #endregion
    }
}
