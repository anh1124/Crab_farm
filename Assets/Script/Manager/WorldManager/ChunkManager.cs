using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Script;
using Script.Controller.GUI_Controller.PlayerGui;
using Script.Data_Model;
using UnityEngine;

namespace Script.Manager.WorldManager
{
public class ChunkManager : MonoBehaviour
{
    public static ChunkManager Instance { get; private set; }
    
    [System.Serializable]
    public class Chunk
    {
        public int id;  // ID để xác định vị trí trái/phải trên bản đồ
        public int prefabIndex;  // Index trong mảng chunkPrefabsList
        public float positionX;
        public string chunkType;  // Loại chunk: "ground", "impassableLeft", "impassableRight"

        public Chunk(int id, int prefabIndex, float positionX, string chunkType = "ground")
        {
            this.id = id;
            this.prefabIndex = prefabIndex;
            this.positionX = positionX;
            this.chunkType = chunkType;
        }
    }

    public int seed;
    [SerializeField] private string groundChunkPath = "Prefab/Chunk/ChunkGround";
    [SerializeField] private string impassableRockPath = "Prefab/Chunk/ImpassableRock";
   
    public List<GameObject> chunkPrefabsList = new List<GameObject>();
    public List<GameObject> impassableRockPrefabs = new List<GameObject>();
    
    public GameObject emptyChunk;//đây là chunk trống,được dùng trong save 
    //bởi vì khi các chunk khác được instance ra thì các container của các chunk đó đã được chuyển đi rồi 
    
    public List<Chunk> chunkList = new List<Chunk>();
    
    // References to specific impassable rock prefabs
    public GameObject impassableRockLeft;
    public GameObject impassableRockRight;

    private int currentChunkLeftId = 0;
    private int currentChunkRightId = 0;
    private int centerChunkId = 0;
    private float chunkWidth = 30f;

    private System.Random rng;
    private string savePath;

