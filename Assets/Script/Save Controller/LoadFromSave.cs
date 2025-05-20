using System;
using System.IO;
using Newtonsoft.Json;
using Script;
using Script.Data_Model;
using Script.Save_Controller;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadFromSave : MonoBehaviour
{
    /*
     *lưu ý,các hàm được chạy khi bắt đầu được load từ world.json sẽ lấy thông tin world từ LoadFromSave.worldInfo
     */
    
    public static LoadFromSave Instance { get; private set; }
    public string saveFilePath;
    public WorldInfo worldInfo;
   
  
    private void Awake()
    {
        AddSingleton();
    }
    private void AddSingleton()
    {
        if (Instance == null)
        {
            Instance = this;
          
        }
        else
        {
            Destroy(gameObject);
        }
    }
    public bool DoLoadFromSaveFilePath(string filePath)
    {
        saveFilePath = filePath;
        
        //kiểm tra đường dẫn file null?
        if (string.IsNullOrEmpty(saveFilePath))
        {
            Debug.LogError("filepath is null or empty");
            return false;
        }
        
        
        //kiểm tra file worldInfo.json
        //và lấy data trong worldInfo.json truyền vào class worldInfo ở script này
        if (!ExtractDataFromWorldInfoJson()) { return false; }

        
        //các structure và map hoặc linh tinh sẽ được load ở hàm dưới vì nó phụ thuộc vào việc map đã được tạo hay chưa 
        LoadMapJson();
        
        GameTimeManager.Instance.SetGameTimeFromSave();
        
        //instance OtherCharactor on container
        EntitySaveManager.Instance.LoadAllContainer();
        //EntitySaveManager.Instance.SaveAllContainer();//vì tạo cây trong hàm tạo thế giới nên phải lưu ngay sau kho load ra
        return true;
    }

    private void LoadMapJson()
    {
        try
        {
            //kiểm tra file map.json
            string mapFilePath = Path.Combine(saveFilePath, "map.json");
            
            // Ensure ChunkManager instance exists
            if (ChunkManager.Instance == null)
            {
                Debug.LogError("ChunkManager.Instance is null! Cannot load or generate map.");
                return;
            }
            
            if (File.Exists(mapFilePath))// File map đã tồn tại, load map từ file
            {
                Debug.Log("Tìm thấy file map.json, đang load map...");
                try
                {
                    ChunkManager.Instance.LoadMapFromJson(mapFilePath);
                    Debug.Log("Đã load map từ file save.");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Lỗi khi load map từ file: {ex.Message}");
                    Debug.LogWarning("Thử tạo map mới do lỗi khi load...");
                    GenerateNewMap();
                }
            }
            else  // File map chưa tồn tại, tạo map mới từ seed
            {
                Debug.Log($"File map.json không tồn tại, tạo map mới với seed từ worldInfo.json: {worldInfo.Seed}");
                GenerateNewMap();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Lỗi không xác định trong LoadMapJson: {ex.Message}");
        }
    }
    
    private void GenerateNewMap()
    {
        try
        {
            // Verify worldInfo and seed are valid
            if (worldInfo == null)
            {
                Debug.LogError("worldInfo is null! Cannot generate map.");
                return;
            }
            
            ChunkManager.Instance.GenerateFirstMap(worldInfo.Seed);
            
            // Sau khi tạo map mới, lưu lại ngay
            ChunkManager.Instance.SaveMapToJson(saveFilePath);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Lỗi khi tạo map mới: {ex.Message}");
        }
    }
  

    private bool ExtractDataFromWorldInfoJson()
    {
        string worldInfoJsonPath = Path.Combine(saveFilePath, "worldInfo.json");

        if (File.Exists(worldInfoJsonPath))
        {
            try
            {
                string json = File.ReadAllText(worldInfoJsonPath);
                worldInfo = JsonConvert.DeserializeObject<WorldInfo>(json);
                
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
                return false;
            }
        }
        else
        {
            
            Debug.LogWarning($"worldInfo.json not found in directory: {saveFilePath}");
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