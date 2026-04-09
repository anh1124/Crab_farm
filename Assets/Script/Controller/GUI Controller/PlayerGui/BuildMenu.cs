using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Script.Controller.PlayerController;
using Script.Game.Other;
using Script.Data_Model;
using Script.Data_Model.Status;
using Script.Entities.Structures.Wall_And_Stump.Wall;
using Script.Game.Structures.Wall_And_Stump;
using Script.Manager.Building;

public class BuildMenu : MonoBehaviour
{
    [Header("Confirm/Cancel Button Prefabs")]
    public Button confirmBuildBtn;
    public Button cancelBuildBtn;

    [Header("Main Menu Button")]
    public Button openBuildMenuBtn;

    [Header("Dynamic Build Buttons Settings")]
    [Tooltip("Danh sách các prefabWall có thể xây dựng")]
    public List<GameObject> buildableStructures;
    
    [Tooltip("Prefab của nút build")]
    public Button buildButtonPrefab;
     
    [Tooltip("Container chứa các nút build")]
    public Transform buildButtonContainer;

    private bool isBuilding = false;
    public GameObject player;
    public bool showBuildMenu = false;

    // Tham chiếu đến các nút confirm/cancel đã instance
    private Button currentConfirmButton;
    private Button currentCancelButton;

    public static BuildMenu instance;

    private BuildingManager _bm;
    private BuildingManager BM => _bm != null ? _bm : (_bm = FindObjectOfType<BuildingManager>());

