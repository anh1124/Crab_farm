using UnityEngine;
using Script.Manager.Building;
using Script.Data_Model.Status;

namespace Script.Manager
{
    /// <summary>
    /// Singleton quản lý việc tạo và reset guide object dẫn đường đến TownCenter.
    /// Sử dụng object pooling để tái sử dụng thay vì instantiate/destroy liên tục.
    /// Điểu khiển toàn bộ logic movement, rotation, animation của guide.
    /// </summary>
    public class ArrowGuideManager : MonoBehaviour
    {
        public static ArrowGuideManager Instance { get; private set; }

        [Header("Prefabs")]
        [Tooltip("Prefab chứa sprite (vỏ rỗng, chỉ cần SpriteRenderer)")]
        [SerializeField] private GameObject guidePrefab;

        [Header("Sprites")]
        [Tooltip("Sprite mũi tên (trạng thái dẫn đường)")]
        [SerializeField] private Sprite arrowSprite;
        [Tooltip("Sprite đồng xu (trạng thái đã đến đích, có thể nâng cấp)")]
        [SerializeField] private Sprite coinSprite;
        [Tooltip("Sprite X (trạng thái max cấp, không thể nâng cấp)")]
        [SerializeField] private Sprite xSprite;

        [Header("Spawn Offset")]
        [Tooltip("Offset từ pivot của Player khi spawn guide")]
        [SerializeField] private Vector3 spawnOffset = new Vector3(0f, 8f, 0f);

        [Header("Guide Scale")]
        [Tooltip("Scale của Guide Arrow(Clone)")]
        [SerializeField] private Vector3 guideScale = Vector3.one;

        [Header("Lifetime")]
        [Tooltip("Thời gian tồn tại của guide (giây)")]
        [SerializeField] private float lifetime = 5f;

        [Header("Movement")]
        [Tooltip("Tốc độ bay của guide")]
        [SerializeField] private float moveSpeed = 8f;
        [Tooltip("Tốc độ xoay của guide")]
        [SerializeField] private float rotationSpeed = 10f;
        [Tooltip("Khoảng cách để xác nhận đã đến đích")]
        [SerializeField] private float reachThreshold = 0.5f;

        [Header("Shake Animation")]
        [Tooltip("Độ mạnh của shake khi hiển thị Coin")]
        [SerializeField] private float shakeIntensity = 0.1f;
        [Tooltip("Tốc độ shake")]
        [SerializeField] private float shakeSpeed = 10f;

        [Header("Pulse Animation")]
        [Tooltip("Tốc độ pulse animation")]
        [SerializeField] private float pulseSpeed = 2f;
        [Tooltip("Scale tối đa khi pulse")]
        [SerializeField] private float pulseMaxScale = 1.2f;

        [Header("Sprite Direction")]
        [Tooltip("Hướng mặc định của sprite arrow")]
        [SerializeField] private SpriteDirection defaultSpriteDirection = SpriteDirection.Up;

        public enum SpriteDirection { Up, Right, Down, Left }

        // Runtime variables
        private GameObject guideObject;
        private SpriteRenderer guideSpriteRenderer;
        private Transform target;
        private Transform player;
        private float currentLifetime;
        private bool canUpgrade;
        private bool targetInCameraView = false;
        private bool reachedTarget = false;
        private bool isMaxTier = false;
        private Vector3 followOffset;
        private Vector3 baseScale;

        private BuildingManager _buildingManager;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Debug.LogWarning("[ArrowGuideManager] Instance already exists, destroying duplicate.");
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            _buildingManager = FindObjectOfType<BuildingManager>();

            // Nếu đây là game mới (Tier 0), hiện dẫn đường 1 lần sau 10s để người chơi biết cần đi về đâu
            StartCoroutine(ShowArrowWithDelayCoroutine(10f));
        }

        private System.Collections.IEnumerator ShowArrowWithDelayCoroutine(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (Utility.GetTownCenterTier() == 0)
            {
                ShowArrow();
            }
        }

