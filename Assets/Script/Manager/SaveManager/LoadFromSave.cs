using System;
using System.IO;
using Newtonsoft.Json;
using Script;
using Script.Data_Model;
using Script.Manager;
using Script.Save_Controller;
using UnityEngine;
using UnityEngine.SceneManagement;
using Script.Manager.WorldManager;

[DefaultExecutionOrder(-70)]
public class LoadFromSave : MonoBehaviour
{
    /*
     *lưu ý,các hàm được chạy khi bắt đầu được load từ world.json sẽ lấy thông tin world từ LoadFromSave.worldInfo
     */
    
    public static LoadFromSave Instance { get; private set; }
    public string saveFilePath;
    public WorldInfo worldInfo;
    public bool IsNewWorld { get; private set; }

   
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

    [Obsolete("Use modular methods: LoadWorldInfoFromSave, LoadMapFromSave, LoadTechAndTimeFromSave, LoadEntitiesFromSave", true)]
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

        var gtm = FindObjectOfType<GameTimeManager>(true);
        if (gtm != null) gtm.SetGameTimeFromSave(worldInfo);

        // Load tech status from WorldInfo to PrefabData
        LoadTechStatus();

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
            var chunkManager = FindObjectOfType<ChunkManager>(true);
            if (chunkManager == null)
            {
                Debug.LogError("ChunkManager is null! Cannot load or generate map.");
                return;
            }
            
            if (File.Exists(mapFilePath))// File map đã tồn tại, load map từ file
            {
                Debug.Log("Tìm thấy file map.json, đang load map...");
                try
                {
                    chunkManager.LoadMapFromJson(mapFilePath);
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
                if (worldInfo != null)
                {
                    Debug.Log($"File map.json không tồn tại, tạo map mới với seed từ worldInfo.json: {worldInfo.Seed}");
                }
                else
                {
                    Debug.LogWarning("File map.json không tồn tại và worldInfo cũng null. Tạo map mới với seed mặc định (0).");
                }
                GenerateNewMap();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Lỗi không xác định trong LoadMapJson: {ex.Message}");
        }
    }
    
