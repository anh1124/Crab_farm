using System;
using System.Collections;
using System.Collections.Generic;
using Script.Data_Model;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class BuildMenu : MonoBehaviour
{
    [FormerlySerializedAs("confirmBuild")]
    [Header("Confirm Button")]
    [Space(10)]  
    public Button confirmBuildBtn;

    public Button cancelBuildBtn;
    public Button openBuildMenuBtn;
    
    //khi click vào 1 trong các nút build bên dưới ,
    //dùng hàm ẩn toàn bộ các nút công trình đi,hiện nút confirmBuild ra
    //đồng thời cho hiện buildGameObject
    
    
    [Header("Build Structure Buttons")]
    [Space(10)]  
    public Button buildWallBtn;
    //mỗi wall phải cách nhau nửa chunk
    //cách cây 1/4 chunk
    //không được xây đè lên công trình khác 
    public Button buildTownCenterBtn;  //nút này sẽ hiện lên nếu TownCenter chưa được xây(chỉ được xây 1 cái)
    public Button buildFarm;//chỉ được xây trên suối nhỏ được gen từ trong map
    public Button buildTower;
    //mỗi tower cách nhau nửa chunk
    //cách cây 1/4 chunk

    [Header("Structure Prefabs")]
    public GameObject wall;
    public GameObject unlitCampfire;
    public GameObject farm;
    public GameObject tower;
    
    [Header("Layer Settings")]
    [SerializeField] private int previewSortingOrder = 10; // Đảm bảo preview hiển thị trên các layer khác
    
    private GameObject currentPreview;   // Instance của preview
    private GameObject structureToSpawn; // Prefab được chọn để xây dựng
    private bool isBuilding = false;
    public GameObject player;
    
    private void Start()
    {
        InitializePlayer();
        SetupButtonListeners();
        HideAllBuildButtons();
    }
       private void InitializePlayer()
    {
        player = PlayerController.Instance?.player;
        if (player == null)
        {
            Debug.LogError("Player not found in BuildMenu!");
        }
    }

    private void Update()
    {
        if (player == null)
        {
            player = PlayerController.Instance.player;
        }
        
        if (isBuilding && currentPreview != null)
        {
            // Preview theo vị trí chuột thay vì vị trí player
            Vector2 PlayerPosition = new Vector2(player.transform.position.x, 0);
            currentPreview.transform.position = PlayerPosition;
        }
    }

    private void SetupButtonListeners()
    {
        cancelBuildBtn.onClick.AddListener(OnCancelBuildClicked);
        openBuildMenuBtn.onClick.AddListener(OpenBuildMenu);
        confirmBuildBtn.onClick.AddListener(OnConfirmBuildClicked);
        buildWallBtn.onClick.AddListener(() => StartBuildingPreview(wall));
        buildTownCenterBtn.onClick.AddListener(() => StartBuildingPreview(unlitCampfire));
        buildFarm.onClick.AddListener(() => StartBuildingPreview(farm));
        buildTower.onClick.AddListener(() => StartBuildingPreview(tower));
    }

    public void OpenBuildMenu()
    {
        ShowBuildButtons();
    }
    private void StartBuildingPreview(GameObject structurePrefab)
    {
        CleanupCurrentPreview();

        structureToSpawn = structurePrefab;
        currentPreview = Instantiate(structurePrefab, Vector3.zero, Quaternion.identity);

        SetupPreviewVisuals(currentPreview);

        isBuilding = true;
        HideAllBuildButtons();
        confirmBuildBtn.gameObject.SetActive(true);
        cancelBuildBtn.gameObject.SetActive(true); 
    }
    private void CleanupCurrentPreview()
    {
        if (currentPreview != null)
        {
            Destroy(currentPreview);
            currentPreview = null;
        }
    }
    private void SetupPreviewVisuals(GameObject preview)
    {
        SpriteRenderer[] renderers = preview.GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer != null)
            {
                renderer.sortingOrder = previewSortingOrder;
                Color color = renderer.color;
                color.a = 0.5f; // Làm mờ
                renderer.color = color;
            }
        }
    }
    private void HideAllBuildButtons()
    {
        confirmBuildBtn.gameObject.SetActive(false);
        cancelBuildBtn.gameObject.SetActive(false);
        buildWallBtn.gameObject.SetActive(false);
        buildTownCenterBtn.gameObject.SetActive(false);
        buildFarm.gameObject.SetActive(false);
        buildTower.gameObject.SetActive(false);
    }
    private void ShowBuildButtons()
    {
        confirmBuildBtn.gameObject.SetActive(true);
        cancelBuildBtn.gameObject.SetActive(false);
        buildWallBtn.gameObject.SetActive(true);
        buildTownCenterBtn.gameObject.SetActive(true);
        buildFarm.gameObject.SetActive(true);
        buildTower.gameObject.SetActive(true);
    }
    private void OnConfirmBuildClicked()
    {
        if (currentPreview != null && structureToSpawn != null)
        {
            Vector3 finalPosition = currentPreview.transform.position;
            CleanupCurrentPreview();

            // Tạo công trình thật
            Instantiate(structureToSpawn, finalPosition, Quaternion.identity);

            ResetBuildState();
        }
    }
    private void OnCancelBuildClicked()
    {
        CleanupCurrentPreview(); // Xóa preview
        ResetBuildState();
    }
    private void ResetBuildState()
    {
        structureToSpawn = null;
        isBuilding = false;
        ShowBuildButtons();
        confirmBuildBtn.gameObject.SetActive(false);
        cancelBuildBtn.gameObject.SetActive(false);
    }
  
}