    private BuildingPlacementManager _bpm;
    private BuildingPlacementManager BPM => _bpm != null ? _bpm : (_bpm = FindObjectOfType<BuildingPlacementManager>());

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        SetupButtonListeners();
        ClearBuildButtonContainer();
    }
    
    public void InitializePlayer()
    {
        player = PlayerController.Instance?.player;
        if (player == null)
            Debug.LogError("Player not found in BuildMenu!");
    }

    private void Update()
    {
        if (player == null)
            player = PlayerController.Instance.player;
    }

    private void SetupButtonListeners()
    {
        openBuildMenuBtn.onClick.AddListener(OpenBuildMenu);
    }

    public void ReloadBuildMenu()
    {
        ClearBuildButtonContainer();
        buildButtonContainer.gameObject.SetActive(false);
        showBuildMenu = false;
    }
    public void OpenBuildMenu()
    {
        // Kiểm tra trạng thái THỰC TẾ của UI hiện tại thay vì dùng cờ bool dễ bị lệch sync
        bool isCurrentlyOpen = buildButtonContainer.gameObject.activeSelf;

        if (isCurrentlyOpen)
        {
            // Nếu đang mở -> ĐÓNG LẠI
            ClearBuildButtonContainer();
            buildButtonContainer.gameObject.SetActive(false);
            showBuildMenu = false;
        }
        else
        {
            // Nếu đang đóng -> CẬP NHẬT DATA & MỞ LÊN
            // CHÚ Ý: Đưa hàm Update List xuống phần "Mở lên"
            // Để chắc chắn lấy data TownCenter Level MỚI NHẤT
            UpdateBuildableStructuresList();

            // SỬA LỖI: Nếu không có gì để xây -> Không bật Background trống lên!
            if (buildableStructures.Count == 0)
            {
                Debug.Log("[BuildMenu] Hiện không có công trình nào khả dụng để xây. Giữ menu đóng.");
                return;
            }

            ClearBuildButtonContainer();
            PopulateBuildButtons();
            buildButtonContainer.gameObject.SetActive(true);
            showBuildMenu = true;
        }
    }
    
    // Method to update buildable structures based on current town center tier and available building slots
    public void UpdateBuildableStructuresList()
    {
        // Clear the existing list
        buildableStructures.Clear();
        
        // Make sure BuildingManager exists
        if (BM == null)
        {
            Debug.LogError("BM is null when trying to update buildable structures");
            return;
        }
        
        // kiểm tra currentTownCenter có null không
        if (BM.currentTownCenter == null)
        {
            Debug.LogWarning("[BuildMenu] Chưa có TownCenter. Chỉ hiển thị TownCenter để xây.");
            // nếu chưa có TownCenter, chỉ thêm TownCenter vào danh sách
            buildableStructures.Add(GetBuildingPrefab(BuildingType.TownCenter));
            Debug.Log("[BuildMenu] Added TownCenter to buildable structures");
            return;
        }
        
        // Get the current town center tier and configs
        int townCenterLevel = BM.currentTownCenter.GetComponent<TownCenterStatus>().tier;
        Debug.Log($"[BuildMenu] Current TownCenter Level: {townCenterLevel}");
        
        // Check if TownCenterConfigs is loaded
        if (BM.TownCenterConfigs == null)
        {
            Debug.LogError("BM.TownCenterConfigs is NULL!");
            return;
        }
        
        // First force BuildingManager to update its available buildings
        BM.UpdateAvailableBuildings();
        if (townCenterLevel < 0 || 
            BM.TownCenterConfigs == null || 
            townCenterLevel >= BM.TownCenterConfigs.Count)
        {
            Debug.LogWarning("Invalid town center tier or configs not loaded");
            return;
        }
        
        // Get available structures for the current tier
        var config = BM.TownCenterConfigs[townCenterLevel];
        if (config == null)
        {
            Debug.LogWarning($"Config for town center tier {townCenterLevel} is null");
            return;
        }
        
        List<StructureInfo> availableStructures = config.buildAvailableStructuresList;
        if (availableStructures == null || availableStructures.Count == 0)
        {
            Debug.LogWarning($"No available structures for town center tier {townCenterLevel} (count: {availableStructures?.Count ?? 0})");
            return;
        }
        
        Debug.Log($"[BuildMenu] Found {availableStructures.Count} structure types available for tier {townCenterLevel}");
        
        foreach (var structureInfo in availableStructures)
        {
            // Count how many of this building type have already been built
            int builtCount = BM.CountBuiltBuildings(structureInfo.buildingType);
            
            // Check if we can build more of this type
            if (builtCount < structureInfo.maxQuantity)
            {
                // Get the prefabWall for this building type and add it to buildable structures
                GameObject buildingPrefab = GetBuildingPrefab(structureInfo.buildingType, structureInfo.maxTier);
                if (buildingPrefab != null)
                {
                    buildableStructures.Add(buildingPrefab);
                    Debug.Log($"Added {structureInfo.buildingType} to buildable structures (Built: {builtCount}/{structureInfo.maxQuantity})");
                }
            }
        }
        
        Debug.Log($"Updated buildable structures list. Total available buildings: {buildableStructures.Count}");
    }
    
    // Helper method to get the prefabWall for a given building type
    private GameObject GetBuildingPrefab(BuildingType buildingType, int maxTier = 0)
    {
        switch (buildingType)
        {
            case BuildingType.TownCenter:
                return PrefabData.Instance.townCenterList[0];
                
            case BuildingType.Wall:
                return PrefabData.Instance.prefabWall;
                
            case BuildingType.HammerVendor:
                return PrefabData.Instance.hammerVendor;
                
            case BuildingType.BowVendor:
                return PrefabData.Instance.bowVendor;
                
            default:
                Debug.LogWarning($"Unknown building type: {buildingType}");
                return null;
        }
    }
    
  
    private void PopulateBuildButtons()
    {
        foreach (GameObject prefab in buildableStructures)
        {
            Button btn = Instantiate(buildButtonPrefab, buildButtonContainer);
            btn.gameObject.SetActive(true);

            // Set sprite từ prefab
            Image btnImage = btn.GetComponent<Image>();
            if (btnImage != null)
            {
                // Tìm SpriteRenderer từ prefab (có thể ở root hoặc child)
                SpriteRenderer sr = prefab.GetComponent<SpriteRenderer>();
                if (sr == null)
                {
                    // Nếu không tìm thấy ở root, tìm trong children
                    sr = prefab.GetComponentInChildren<SpriteRenderer>();
                }

                if (sr != null)
                {
                    btnImage.sprite = sr.sprite;
                    // Preserve aspect ratio để tránh bị méo sprite
                    btnImage.preserveAspect = true;
                    Debug.Log($"[BuildMenu] Set sprite for {prefab.name}. Sprite: {sr.sprite?.name ?? "null"}, SpriteRenderer on: {sr.gameObject.name}");
                }
                else
                {
                    Debug.LogWarning($"[BuildMenu] No SpriteRenderer found in prefab {prefab.name}");
                }
            }
            else
            {
                Debug.LogError("[BuildMenu] Button Image component is null!");
            }
            
            // Set tên công trình
            Text btnText = btn.GetComponentInChildren<Text>();
            if (btnText != null)
                btnText.text = prefab.name;
            
            GameObject prefabCopy = prefab;
            btn.onClick.AddListener(() =>
            {
                ClearBuildButtonContainer();
                StartBuildingPreview(prefabCopy);
            });
        }
    }
    
    private void ClearDynamicBuildButtons()
    {
        // Xóa tất cả nút con trừ confirm/cancel buttons (nếu có)
        foreach (Transform child in buildButtonContainer)
        {
            if (child.gameObject != currentConfirmButton?.gameObject && 
                child.gameObject != currentCancelButton?.gameObject)
            {
                Destroy(child.gameObject);
            }
        }
    }
    
    private void ClearBuildButtonContainer()
    {
        // Xóa tham chiếu đến các nút confirm/cancel
        currentConfirmButton = null;
        currentCancelButton = null;

        // Xóa tất cả game object con
        foreach (Transform child in buildButtonContainer)
        {
            Destroy(child.gameObject);
        }
    }

    private void StartBuildingPreview(GameObject structurePrefab)
    {
        BPM.StartPlacement(structurePrefab);
        isBuilding = true;

        ClearBuildButtonContainer();
        
        // Tạo và lưu tham chiếu đến các nút confirm/cancel
        currentConfirmButton = Instantiate(confirmBuildBtn, buildButtonContainer);
        currentCancelButton = Instantiate(cancelBuildBtn, buildButtonContainer);

        // Đảm bảo các nút được active
        currentConfirmButton.gameObject.SetActive(true);
        currentCancelButton.gameObject.SetActive(true);

        currentConfirmButton.onClick.AddListener(OnConfirmBuildClicked);
        currentCancelButton.onClick.AddListener(OnCancelBuildClicked);
    }

    private void OnConfirmBuildClicked()
    {
        if (BPM.TryPlaceBuilding())
        {
            
            ResetBuildState();
        }
        else
        {
            Debug.Log("Vị trí đặt không hợp lệ, vui lòng điều chỉnh.");
        }
    }

    private void OnCancelBuildClicked()
    {
        BPM.CancelPlacement();
        ResetBuildState();
        showBuildMenu = false;
    }

    public void ResetBuildState()
    {
        isBuilding = false;
        ClearBuildButtonContainer();
        
        // ĐẢM BẢO ẨN LUÔN CÁI BẢNG TRỐNG NÀY ĐI SAU KHI XÂY XONG HOẶC HỦY:
        if (buildButtonContainer != null && buildButtonContainer.gameObject != null)
        {
            buildButtonContainer.gameObject.SetActive(false);
        }
    }

    private void OnDisable()
    {
        // Chỉ cleanup khi đang trong chế độ build
        if (isBuilding)
        {
            BPM?.CancelPlacement();
            ResetBuildState();
        }
    }

    public void ResetShowBuildMenuState()
    {
        showBuildMenu = false;
    }
}