    //khi file map.json không tồn tại, tạo map mới từ seed trong worldinfo từ saveFilePath hiện tại
    private void GenerateNewMap()
    {
        IsNewWorld = true;
        try

        {
            // Verify worldInfo and seed are valid
            if (worldInfo == null)
            {
                Debug.LogError("worldInfo is null! Cannot generate map.");
                return;
            }
            
            var chunkManager = FindObjectOfType<ChunkManager>(true);
            if (chunkManager != null)
            {
                int seed = (worldInfo != null) ? worldInfo.Seed : 0;
                chunkManager.GenerateFirstMap(seed);
                
                // Sau khi tạo map mới, lưu lại ngay
                chunkManager.SaveMapToJson(saveFilePath);
            }
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
                if (string.IsNullOrEmpty(worldInfo.CreationDate))
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
 
    // Method to load tech status from WorldInfo to PrefabData
    private void LoadTechStatus()
    {
        if (worldInfo != null)
        {
            // Không cần gán trực tiếp vào PrefabData nữa vì tech đã được di chuyển vào WorldInfo
            // Thay vào đó, sẽ gán thông tin tech từ worldInfo sang WorldFileManager.Instance.worldInfo
            // và thông báo thay đổi để các component cập nhật

            if (WorldFileManager.Instance != null && WorldFileManager.Instance.worldInfo != null)
            {
                // Cập nhật dữ liệu tech vào WorldFileManager
                WorldFileManager.Instance.worldInfo.TechFireArrow = worldInfo.TechFireArrow;
                WorldFileManager.Instance.worldInfo.TechPenetrating = worldInfo.TechPenetrating;
                WorldFileManager.Instance.worldInfo.TechForge = worldInfo.TechForge;
                WorldFileManager.Instance.worldInfo.TechAdhesive = worldInfo.TechAdhesive;

                // Sử dụng phương thức công khai để thông báo thay đổi
                WorldFileManager.Instance.NotifyWorldInfoChanged();

                Debug.Log("Đã tải trạng thái tech từ file save vào WorldFileManager");
            }
            else
            {
                Debug.LogWarning("WorldFileManager.Instance hoặc worldInfo của nó là null, không thể cập nhật tech");
            }
        }
        else
        {
            Debug.LogWarning("Không thể tải trạng thái tech: worldInfo của LoadFromSave là null");
        }
    }

    #region NEW MODULAR METHODS - Phase 1 Implementation

    /// <summary>
    /// PHASE 2: Load world info from worldInfo.json
    /// Called early in loading sequence for context
    /// Dependencies: WorldFileManager.Instance must exist
    ///
    /// HÀM NÀY LÀM GÌ:
    /// - Đọc file worldInfo.json từ thư mục save game
    /// - Parse JSON và tạo object WorldInfo
    /// - Lưu vào this.worldInfo để các hàm khác sử dụng
    /// - Cung cấp seed, tech, time data cho loading tiếp theo
    ///
    /// ĐƯỢC GỌI Ở: StartGameScrene.cs Phase 2
    /// CẦN GỌI TRƯỚC: LoadMapFromSave() (cần seed từ worldInfo)
    /// </summary>
    public bool LoadWorldInfoFromSave(string filePath)
    {
        // Validate dependencies
        if (WorldFileManager.Instance == null)
        {
            Debug.LogError("[LoadFromSave] WorldFileManager.Instance is null! Cannot load world info.");
            return false;
        }

        // Validate input
        if (string.IsNullOrEmpty(filePath))
        {
            Debug.LogError("[LoadFromSave] filePath is null or empty");
            return false;
        }

        // Set save path
        saveFilePath = filePath;

        // Extract worldInfo.json and populate this.worldInfo
        bool success = ExtractDataFromWorldInfoJson();

        if (success && worldInfo != null)
        {
            // Sync to WorldFileManager
            if (WorldFileManager.Instance != null)
            {
                WorldFileManager.Instance.worldInfo = worldInfo;
                WorldFileManager.Instance.NotifyWorldInfoChanged();
            }

            // Load hint state cho world này
            if (Script.Tutorial.HintSaveManager.Instance != null)
                Script.Tutorial.HintSaveManager.Instance.Load(filePath);

            Debug.Log($"[LoadFromSave] Successfully loaded world info for: {worldInfo.Name}");
        }
        else
        {
            Debug.LogError("[LoadFromSave] Failed to load world info from save");
        }

        return success;
    }

    /// <summary>
    /// PHASE 2: Load map data or generate new map
    /// Called after world info is loaded
    /// Dependencies: worldInfo must be loaded, ChunkManager.Instance must exist
    /// </summary>
    public bool LoadMapFromSave(string filePath)
    {
        // Validate dependencies
        if (worldInfo == null)
        {
            Debug.LogError("[LoadFromSave] WorldInfo not loaded! Call LoadWorldInfoFromSave first.");
            return false;
        }

        if (FindObjectOfType<ChunkManager>(true) == null)
        {
            Debug.LogError("[LoadFromSave] ChunkManager is null! Cannot load/generate map.");
            return false;
        }

        // Set save path
        saveFilePath = filePath;

        try
        {
            // Call existing LoadMapJson() method
            LoadMapJson();
            Debug.Log("[LoadFromSave] Map loading/generation completed successfully");
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[LoadFromSave] Exception during map loading: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// PHASE 4: Load tech status and game time
    /// Called before entity loading
    /// Dependencies: worldInfo must be loaded, managers must exist
    /// </summary>
    public void LoadTechAndTimeFromSave()
    {
        // Validate dependencies
        if (worldInfo == null)
        {
            Debug.LogWarning("[LoadFromSave] WorldInfo not loaded, skipping tech/time load");
            return;
        }

        if (WorldFileManager.Instance == null)
        {
            Debug.LogWarning("[LoadFromSave] WorldFileManager.Instance is null, skipping tech load");
            return;
        }

        var gtm = FindObjectOfType<GameTimeManager>(true);
        if (gtm == null)
        {
            Debug.LogWarning("[LoadFromSave] GameTimeManager is null, skipping time load");
            return;
        }

        try
        {
            // Load game time
            gtm.SetGameTimeFromSave(worldInfo);
            Debug.Log("[LoadFromSave] Game time loaded successfully");

            // Load tech status
            LoadTechStatus();
            Debug.Log("[LoadFromSave] Tech and time loading completed");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[LoadFromSave] Exception during tech/time loading: {ex.Message}");
        }
    }

    /// <summary>
    /// PHASE 4: Load all entities (walls, structures, enemies, etc.)
    /// Called last in loading sequence
    /// Dependencies: EntitySaveManager.Instance must exist
    /// </summary>
    public void LoadEntitiesFromSave()
    {
        // Validate dependencies
        if (EntitySaveManager.Instance == null)
        {
            Debug.LogError("[LoadFromSave] EntitySaveManager.Instance is null! Cannot load entities.");
            return;
        }

        try
        {
            // Load all entities via EntitySaveManager
            EntitySaveManager.Instance.LoadAllContainer();
            Debug.Log("[LoadFromSave] Entity loading completed successfully");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[LoadFromSave] Exception during entity loading: {ex.Message}");
        }
    }

    #endregion
}