        private void Update()
        {
            if (guideObject == null || !guideObject.activeInHierarchy) return;

            UpdateGuideLogic();
        }

        /// <summary>
        /// Gọi từ BuildMenu khi muốn hiển thị dẫn đường.
        /// Tự động hủy arrow cũ nếu có và tạo arrow mới.
        /// </summary>
        public void ShowArrow()
        {
            if (guidePrefab == null)
            {
                Debug.LogError("[ArrowGuideManager] Guide prefab is not assigned!");
                return;
            }

            // Lấy TownCenter từ BuildingManager
            if (_buildingManager == null || _buildingManager.currentTownCenter == null)
            {
                Debug.LogWarning("[ArrowGuideManager] Cannot show arrow - TownCenter not available.");
                return;
            }

            Transform target = _buildingManager.currentTownCenter.transform;
            Transform player = GetPlayerTransform();

            if (player == null)
            {
                Debug.LogWarning("[ArrowGuideManager] Cannot show arrow - Player not found.");
                return;
            }

            // Khởi tạo guide instance nếu chưa có (object pooling)
            if (guideSpriteRenderer == null)
            {
                InitializeGuideInstance();
            }

            if (guideSpriteRenderer == null)
            {
                Debug.LogError("[ArrowGuideManager] Failed to initialize guide instance!");
                return;
            }

            // Tính toán vị trí spawn với offset từ player pivot
            Vector3 spawnPosition = player.position + spawnOffset;

            // Đặt vị trí, scale và kích hoạt guide
            guideObject.transform.position = spawnPosition;
            guideObject.transform.localScale = guideScale;
            guideObject.SetActive(true);

            // Reset runtime variables
            this.target = target;
            this.player = player;
            this.canUpgrade = CanTownCenterBeUpgraded();
            this.currentLifetime = lifetime;
            this.isMaxTier = !this.canUpgrade;
            this.followOffset = spawnOffset;
            this.reachedTarget = false;
            this.targetInCameraView = false;
            this.baseScale = guideScale;

            // Thiết lập sprite ban đầu
            if (isMaxTier)
            {
                SetSprite(xSprite);
            }
            else
            {
                SetSprite(arrowSprite);
            }
        }

        /// <summary>
        /// Kiểm tra xem TownCenter hiện tại có thể nâng cấp không.
        /// Dùng logic giống TownCenterBehavior.CanReceiveCoins() - kiểm tra TownCenterDataList.
        /// </summary>
        private bool CanTownCenterBeUpgraded()
        {
            if (_buildingManager == null || _buildingManager.currentTownCenter == null)
                return false;

            var townCenterStatus = _buildingManager.currentTownCenter.GetComponent<TownCenterStatus>();
            if (townCenterStatus == null)
                return false;

            // Dùng logic TownCenterBehavior: kiểm tra TownCenterDataList có tier tiếp theo không
            var townCenterDataList = Script.Game.Structures.TownCenter.TownCenterDataList.Instance;
            if (townCenterDataList == null) return false;

            return townCenterDataList.TryGetByTier(townCenterStatus.tier + 1, out _, logIfMissing: false);
        }

        /// <summary>
        /// Lấy Transform của Player từ PlayerController.
        /// </summary>
        private Transform GetPlayerTransform()
        {
            var playerController = Controller.PlayerController.PlayerController.Instance;
            if (playerController != null && playerController.player != null)
            {
                return playerController.player.transform;
            }
            return null;
        }

        /// <summary>
        /// Khởi tạo guide instance (chỉ gọi 1 lần duy nhất để pooling).
        /// </summary>
        private void InitializeGuideInstance()
        {
            if (guidePrefab == null) return;

            guideObject = Instantiate(guidePrefab);
            guideObject.SetActive(false);
            guideSpriteRenderer = guideObject.GetComponent<SpriteRenderer>();

            if (guideSpriteRenderer == null)
            {
                Debug.LogError("[ArrowGuideManager] Guide prefab does not have SpriteRenderer component!");
                Destroy(guideObject);
                guideObject = null;
            }
            else
            {
                Debug.Log("[ArrowGuideManager] Guide instance initialized for pooling.");
            }
        }

