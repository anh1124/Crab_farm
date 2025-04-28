using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Script.Game.Other;
using Script.Data_Model;

public class BuildMenu : MonoBehaviour
{
    [Header("Confirm/Cancel Button Prefabs")]
    public Button confirmBuildBtn;
    public Button cancelBuildBtn;

    [Header("Main Menu Button")]
    public Button openBuildMenuBtn;

    [Header("Dynamic Build Buttons Settings")]
    [Tooltip("Danh sách các prefab có thể xây dựng")]
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
        // First update the available buildings list
        UpdateBuildableStructuresList();
        
        if (showBuildMenu)
        {
            ClearBuildButtonContainer();
            buildButtonContainer.gameObject.SetActive(false);
            showBuildMenu = false;
           
        }
        else
        {
            ClearBuildButtonContainer();
            PopulateBuildButtons();
            buildButtonContainer.gameObject.SetActive(true);
            showBuildMenu = true;
        }
    }
    
    // Method to update buildable structures based on current town center level and available building slots
    public void UpdateBuildableStructuresList()
    {
        // Clear the existing list
        buildableStructures.Clear();
        
        // Make sure BuildingManager exists
        if (BuildingManager.instance == null)
        {
            Debug.LogError("BuildingManager.instance is null when trying to update buildable structures");
            return;
        }
        
        // First force BuildingManager to update its available buildings
        BuildingManager.instance.UpdateAvailableBuildings();
        
        // Get the current town center level and configs
        int townCenterLevel = BuildingManager.instance.currentTownCenterLevel;
        if (townCenterLevel < 0 || 
            BuildingManager.instance.TownCenterConfigs == null || 
            townCenterLevel >= BuildingManager.instance.TownCenterConfigs.Count)
        {
            Debug.LogWarning("Invalid town center level or configs not loaded");
            return;
        }
        
        // Get available structures for the current level
        var config = BuildingManager.instance.TownCenterConfigs[townCenterLevel];
        if (config == null)
        {
            Debug.LogWarning($"Config for town center level {townCenterLevel} is null");
            return;
        }
        
        List<StructureInfo> availableStructures = config.availableStructures;
        if (availableStructures == null)
        {
            Debug.LogWarning($"No available structures for town center level {townCenterLevel}");
            return;
        }
        
        foreach (var structureInfo in availableStructures)
        {
            // Count how many of this building type have already been built
            int builtCount = BuildingManager.instance.CountBuiltBuildings(structureInfo.buildingType);
            
            // Check if we can build more of this type
            if (builtCount < structureInfo.maxQuantity)
            {
                // Get the prefab for this building type and add it to buildable structures
                GameObject buildingPrefab = GetBuildingPrefab(structureInfo.buildingType, structureInfo.maxLevel);
                if (buildingPrefab != null)
                {
                    buildableStructures.Add(buildingPrefab);
                    Debug.Log($"Added {structureInfo.buildingType} to buildable structures (Built: {builtCount}/{structureInfo.maxQuantity})");
                }
            }
        }
        
        Debug.Log($"Updated buildable structures list. Total available buildings: {buildableStructures.Count}");
    }
    
    // Helper method to get the prefab for a given building type
    private GameObject GetBuildingPrefab(BuildingType buildingType, int maxLevel = 0)
    {
        switch (buildingType)
        {
            case BuildingType.TownCenter:
                return PrefabData.Instance.townCenterList?.Count > 0 ? PrefabData.Instance.townCenterList[0] : null;
                
            case BuildingType.Wall:
                return GetWallPrefab();
                
            case BuildingType.HammerVendor:
                return PrefabData.Instance.hammerVendor;
                
            case BuildingType.BowVendor:
                return PrefabData.Instance.bowVendor;
                
            default:
                Debug.LogWarning($"Unknown building type: {buildingType}");
                return null;
        }
    }
    
    private GameObject GetWallPrefab()
    {
        // Wall always starts at level 1 (wallSpike1) when built from menu
        // Now using the first wall from the list (index 0)
        return PrefabData.Instance.wallList[0];
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
                SpriteRenderer sr = prefab.GetComponent<SpriteRenderer>();
                if (sr != null)
                    btnImage.sprite = sr.sprite;
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
        BuildingPlacementManager.Instance.StartPlacement(structurePrefab);
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
        if (BuildingPlacementManager.Instance.TryPlaceBuilding())
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
        BuildingPlacementManager.Instance.CancelPlacement();
        ResetBuildState();
        showBuildMenu = false;
    }

    public void ResetBuildState()
    {
        isBuilding = false;
        ClearBuildButtonContainer();
    }

    private void OnDisable()
    {
        // Chỉ cleanup khi đang trong chế độ build
        if (isBuilding)
        {
            BuildingPlacementManager.Instance?.CancelPlacement();
            ResetBuildState();
        }
    }

    public void ResetShowBuildMenuState()
    {
        showBuildMenu = false;
    }
}