    private void Awake()
    {
        AddSingleton();
        LoadAllPrefabs();
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

    private void Start()
    {
        PlayerGui.Instance.OnSkill3PressedACT += test;
    }

    private void LoadAllPrefabs()
    {
        // Initialize the lists if they're null
        if (chunkPrefabsList == null)
        {
            chunkPrefabsList = new List<GameObject>();
        }
        else
        {
            chunkPrefabsList.Clear();
        }

        if (impassableRockPrefabs == null)
        {
            impassableRockPrefabs = new List<GameObject>();
        }
        else
        {
            impassableRockPrefabs.Clear();
        }

        // Load ground chunks
        LoadGroundChunks();
        
        // Load impassable rocks
        LoadImpassableRocks();
    }

    private void LoadGroundChunks()
    {
        GameObject[] loadedPrefabs = Resources.LoadAll<GameObject>(groundChunkPath);

        if (loadedPrefabs == null || loadedPrefabs.Length == 0)
        {
            Debug.LogWarning($"Không có prefabWall ground chunk nào được tìm thấy trong thư mục {groundChunkPath}. Thử các đường dẫn khác...");
            
            // Try alternative paths
            string[] alternativePaths = new string[]
            {
                "Prefab/Chunk/ChunkGround",
                "Art/Prefab/Chunk/ChunkGround",
                "Art/Resources/Prefab/Chunk/ChunkGround",
                "Chunk/ChunkGround",
                "ChunkGround"
            };
            
            TryLoadFromAlternativePaths(alternativePaths, ref chunkPrefabsList);
        }
        else
        {
            chunkPrefabsList.AddRange(loadedPrefabs);
           // Debug.Log($"Đã load {loadedPrefabs.Length} ground chunk prefabWall(s) thành công từ {groundChunkPath}.");
        }
    }

    private void LoadImpassableRocks()
    {
        GameObject[] loadedRocks = Resources.LoadAll<GameObject>(impassableRockPath);

        if (loadedRocks == null || loadedRocks.Length == 0)
        {
            Debug.LogWarning($"Không có prefabWall impassable rock nào được tìm thấy trong thư mục {impassableRockPath}. Thử các đường dẫn khác...");
            
            // Try alternative paths
            string[] alternativePaths = new string[]
            {
                "Prefab/Chunk/ImpassableRock",
                "Art/Prefab/Chunk/ImpassableRock",
                "Art/Resources/Prefab/Chunk/ImpassableRock",
                "Chunk/ImpassableRock",
                "ImpassableRock"
            };
            
            if (TryLoadFromAlternativePaths(alternativePaths, ref impassableRockPrefabs))
            {
                FindImpassableRockPrefabs();
            }
        }
        else
        {
            impassableRockPrefabs.AddRange(loadedRocks);
          //  Debug.Log($"Đã load {loadedRocks.Length} impassable rock prefabWall(s) thành công từ {impassableRockPath}.");
            FindImpassableRockPrefabs();
        }
    }

    private bool TryLoadFromAlternativePaths(string[] paths, ref List<GameObject> targetList)
    {
        foreach (string path in paths)
        {
            //Debug.Log($"Thử tải prefabWall từ: {path}");
            GameObject[] prefabs = Resources.LoadAll<GameObject>(path);
            
            if (prefabs != null && prefabs.Length > 0)
            {
               // Debug.Log($"Đã tìm thấy {prefabs.Length} prefabWall(s) ở đường dẫn: {path}");
                targetList.AddRange(prefabs);
                return true;
            }
        }
        
        Debug.LogError("Không thể tìm thấy prefabWall ở bất kỳ đường dẫn nào. Kiểm tra cấu trúc thư mục và định dạng prefabWall.");
        return false;
    }

    private void FindImpassableRockPrefabs()
    {
        foreach (GameObject rock in impassableRockPrefabs)
        {
            if (rock.name.Contains("Left"))
            {
                impassableRockLeft = rock;
             //   Debug.Log("Đã tìm thấy ImpassableRockLeft prefabWall: " + rock.name);
            }
            else if (rock.name.Contains("Right"))
            {
                impassableRockRight = rock;
               // Debug.Log("Đã tìm thấy ImpassableRockRight prefabWall: " + rock.name);
            }
        }

        // If still not found, try direct loading
        if (impassableRockLeft == null)
        {
            impassableRockLeft = Resources.Load<GameObject>("Prefab/Chunk/ImpassableRock/ImpassableRockLeft");
            if (impassableRockLeft != null)
            {
               // Debug.Log("Đã tải ImpassableRockLeft bằng Resources.Load");
                if (!impassableRockPrefabs.Contains(impassableRockLeft))
                    impassableRockPrefabs.Add(impassableRockLeft);
            }
        }

        if (impassableRockRight == null)
        {
            impassableRockRight = Resources.Load<GameObject>("Prefab/Chunk/ImpassableRock/ImpassableRockRight");
            if (impassableRockRight != null)
            {
               // Debug.Log("Đã tải ImpassableRockRight bằng Resources.Load");
                if (!impassableRockPrefabs.Contains(impassableRockRight))
                    impassableRockPrefabs.Add(impassableRockRight);
            }
        }
    }

    public void GenerateFirstMap(int Seed)
    {
        rng = new System.Random(Seed);
        seed = Seed;
        
        // Check if ground chunks are loaded
        if (chunkPrefabsList.Count == 0)
        {
            Debug.LogError("Không thể tạo map vì không có ground chunk prefabWall nào được load. Kiểm tra đường dẫn Resources và cấu trúc thư mục.");
            LoadGroundChunks();
            
            if (chunkPrefabsList.Count == 0)
            {
                Debug.LogError("Không thể tạo map sau khi reload prefabs. Sử dụng cơ chế dự phòng.");
                return;
            }
        }
        
        // Check if impassable rocks are loaded
        if (impassableRockPrefabs.Count == 0 || impassableRockLeft == null || impassableRockRight == null)
        {
            Debug.LogWarning("Không tìm thấy impassable rock prefabs. Thử tải lại...");
            LoadImpassableRocks();
        }
        
        // Get chunk count from PrefabData - this is the number of ground chunks only
        // Impassable rocks will be added separately
        int groundChunkCount = 3; // Default fallback value if PrefabData isn't available
        if (PrefabData.Instance != null)
        {
            groundChunkCount = PrefabData.Instance.currentCreatChunk;
          //  Debug.Log($"Using PrefabData.currentCreatChunk value: {groundChunkCount}");
        }
        else
        {
            Debug.LogWarning("PrefabData.Instance is null, using default value of 3 chunks");
        }
        
     //   Debug.Log($"Đang tạo map với {groundChunkCount} ground chunks và 2 impassable rocks");
        
        CreateInitialChunks(groundChunkCount);
        EntitySaveManager.Instance.SaveAllContainer();
    }

    private void CreateInitialChunks(int count)
    {
        // The count parameter is the total number of ground chunks we want to create
        int totalGroundChunks = count;
        
        // Ensure odd number for symmetry around center
        if (totalGroundChunks % 2 == 0) totalGroundChunks--;
        
        // Calculate how many chunks to create on each side of center
        int sideChunks = totalGroundChunks / 2;
        
        Debug.Log($"Creating a total of {totalGroundChunks} ground chunks (center + {sideChunks} on each side) + 2 impassable rocks");

        // Create center chunk
        CreateGroundChunk(new Vector3(0, 0, 0), centerChunkId);
        currentChunkLeftId = centerChunkId;
        currentChunkRightId = centerChunkId;

        // Create ground chunks to left and right
        for (int i = 1; i <= sideChunks; i++)
        {
            CreateGroundChunk(new Vector3(-i * chunkWidth, 0, 0), centerChunkId - i);
            currentChunkLeftId = centerChunkId - i;

            CreateGroundChunk(new Vector3(i * chunkWidth, 0, 0), centerChunkId + i);
            currentChunkRightId = centerChunkId + i;
        }

        // Add boundary rocks at the outer edges
        CreateImpassableRock(true);  // Left rock
        CreateImpassableRock(false); // Right rock

        // Sort the chunks by X position
        chunkList.Sort((a, b) => a.positionX.CompareTo(b.positionX));
        
        //Debug.Log($"Created total of {chunkList.Count} chunks (including impassable rocks)");
    }

    private Chunk CreateGroundChunk(Vector3 position, int id)
    {
        // Safety check
        if (chunkPrefabsList == null || chunkPrefabsList.Count == 0)
        {
            Debug.LogError("chunkPrefabsList is empty or null when trying to create chunk. Trying emergency reload...");
            
            LoadGroundChunks();
            
            // If still no prefabs, use a fallback
            if (chunkPrefabsList.Count == 0)
            {
                Debug.LogWarning("Tạo chunk dự phòng vì không thể load prefabs...");
                
                GameObject fallbackPrefab = emptyChunk;
                if (fallbackPrefab == null)
                {
                    fallbackPrefab = new GameObject("EmergencyChunk");
                    fallbackPrefab.AddComponent<Transform>();
                }

                GameObject emergencyChunk = Instantiate(fallbackPrefab, position, Quaternion.identity, 
                    EntitySaveManager.Instance.chunkContainer.transform);
                Chunk emergencyChunkData = new Chunk(id, 0, position.x, "ground");
                chunkList.Add(emergencyChunkData);
                
                if (EntitySaveManager.Instance != null)
                {
                    EntitySaveManager.Instance.MoveChildrenToContainers(emergencyChunk);
                }
                return emergencyChunkData;
            }
        }

        int prefabIndex = rng.Next(chunkPrefabsList.Count);
        GameObject selectedPrefab = chunkPrefabsList[prefabIndex];

        if (selectedPrefab == null)
        {
            Debug.LogError($"selectedPrefab is null at index {prefabIndex}. Attempting to use fallback...");
            
            if (emptyChunk != null)
            {
                selectedPrefab = emptyChunk;
            }
            else
            {
                Debug.LogError("No fallback prefabWall available, cannot create chunk");
                return null;
            }
        }

        GameObject newChunkObject = Instantiate(selectedPrefab, position, Quaternion.identity, EntitySaveManager.Instance.chunkContainer.transform);
        Chunk chunk = new Chunk(id, prefabIndex, position.x, "ground");
        chunkList.Add(chunk);

        if (EntitySaveManager.Instance != null)
        {
            EntitySaveManager.Instance.MoveChildrenToContainers(newChunkObject);
        }
        return chunk;
    }

    private void CreateImpassableRock(bool isLeft)
    {
        // Check if we have the needed prefabs
        if (isLeft && impassableRockLeft == null)
        {
            Debug.LogError("ImpassableRockLeft prefabWall not found! Cannot create left boundary.");
            return;
        }
        
        if (!isLeft && impassableRockRight == null)
        {
            Debug.LogError("ImpassableRockRight prefabWall not found! Cannot create right boundary.");
            return;
        }
        
        // Calculate position and ID based on the furthest ground chunks
        Vector3 position;
        int id;
        GameObject rockPrefab;
        
        if (isLeft)
        {
            position = new Vector3((currentChunkLeftId - 1) * chunkWidth, 0, 0);
            id = currentChunkLeftId - 1;
            rockPrefab = impassableRockLeft;
            // Update the leftmost chunk ID
            currentChunkLeftId = id;
        }
        else
        {
            position = new Vector3((currentChunkRightId + 1) * chunkWidth, 0, 0);
            id = currentChunkRightId + 1;
            rockPrefab = impassableRockRight;
            // Update the rightmost chunk ID
            currentChunkRightId = id;
        }
        
        // Find prefabWall index in impassable rocks list
        int prefabIndex = impassableRockPrefabs.IndexOf(rockPrefab);
        if (prefabIndex < 0) 
        {
            Debug.LogWarning($"Impassable rock prefabWall not found in list. Using default index 0.");
            prefabIndex = 0;
        }
        
        // Create the rock object
        GameObject rockObject = Instantiate(rockPrefab, position, Quaternion.identity, 
            EntitySaveManager.Instance.chunkContainer.transform);
        
        // Add to chunk list with appropriate type
        string chunkType = isLeft ? "impassableLeft" : "impassableRight";
        Chunk rock = new Chunk(id, prefabIndex, position.x, chunkType);
        chunkList.Add(rock);
        
        if (EntitySaveManager.Instance != null)
        {
            EntitySaveManager.Instance.MoveChildrenToContainers(rockObject);
        }
        
        //Debug.Log($"Created ImpassableRock {(isLeft ? "Left" : "Right")} at position {position} with ID {id}");
    }

    public void AddChunk(bool toRight)
    {
        if (chunkList.Count == 0) return;

        // Check if prefabs are loaded
        if (chunkPrefabsList.Count == 0)
        {
            Debug.LogError("Không thể thêm chunk vì không có prefabWall nào được load.");
            return;
        }

        // First, we need to remove the impassable rock on the side where we want to add a chunk
        RemoveImpassableRock(toRight);
        
        // Now add the new ground chunk
        float newX;
        int newId;

        if (toRight)
        {
            // Find the rightmost non-impassable chunk
            float maxX = FindRightmostGroundChunkX();
            newX = maxX + chunkWidth;
            newId = currentChunkRightId + 1;
            currentChunkRightId = newId;
        }
        else
        {
            // Find the leftmost non-impassable chunk
            float minX = FindLeftmostGroundChunkX();
            newX = minX - chunkWidth;
            newId = currentChunkLeftId - 1;
            currentChunkLeftId = newId;
        }

        bool chunkExists = chunkList.Exists(c => Mathf.Approximately(c.positionX, newX) && c.chunkType == "ground");
        if (!chunkExists)
        {
            Vector3 newPosition = new Vector3(newX, 0, 0);
            CreateGroundChunk(newPosition, newId);
        }

        // Add the impassable rock back at the new boundary
        // If toRight is true, add to the right (isLeft = false)
        // If toRight is false, add to the left (isLeft = true)
        CreateImpassableRock(!toRight);
        
        // Sort chunks by position
        chunkList.Sort((a, b) => a.positionX.CompareTo(b.positionX));
        
        EntitySaveManager.Instance.SaveAllContainer();
    }

    private void RemoveImpassableRock(bool fromRight)
    {
        // If fromRight is true, we're adding to the right, so remove the right impassable rock
        // If fromRight is false, we're adding to the left, so remove the left impassable rock
        string targetType = fromRight ? "impassableRight" : "impassableLeft";
        
        // Find the impassable rock
        Chunk rockToRemove = chunkList.Find(c => c.chunkType == targetType);
        
        if (rockToRemove != null)
        {
            // Find and destroy the GameObject
            foreach (Transform child in EntitySaveManager.Instance.chunkContainer.transform)
            {
                if (Mathf.Approximately((float)rockToRemove.positionX, child.position.x))
                {
                    Debug.Log($"Removing {targetType} at position {child.position.x}");
                    Destroy(child.gameObject);
                    break;
                }
            }
            
            // Remove from list
            chunkList.Remove(rockToRemove);
            
            // Update the current chunk ID based on the direction
            if (fromRight)
            {
                // We've removed the right impassable rock, so update the right ID
                currentChunkRightId--;
            }
            else
            {
                // We've removed the left impassable rock, so update the left ID
                currentChunkLeftId++;
            }
        }
        else
        {
            Debug.LogWarning($"No {targetType} found to remove");
        }
    }

    private float FindRightmostGroundChunkX()
    {
        float maxX = float.MinValue;
        foreach (var chunk in chunkList)
        {
            if (chunk.chunkType == "ground" && chunk.positionX > maxX)
            {
                maxX = chunk.positionX;
            }
        }
        return maxX;
    }

    private float FindLeftmostGroundChunkX()
    {
        float minX = float.MaxValue;
        foreach (var chunk in chunkList)
        {
            if (chunk.chunkType == "ground" && chunk.positionX < minX)
            {
                minX = chunk.positionX;
            }
        }
        return minX;
    }

    private void ClearCurrentMap()
    {
        foreach (Transform child in EntitySaveManager.Instance.chunkContainer.transform)
        {
            Destroy(child.gameObject);
        }
        chunkList.Clear();
        currentChunkLeftId = 0;
        currentChunkRightId = 0;
    }

    [System.Serializable]
    public class MapSaveData
    {
        public int seed;
        public int currentChunkLeftId;
        public int currentChunkRightId;
        public List<Chunk> chunks;
        public string chunkContainerName;
        public bool hasImpassableRocks; // Flag to indicate if the map includes boundary rocks

        public MapSaveData(int seed, int currentChunkLeftId, int currentChunkRightId, List<Chunk> chunks,
            string chunkContainerName)
        {
            this.seed = seed;
            this.currentChunkLeftId = currentChunkLeftId;
            this.currentChunkRightId = currentChunkRightId;
            this.chunks = chunks;
            this.chunkContainerName = chunkContainerName;
            
            // Check if any chunks are impassable rocks
            this.hasImpassableRocks = chunks.Exists(c => c.chunkType == "impassableLeft" || c.chunkType == "impassableRight");
        }
    }

    public void SaveMapToJson(string saveFilePath)
    {
        //kiểm tra script nào đã gọi
        var stackFrame = new System.Diagnostics.StackTrace().GetFrame(1);
        string callingScript = stackFrame.GetMethod().DeclaringType.Name;
        string methodName = stackFrame.GetMethod().Name;
        
        if (saveFilePath == null)
        {
            Debug.LogError($"{callingScript}.{methodName}: saveFilePath trống");
        }
        MapSaveData mapData = new MapSaveData(
            seed,
            currentChunkLeftId,
            currentChunkRightId,
            chunkList,
            EntitySaveManager.Instance.chunkContainer != null ? EntitySaveManager.Instance.chunkContainer.name : "UnnamedChunkContainer"
        );

        string jsonData = JsonConvert.SerializeObject(mapData, Formatting.Indented);
        string mapFilePath = Path.Combine(saveFilePath, "map.json");
        File.WriteAllText(mapFilePath, jsonData);
        
      
        Debug.Log($" Đã lưu map vào: {mapFilePath}");
    }

    public void LoadMapFromJson(string mapFilePath)
    {
        if (!File.Exists(mapFilePath))
        {
            Debug.LogWarning("Không tìm thấy file map!");
            return;
        }

        try
        {
            string jsonData = File.ReadAllText(mapFilePath);
            MapSaveData mapData = JsonConvert.DeserializeObject<MapSaveData>(jsonData);

            if (mapData == null)
            {
                Debug.LogError("Không thể đọc dữ liệu từ map.json. File có thể bị hỏng.");
                return;
            }

            // Cập nhật các giá trị từ mapData
            seed = mapData.seed;
            currentChunkLeftId = mapData.currentChunkLeftId;
            currentChunkRightId = mapData.currentChunkRightId;

            // Xóa map hiện tại
            ClearCurrentMap();

            // Khởi tạo Random với seed đã lưu
            rng = new System.Random(seed);

            // Restore chunk ID bounds ngay sau ClearCurrentMap (ClearCurrentMap reset chúng về 0!)
            currentChunkLeftId  = mapData.currentChunkLeftId;
            currentChunkRightId = mapData.currentChunkRightId;

            // Ensure the necessary prefabs are loaded
            if (chunkPrefabsList.Count == 0)
            {
                LoadGroundChunks();
            }
            
            if (impassableRockPrefabs.Count == 0)
            {
                LoadImpassableRocks();
            }

            // Check if emptyChunk is assigned
            if (emptyChunk == null)
            {
                Debug.LogError("emptyChunk prefabWall is not assigned! Cannot load map.");
                return;
            }

            // Ensure chunkList is initialized
            if (chunkList == null)
            {
                chunkList = new List<Chunk>();
            }

            // Ensure EntitySaveManager instance exists
            if (EntitySaveManager.Instance == null || EntitySaveManager.Instance.chunkContainer == null)
            {
                Debug.LogError("EntitySaveManager.Instance or chunkContainer is null. Cannot instantiate chunks.");
                return;
            }

            // Tạo lại các chunk từ dữ liệu đã lưu
            foreach (var chunk in mapData.chunks)
            {
                Vector3 position = new Vector3(chunk.positionX, 0, 0);
                GameObject prefabToUse = emptyChunk; // Default to empty chunk
                
                // Determine which prefabWall to use based on chunk type (if available)
                if (chunk.chunkType != null)
                {
                    switch (chunk.chunkType)
                    {
                        case "impassableLeft":
                            if (impassableRockLeft != null)
                            {
                                prefabToUse = impassableRockLeft;
                                //Debug.Log("Restoring ImpassableRockLeft at " + position);
                            }
                            break;
                            
                        case "impassableRight":
                            if (impassableRockRight != null)
                            {
                                prefabToUse = impassableRockRight;
                               // Debug.Log("Restoring ImpassableRockRight at " + position);
                            }
                            break;
                            
                        default: // "ground" or null
                            prefabToUse = emptyChunk;
                            break;
                    }
                }

                GameObject newChunkObject = Instantiate(prefabToUse, position, Quaternion.identity, 
                    EntitySaveManager.Instance.chunkContainer.transform);
                
                if (newChunkObject != null)
                {
                    // [BUGFIX] Remove any pre-planted trees/bushes from the instantiated chunk 
                    // so we don't duplicate objects that will be loaded by EntitySaveManager
                    Transform[] childTransforms = newChunkObject.GetComponentsInChildren<Transform>(true);
                    foreach (Transform child in childTransforms)
                    {
                        if (child != null && child.gameObject != newChunkObject)
                        {
                            if (child.CompareTag(Script.Utils.GameTags.Tree))
                            {
                                Destroy(child.gameObject);
                            }
                        }
                    }

                    EntitySaveManager.Instance.MoveChildrenToContainers(newChunkObject);
                }
                else
                {
                    Debug.LogError($"Failed to instantiate chunk at position {position}");
                }
            }

            chunkList = mapData.chunks;
            chunkList.Sort((a, b) => a.positionX.CompareTo(b.positionX));
            
            Debug.Log($"Loaded map with {chunkList.Count} chunks from JSON file");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error loading map from JSON: {ex.Message}\n{ex.StackTrace}");
        }
    }

    public void test()
    {

    }
}
}