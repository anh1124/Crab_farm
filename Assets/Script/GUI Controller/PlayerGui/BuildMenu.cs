using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

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

    public static BuildMenu Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
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
    
    public void OpenBuildMenu()
    {
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
        // Cleanup khi script bị disable
        if (isBuilding)
        {
            BuildingPlacementManager.Instance.CancelPlacement();
            ResetBuildState();
        }
    }

    public void ResetShowBuildMenuState()
    {
        showBuildMenu = false;
    }
}