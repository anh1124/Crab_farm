using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Script.GUI_Controller.StartMenu;
using Script.Data_Model;

namespace Script.Save_Controller
{
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }
        public string whatSavesNameUse;
        private void Awake()     
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject); // Giữ instance này khi chuyển scene
            }
            else
            {
                Destroy(gameObject); // Nếu đã tồn tại ingstance khác, hủy bản sao này
            }                         
        }
        
        // Class này chứa thông tin cơ bản của thế giới
        public class WorldInfo
        {
            public string Name { get; set; }
            public System.DateTime CreationDate { get; set; }

            public WorldInfo(string name, System.DateTime creationDate)
            {
                Name = name;
                CreationDate = creationDate;
            }
        }
        [SerializeField] private string saveRootPath; // Biến lưu đường dẫn đến thư mục save
        public List<WorldInfo> WorldInfos { get; private set; } // Danh sách lưu trữ các đối tượng WorldInfo
        private void Start()
        {
            //InitializeSaves();
        }

        //tạo thư mục saves
        public void InitializeSaves()
        {
            // Đường dẫn gốc cho thư mục lưu trữ trong Android
            string rootPath = Application.persistentDataPath;

            // Đường dẫn đến thư mục saves
            saveRootPath = Path.Combine(rootPath, "saves");

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

        // Hàm tạo thư mục lưu trữ mới cho một thế giới
        public void CreateWorldSave(string worldName)
        {
            string worldPath = Path.Combine(saveRootPath, worldName);
 
            if (!Directory.Exists(worldPath))
            {
                Directory.CreateDirectory(worldPath);
                Directory.CreateDirectory(Path.Combine(worldPath, "map"));

                // Create an empty player data file
                string playerDataPath = Path.Combine(worldPath, "playerData.json");
                if (!File.Exists(playerDataPath))
                {
                    PlayerData emptyPlayerData = new PlayerData();
                    SavePlayerDataToJson(emptyPlayerData, playerDataPath);
                }
                
                Debug.Log("Created new world save directories for: " + worldName);
            }
            else
            {
                Debug.Log("World save directories already exist for: " + worldName);
            }
            //theem hamf go playscene
            //thêm hàm load play save
        }
        
        //player data save load=======================================================
        private void SavePlayerDataToJson(PlayerData playerData, string playerDataPath)
        {
            string playerDataJson = JsonUtility.ToJson(playerData);
            File.WriteAllText(playerDataPath, playerDataJson);
            Debug.Log("Player data saved to: " + playerDataPath);
        }
        public void LoadPlayerDataFromJson(PlayerData playerData, string worldName)
        {
            string worldPath = Path.Combine(saveRootPath, worldName);
            string playerDataPath = Path.Combine(worldPath, "playerData.json");

            if (File.Exists(playerDataPath))
            {
                string playerDataJson = File.ReadAllText(playerDataPath);
                JsonUtility.FromJsonOverwrite(playerDataJson, playerData);
                Debug.Log("Player data loaded from: " + playerDataPath);
            }
            else
            {
                Debug.LogWarning("No player data file found in: " + playerDataPath);
            }
        }
        
        
        // Hàm kiểm tra xem một save có tồn tại hay không
        public bool SaveExists(string worldName)
        {
            string worldPath = Path.Combine(saveRootPath, worldName);
            return Directory.Exists(worldPath);
        }
        
       
        
        // Hàm lấy danh sách các thư mục trong root và chuyển đổi thành danh sách các đối tượng WorldInfo
        public void LoadWorldInfos()
        {
            WorldInfos = new List<WorldInfo>();

            if (Directory.Exists(saveRootPath))
            {
                string[] directories = Directory.GetDirectories(saveRootPath);

                foreach (string dir in directories)
                {
                    string name = Path.GetFileName(dir);
                    System.DateTime creationDate = Directory.GetCreationTime(dir);
                    WorldInfos.Add(new WorldInfo(name, creationDate));
                }

                Debug.Log($"Number of world saves: {WorldInfos.Count}");
                foreach (var worldInfo in WorldInfos)
                {
                   // Debug.Log($"World Name: {worldInfo.Name}, Creation Date: {worldInfo.CreationDate}");
                }
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

                // Thông báo debug để xem dữ liệu trả về
                //Debug.Log($"GetWorldInfoByIndex: , World Name: {worldInfo.Name}, Creation Date: {worldInfo.CreationDate}");

                return worldInfo;
            }
            else
            {
                Debug.LogError("GetWorldInfoByIndex: Index out of range");
                return null;
            }
        }
        
    }
}