        /// <summary>
        /// Ẩn guide hiện tại (set inactive để tái sử dụng, không destroy).
        /// </summary>
        public void HideGuide()
        {
            if (guideObject != null)
            {
                guideObject.SetActive(false);
            }
        }

        /// <summary>
        /// Log debug info đầy đủ. Click chuột phải component trong Inspector để gọi.
        /// </summary>
        [ContextMenu("Log Debug Context")]
        public void LogDebugContext()
        {
            // Inspector values
            string prefabName = guidePrefab != null ? guidePrefab.name : "NULL";

            // Runtime values
            var player = GetPlayerTransform();
            var target = _buildingManager?.currentTownCenter?.transform;
            var sprite = guideSpriteRenderer?.sprite;
            bool inCam = target != null && Camera.main != null &&
                (Camera.main.WorldToViewportPoint(target.position)).x is > 0 and < 1 &&
                (Camera.main.WorldToViewportPoint(target.position)).y is > 0 and < 1;

            // Check upgrade/build status
            bool canUpgradeTC = CanTownCenterBeUpgraded();
            int availableBuildings = GetAvailableBuildingCount();

            // Get TownCenter tier info for debug
            var tcStatus = _buildingManager?.currentTownCenter?.GetComponent<TownCenterStatus>();
            int currentTier = tcStatus != null ? tcStatus.tier : -1;
            int maxTier = _buildingManager != null ? _buildingManager.GetMaxAllowedTier(BuildingType.TownCenter) : -1;

            Debug.Log(
                $"[GuideDebug]\n" +
                $"  Prefab: {prefabName}\n" +
                $"  Spawn Offset: {spawnOffset}\n" +
                $"  ---\n" +
                $"  Guide Active: {guideObject?.activeInHierarchy}\n" +
                $"  Guide Position: {guideObject?.transform.position}\n" +
                $"  ---\n" +
                $"  Player Pivot: {player?.position}\n" +
                $"  TownCenter Pivot: {target?.position}\n" +
                $"  TownCenter In Camera: {inCam}\n" +
                $"  ---\n" +
                $"  TownCenter Tier: {currentTier} / Max: {maxTier}\n" +
                $"  Can Upgrade TownCenter: {canUpgradeTC}\n" +
                $"  Available Buildings: {availableBuildings}\n" +
                $"  ---\n" +
                $"  Current Sprite: {sprite?.name}"
            );
        }

        /// <summary>
        /// Đếm số lượng công trình có thể xây hiện tại.
        /// </summary>
        private int GetAvailableBuildingCount()
        {
            if (_buildingManager == null || _buildingManager.currentTownCenter == null) return 0;

            var townCenterStatus = _buildingManager.currentTownCenter.GetComponent<TownCenterStatus>();
            if (townCenterStatus == null) return 0;

            int currentTier = townCenterStatus.tier;
            if (currentTier < 0 || currentTier >= _buildingManager.TownCenterConfigs?.Count) return 0;

            var config = _buildingManager.TownCenterConfigs[currentTier];
            if (config?.buildAvailableStructuresList == null) return 0;

            int count = 0;
            foreach (var structure in config.buildAvailableStructuresList)
            {
                int built = _buildingManager.CountBuiltBuildings(structure.buildingType);
                if (built < structure.maxQuantity)
                    count++;
            }
            return count;
        }

        /// <summary>
        /// Reset guide instance (gọi khi cần tạo mới hoàn toàn, ví dụ scene change).
        /// </summary>
        public void ResetGuideInstance()
        {
            if (guideObject != null)
            {
                Destroy(guideObject);
                guideObject = null;
                guideSpriteRenderer = null;
            }
            InitializeGuideInstance();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            // Cleanup khi destroy
            if (guideObject != null)
            {
                Destroy(guideObject);
            }
        }

        #region Guide Logic Methods

