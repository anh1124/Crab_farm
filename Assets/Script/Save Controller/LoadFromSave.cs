using System;
using System.IO;
using Newtonsoft.Json;
using Script;
using Script.Save_Controller;
using UnityEngine;

public class LoadFromSave : MonoBehaviour
{
    public static LoadFromSave Instance { get; private set; }
    public string saveFilePath;
    public SaveManager.WorldInfo worldInfo;
   
    #region worldSaveJson

    public int seed;
    public string worldName;
    
    #endregion
    private void Awake()
    {
        AddSingleton();
    }
    private void AddSingleton()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    public void DoLoadFromSave()
    {
        //kiểm tra đường dẫn file
        if (!saveFilePathCheck())
        {
            Debug.LogError("Save file path is not set in PlayerPrefs.");
            return;
        }
        
        //kiểm tra file worldInfo.json
        LoadWorldInfoJson();
        GameTimeManager.Instance.SetGameTimeSave();
        //các structure và map hoặc linh tinh sẽ được load ở hàm dưới vì nó phụ thuộc vào việc map đã được tạo hay chưa 
        LoadMapJson();
    }

    private void LoadMapJson()
    {
        //kiểm tra file map.json
        string mapFilePath = Path.Combine(saveFilePath, "map.json");
        if (File.Exists(mapFilePath))
        {
            // File map đã tồn tại, load map từ file
            ChunkManager.Instance.LoadMapFromJson(mapFilePath);
            Debug.Log("Đã load map từ file save.");
            
            SetUpContainer.Instance.LoadTrees();
        }
        else
        {
            // File map chưa tồn tại, tạo map mới từ seed
            Debug.Log($"File map.json không tồn tại, tạo map mới với seed: {seed}");
            ChunkManager.Instance.GenerateFirstMap(seed);
            
            // Sau khi tạo map mới, lưu lại ngay
            ChunkManager.Instance.SaveMapToJson(saveFilePath);
        }
    }
    private void LoadWorldInfoJson()
    {
        string worldInfoPath = Path.Combine(saveFilePath, "worldInfo.json");

        if (File.Exists(worldInfoPath))
        {
            try
            {
                string json = File.ReadAllText(worldInfoPath);
                worldInfo = JsonConvert.DeserializeObject<SaveManager.WorldInfo>(json);

                // Kiểm tra Seed
                if (worldInfo.Seed == 0)
                {
                    worldInfo.Seed = UnityEngine.Random.Range(1, 10000000);
                    Debug.Log($"Seed not found, generated random Seed: {worldInfo.Seed}");
                    
                    // Ghi lại Seed mới vào file worldInfo.json
                    string updatedJson = JsonConvert.SerializeObject(worldInfo, Formatting.Indented);
                    File.WriteAllText(worldInfoPath, updatedJson);
                    Debug.Log("Updated Seed and saved back to worldInfo.json.");
                }
                else
                {
                    Debug.Log($"Loaded Seed: {worldInfo.Seed}");
                }

                // Kiểm tra CreationDate
                if (worldInfo.CreationDate == default(DateTime))
                {
                    Debug.LogError("CreationDate not found in worldInfo.json.");
                }
                else
                {
                    Debug.Log($"Loaded CreationDate: {worldInfo.CreationDate}");
                }

                // Debug thông tin khác
             
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load worldInfo.json. Error: {ex.Message}");
            }
        }
        else
        {
            Debug.LogWarning($"worldInfo.json not found in directory: {saveFilePath}");
        }

        seed = worldInfo.Seed;
        worldName = worldInfo.Name;
    }
    public bool saveFilePathCheck()
    {
        // Lấy đường dẫn lưu trữ từ PlayerPrefs
         saveFilePath = PlayerPrefs.GetString("SaveFilePath", "");
        // Kiểm tra xem đường dẫn có hợp lệ không
        if (string.IsNullOrEmpty(saveFilePath))
        {
            return false;
        }
        return true;
    }
    
    
    //kiểm tra file có tồn tại trong path không
    public bool ExistenceFileCheck(string saveFilePath,string fileName)
    {
        // Kiểm tra sự tồn tại của tệp map.json trong thư mục
        string mapFilePath = Path.Combine(saveFilePath,fileName);
        if (!File.Exists(mapFilePath))
        {
            return false;
        }
        return true;
    }
    
    // Hàm để tải dữ liệu từ file map.json
    private void LoadMapData(string mapFilePath)
    {
        // Đọc dữ liệu từ tệp và xử lý (ví dụ: parse JSON, khôi phục trạng thái trò chơi)
        string mapData = File.ReadAllText(mapFilePath);
        Debug.Log("Map data loaded: " + mapData);
        // Thêm logic xử lý dữ liệu map tại đây
    }
}