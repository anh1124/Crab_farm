using System;
using System.Collections.Generic;
using System.IO;
using Script.GUI_Controller.PlayerGui;
using Newtonsoft.Json;
using Script;
using UnityEngine;

public class ChunkManager : MonoBehaviour
{
    /// <summary>
    /// có lỗi
    /// khi load chunk từ json ra thì các cái cây cũng được laod theo
    /// </summary>
    public static ChunkManager Instance { get; private set; }
    
    [System.Serializable]
    public class Chunk
    {
        public int id;  // ID để xác định vị trí trái/phải trên bản đồ
        public int prefabIndex;  // Index trong mảng chunkPrefabsList
        public float positionX;

        public Chunk(int id, int prefabIndex, float positionX)
        {
            this.id = id;
            this.prefabIndex = prefabIndex;
            this.positionX = positionX;
        }
    }

    public int seed;
    [SerializeField] private string folderPath = "Prefab/GroundChunk";
   
    public int currentCreatChunk = 15;
    public List<GameObject> chunkPrefabsList;
    public GameObject emptyChunk;//đây là chunk trống,được dùng trong save 
    //bởi vì khi các chunk khác được instance ra thì các container của các chunk đó đã được chuyển đi rồi 
    
    public List<Chunk> chunkList;

    private int currentChunkLeftId = 0;
    private int currentChunkRightId = 0;
    private int centerChunkId = 0;
    private float chunkWidth = 30f;

    private System.Random rng;
    private string savePath;

    private void Awake()
    {
        AddSingleton();
        AddChunkPrefabsFromFodel();
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

    private void AddChunkPrefabsFromFodel()
    {
        GameObject[] loadedPrefabs = Resources.LoadAll<GameObject>(folderPath);

        if (loadedPrefabs.Length == 0)
        {
            Debug.LogWarning($"Không có prefab nào được tìm thấy trong thư mục {folderPath}");
        }
        else
        {
            chunkPrefabsList.AddRange(loadedPrefabs);
            Debug.Log($"Đã load {loadedPrefabs.Length} prefab(s) thành công từ {folderPath}.");
        }
    }

    public void GenerateFirstMap(int Seed)
    {
        rng = new System.Random(Seed);
        seed = Seed;
        CreateInitialChunks(currentCreatChunk);
        SetUpContainer.Instance.SaveAllContainer();
    }

    private void CreateInitialChunks(int count)
    {
        if (count % 2 == 0) count++;
        int halfCount = count / 2;

        CreateChunk(new Vector3(0, 0, 0), centerChunkId);
        currentChunkLeftId = centerChunkId;
        currentChunkRightId = centerChunkId;

        for (int i = 1; i <= halfCount; i++)
        {
            CreateChunk(new Vector3(-i * chunkWidth, 0, 0), centerChunkId - i);
            currentChunkLeftId = centerChunkId - i;

            CreateChunk(new Vector3(i * chunkWidth, 0, 0), centerChunkId + i);
            currentChunkRightId = centerChunkId + i;
        }

        chunkList.Sort((a, b) => a.positionX.CompareTo(b.positionX));
    }

    private Chunk CreateChunk(Vector3 position, int id)
    {
        int prefabIndex = rng.Next(chunkPrefabsList.Count);
        GameObject selectedPrefab = chunkPrefabsList[prefabIndex];

        GameObject newChunkObject = Instantiate(selectedPrefab, position, Quaternion.identity, SetUpContainer.Instance.chunkContainer.transform);
        Chunk chunk = new Chunk(id, prefabIndex, position.x);
        chunkList.Add(chunk);

        SetUpContainer.Instance.MoveChildrenToContainers(newChunkObject);
        return chunk;
    }

    public void AddChunk(bool toRight)
    {
        if (chunkList.Count == 0) return;

        float newX;
        int newId;

        if (toRight)
        {
            float maxX = chunkList[chunkList.Count - 1].positionX;
            newX = maxX + chunkWidth;
            newId = currentChunkRightId + 1;
            currentChunkRightId = newId;
        }
        else
        {
            float minX = chunkList[0].positionX;
            newX = minX - chunkWidth;
            newId = currentChunkLeftId - 1;
            currentChunkLeftId = newId;
        }

        bool chunkExists = chunkList.Exists(c => Mathf.Approximately(c.positionX, newX));
        if (!chunkExists)
        {
            Vector3 newPosition = new Vector3(newX, 0, 0);
            CreateChunk(newPosition, newId);
            chunkList.Sort((a, b) => a.positionX.CompareTo(b.positionX));
        }

        SetUpContainer.Instance.SaveAllContainer();
    }

    private void ClearCurrentMap()
    {
        foreach (Transform child in SetUpContainer.Instance.chunkContainer.transform)
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

        public MapSaveData(int seed, int currentChunkLeftId, int currentChunkRightId, List<Chunk> chunks,
            string chunkContainerName)
        {
            this.seed = seed;
            this.currentChunkLeftId = currentChunkLeftId;
            this.currentChunkRightId = currentChunkRightId;
            this.chunks = chunks;
            this.chunkContainerName = chunkContainerName;
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
            SetUpContainer.Instance.chunkContainer != null ? SetUpContainer.Instance.chunkContainer.name : "UnnamedChunkContainer"
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

        string jsonData = File.ReadAllText(mapFilePath);
        MapSaveData mapData = JsonConvert.DeserializeObject<MapSaveData>(jsonData);

        // Cập nhật các giá trị từ mapData
        seed = mapData.seed;
        currentChunkLeftId = mapData.currentChunkLeftId;
        currentChunkRightId = mapData.currentChunkRightId;

        // Xóa map hiện tại
        ClearCurrentMap();

        // Khởi tạo Random với seed đã lưu
        rng = new System.Random(seed);

        // Tạo lại các chunk từ dữ liệu đã lưu
        foreach (var chunk in mapData.chunks)
        {
            Vector3 position = new Vector3(chunk.positionX, 0, 0);
            GameObject selectedPrefab = emptyChunk;//chunk được khởi tạo phải là empty vì container đã được chuyển hết các child sang container rồi 
            GameObject newChunkObject = Instantiate(selectedPrefab, position, Quaternion.identity, 
                SetUpContainer.Instance.chunkContainer.transform);
            SetUpContainer.Instance.MoveChildrenToContainers(newChunkObject);
        }

        chunkList = mapData.chunks;
        chunkList.Sort((a, b) => a.positionX.CompareTo(b.positionX));
    }

    public void test()
    {

    }
}