        private void UpdateGuideLogic()
        {
            if (reachedTarget && !isMaxTier) return;

            // Đếm ngược lifetime
            currentLifetime -= Time.deltaTime;
            if (currentLifetime <= 0f)
            {
                guideObject.SetActive(false);
                return;
            }

            // Nếu là max tier, chỉ giữ trên đầu player và hiện X
            if (isMaxTier)
            {
                KeepAbovePlayer();
                return;
            }

            // Kiểm tra target có trong camera view không
            targetInCameraView = IsTargetInCameraView();

            if (!targetInCameraView)
            {
                // Trạng thái A: Ở trên đầu player, xoay về phía target
                KeepAbovePlayer();
                RotateTowardsTarget();
            }
            else
            {
                // Trạng thái B: Bay về phía target
                MoveTowardsTarget();
                RotateTowardsTarget();

                // Kiểm tra đã đến gần target chưa
                float distanceToTarget = Vector3.Distance(guideObject.transform.position, target.position);
                if (distanceToTarget < reachThreshold && !reachedTarget)
                {
                    reachedTarget = true;
                    ShowCoinAtTarget();
                }
            }

            // Animations
            HandleShakeAnimation();
            HandlePulseAnimation();
        }

        private void KeepAbovePlayer()
        {
            if (player != null)
            {
                guideObject.transform.position = player.position + followOffset;
            }
        }

        private void RotateTowardsTarget()
        {
            if (target == null) return;

            Vector3 direction = (target.position - guideObject.transform.position).normalized;
            if (direction != Vector3.zero)
            {
                float baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                float offset = GetDirectionOffset();
                float angle = baseAngle + offset;
                Quaternion targetRotation = Quaternion.Euler(0, 0, angle);
                guideObject.transform.rotation = Quaternion.Slerp(guideObject.transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }

        private float GetDirectionOffset()
        {
            switch (defaultSpriteDirection)
            {
                case SpriteDirection.Up: return -90f;
                case SpriteDirection.Right: return 0f;
                case SpriteDirection.Down: return 90f;
                case SpriteDirection.Left: return 180f;
                default: return -90f;
            }
        }

        private void MoveTowardsTarget()
        {
            if (target == null) return;
            guideObject.transform.position = Vector3.MoveTowards(guideObject.transform.position, target.position, moveSpeed * Time.deltaTime);
        }

        private bool IsTargetInCameraView()
        {
            if (target == null || Camera.main == null) return false;
            Vector3 viewportPoint = Camera.main.WorldToViewportPoint(target.position);
            return viewportPoint.x > 0 && viewportPoint.x < 1 && viewportPoint.y > 0 && viewportPoint.y < 1 && viewportPoint.z > 0;
        }

        private void ShowCoinAtTarget()
        {
            SetSprite(coinSprite);
            if (target != null)
            {
                guideObject.transform.position = target.position + followOffset;
                guideObject.transform.rotation = Quaternion.identity;
            }
        }

        private void HandleShakeAnimation()
        {
            if (!reachedTarget || guideSpriteRenderer == null || guideSpriteRenderer.sprite != coinSprite) return;
            float shakeX = Mathf.Sin(Time.time * shakeSpeed) * shakeIntensity;
            float shakeY = Mathf.Cos(Time.time * shakeSpeed * 0.5f) * shakeIntensity;
            guideObject.transform.position += new Vector3(shakeX, shakeY, 0) * Time.deltaTime;
        }

        private void HandlePulseAnimation()
        {
            if (baseScale == Vector3.zero) return;
            float pulse = Mathf.Sin(Time.time * pulseSpeed) * 0.5f + 0.5f;
            float scaleMultiplier = 1f + (pulseMaxScale - 1f) * pulse;
            guideObject.transform.localScale = baseScale * scaleMultiplier;
        }

        private void SetSprite(Sprite newSprite)
        {
            if (guideSpriteRenderer != null && newSprite != null)
            {
                guideSpriteRenderer.sprite = newSprite;
            }
        }

        #endregion
    }
}
