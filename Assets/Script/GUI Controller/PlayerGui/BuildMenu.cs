using System;
using System.Collections;
using System.Collections.Generic;
using Script;
using Script.Data_Model;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class BuildMenu : MonoBehaviour
{
    [Header("Confirm/Cancel Button Prefabs")]
    public Button confirmBuildBtn; // Prefab cho nút xác nhận
    public Button cancelBuildBtn;  // Prefab cho nút hủy xây

    [Header("Main Menu Button")]
    public Button openBuildMenuBtn;

    [Header("Dynamic Build Buttons Settings")]
    [Tooltip("Danh sách các prefab có thể xây dựng (kéo thả prefab vào đây)")]
    public List<GameObject> buildableStructures; // Danh sách prefab xây dựng

    [Tooltip("Prefab của nút build dùng để tạo động các nút build (UI Button có Image và Text)")]
    public Button buildButtonPrefab; // Prefab cho nút build
     
    [Tooltip("Container chứa các nút build (UI Panel)")]
    public Transform buildButtonContainer; // Container chứa các nút build

    [Header("Layer Settings")]
    [SerializeField] private int previewSortingOrder = 10; // Đảm bảo preview hiển thị trên các layer khác

    private GameObject currentPreview;   // Instance preview
    private GameObject structureToSpawn; // Prefab được chọn để xây dựng
    private bool isBuilding = false;
    public GameObject player;
    public bool showBuildMenu = false;

    public static BuildMenu Instance { get; private set; }

    private void Awake()
    {
        // Thiết lập singleton
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        SetupButtonListeners();
        // Khi bắt đầu, clear container và ẩn (hoặc không instance) các nút confirm/cancel
        ClearBuildButtonContainer();
    }
    
    /// <summary>
    /// Hàm InitializePlayer được gọi từ StartGameScene
    /// </summary>
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
        
        if (isBuilding && currentPreview != null)
        {
            // Cập nhật vị trí preview theo vị trí player (bạn có thể điều chỉnh theo ý muốn)
            Vector2 previewPosition = new Vector2(player.transform.position.x, player.transform.position.y);
            currentPreview.transform.position = previewPosition;
        }
    }

    private void SetupButtonListeners()
    {
        openBuildMenuBtn.onClick.AddListener(OpenBuildMenu);
        // Các nút confirm/cancel sẽ được instance vào container nên listener của chúng được gán qua prefab (nếu cần)
    }

    /// <summary>
    /// Khi ấn nút OpenBuildMenu:
    /// - Nếu menu đang mở, đóng menu (clear dynamic buttons và ẩn container).
    /// - Nếu menu đang đóng, clear dynamic buttons, ẩn (hoặc không instance) confirm/cancel, tạo mới các nút chọn công trình và hiển thị container.
    /// </summary>
    public void OpenBuildMenu()
    {
        if (showBuildMenu)
        {
            // Menu đang mở, đóng lại
            ClearDynamicBuildButtons();
            buildButtonContainer.gameObject.SetActive(false);
            showBuildMenu = false;
        }
        else
        {
            // Menu đang đóng, mở ra:
            ClearDynamicBuildButtons(); // Xóa các nút cũ (nếu có)
            // Tạo các nút xây dựng động từ danh sách prefab
            PopulateBuildButtons();
            buildButtonContainer.gameObject.SetActive(true);
            showBuildMenu = true;
        }
    }

    /// <summary>
    /// Tạo các nút xây dựng động dựa trên danh sách buildableStructures.
    /// Mỗi nút hiển thị icon (lấy từ SpriteRenderer của prefab) và tên prefab.
    /// Khi nhấn nút, sẽ gọi StartBuildingPreview.
    /// </summary>
    private void PopulateBuildButtons()
    {
        foreach (GameObject prefab in buildableStructures)
        {
            // Tạo nút mới từ buildButtonPrefab
            Button btn = Instantiate(buildButtonPrefab, buildButtonContainer);
            btn.gameObject.SetActive(true); // Đảm bảo nút được active

            // Cập nhật icon cho nút dựa trên Sprite của prefab
            Image btnImage = btn.GetComponent<Image>();
            if (btnImage != null)
            {
                SpriteRenderer sr = prefab.GetComponent<SpriteRenderer>();
                if (sr != null)
                    btnImage.sprite = sr.sprite;
            }
            
            // Nếu có Text, hiển thị tên prefab
            Text btnText = btn.GetComponentInChildren<Text>();
            if (btnText != null)
                btnText.text = prefab.name;
            
            // Sử dụng biến cục bộ để tránh closure
            GameObject prefabCopy = prefab;
            btn.onClick.AddListener(() =>
            {
                // Khi chọn công trình, clear toàn bộ nút xây dựng động trong container
                ClearDynamicBuildButtons();
                // Bắt đầu chế độ xây (preview)
                StartBuildingPreview(prefabCopy);
            });
        }
    }

    /// <summary>
    /// Xóa tất cả các nút con trong buildButtonContainer, trừ (nếu có) các nút confirm/cancel đã instance trước đó.
    /// </summary>
    private void ClearDynamicBuildButtons()
    {
        List<Transform> toDelete = new List<Transform>();
        foreach (Transform child in buildButtonContainer)
        {
            // Nếu child không phải là confirm hay cancel thì xóa
            if (child.name != confirmBuildBtn.name && child.name != cancelBuildBtn.name)
            {
                toDelete.Add(child);
            }
        }
        foreach (Transform child in toDelete)
        {
            Destroy(child.gameObject);
        }
    }

    /// <summary>
    /// Xóa toàn bộ các nút con trong buildButtonContainer.
    /// </summary>
    private void ClearBuildButtonContainer()
    {
        for (int i = buildButtonContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(buildButtonContainer.GetChild(i).gameObject);
        }
    }

    /// <summary>
    /// Khi chọn một prefab để xây, tạo preview, clear các nút xây dựng động
    /// và instance nút confirm và cancel vào buildButtonContainer.
    /// </summary>
    /// <param name="structurePrefab"></param>
    private void StartBuildingPreview(GameObject structurePrefab)
    {
        // Xóa preview cũ nếu có
        CleanupCurrentPreview();

        // Gán prefab được chọn
        structureToSpawn = structurePrefab;
        currentPreview = Instantiate(structurePrefab, Vector3.zero, Quaternion.identity);

        // Vô hiệu hóa các component không cần thiết để chỉ hiển thị hình ảnh
        DisableNonPreviewComponents(currentPreview);
        SetupPreviewVisuals(currentPreview);

        isBuilding = true;

        // Clear toàn bộ các nút con trong container (để không còn nút chọn structure)
        ClearBuildButtonContainer();
        
        // Instance nút confirm và cancel vào container
        // (Giả sử confirmBuildBtn và cancelBuildBtn được gán dưới dạng prefab)
        Button confirmInstance = Instantiate(confirmBuildBtn, buildButtonContainer);
        Button cancelInstance = Instantiate(cancelBuildBtn, buildButtonContainer);

        // (Nếu cần, có thể thiết lập lại listeners cho confirmInstance và cancelInstance)
        confirmInstance.onClick.AddListener(OnConfirmBuildClicked);
        cancelInstance.onClick.AddListener(OnCancelBuildClicked);
    }

    private void CleanupCurrentPreview()
    {
        if (currentPreview != null)
        {
            Destroy(currentPreview);
            currentPreview = null;
        }
    }

    /// <summary>
    /// Cấu hình lại hiển thị cho preview: đặt sortingOrder và giảm độ trong suốt.
    /// </summary>
    private void SetupPreviewVisuals(GameObject preview)
    {
        SpriteRenderer[] renderers = preview.GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer != null)
            {
                renderer.sortingOrder = previewSortingOrder;
                Color color = renderer.color;
                color.a = 0.5f;
                renderer.color = color;
            }
        }
    }

    /// <summary>
    /// Vô hiệu hóa các component không cần thiết để preview chỉ hiển thị hình ảnh (giữ lại Transform và SpriteRenderer).
    /// </summary>
    private void DisableNonPreviewComponents(GameObject obj)
    {
        foreach (var comp in obj.GetComponents<Component>())
        {
            if (!(comp is Transform) && !(comp is SpriteRenderer))
            {
                if (comp is Behaviour behaviour)
                    behaviour.enabled = false;
                else if (comp is Collider collider)
                    collider.enabled = false;
            }
        }
        foreach (Transform child in obj.transform)
        {
            DisableNonPreviewComponents(child.gameObject);
        }
    }

    /// <summary>
    /// Khi nhấn nút "Confirm Build", tạo đối tượng thật và chuyển vào SetUpContainer, sau đó reset trạng thái xây dựng.
    /// </summary>
    private void OnConfirmBuildClicked()
    {
        if (currentPreview != null && structureToSpawn != null)
        {
            Vector3 finalPosition = currentPreview.transform.position;
            CleanupCurrentPreview();

            // Tạo đối tượng thật và chuyển vào container qua SetUpContainer
            GameObject builtStructure = SetUpContainer.Instance.InstanceAndMoveObjectToContainer(structureToSpawn, finalPosition);
            // (Nếu cần, thực hiện thêm setup cho builtStructure)
            
            ResetBuildState();
            showBuildMenu = false;
        }
    }

    /// <summary>
    /// Khi nhấn nút "Cancel Build", hủy chế độ xây dựng, xóa preview và reset trạng thái.
    /// </summary>
    private void OnCancelBuildClicked()
    {
        CleanupCurrentPreview();
        ResetBuildState();
        showBuildMenu = false;
    }

    /// <summary>
    /// Reset trạng thái xây dựng: xóa lựa chọn, ẩn nút confirm/cancel và clear container.
    /// </summary>
    private void ResetBuildState()
    {
        structureToSpawn = null;
        isBuilding = false;
        ClearBuildButtonContainer();
       
    }
}
