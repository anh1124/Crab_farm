using UnityEngine;
using System.Collections;
using Script.Manager.Building;
using Script.Data_Model.Status;

namespace Script.Manager
{
    [RequireComponent(typeof(ArrowGuideMover))]
    public class ArrowGuideManager : MonoBehaviour
    {
        public static ArrowGuideManager Instance { get; private set; }

        [Header("Prefabs")]
        [SerializeField] private GameObject guidePrefab; // Prefab có chứa SpriteRenderer + ArrowGuideMover

        [Header("Sprites")]
        [SerializeField] private Sprite arrowSprite;
        [SerializeField] private Sprite coinSprite;
        [SerializeField] private Sprite xSprite;

        [Header("Lifetime")]
        [SerializeField] private float lifetime = 5f;

        [Header("Shake Animation (khi guide đạt target)")]
        [SerializeField] private float shakeIntensity = 0.1f;
        [SerializeField] private float shakeSpeed = 10f;

        [Header("Pulse Animation")]
        [SerializeField] private float pulseSpeed = 2f;
        [SerializeField] private float pulseMaxScale = 1.2f;

        [Header("Guide Scale")]
        [SerializeField] private Vector3 guideScale = Vector3.one;

        [Header("Movement Settings")]
        public float moveSpeed = 8f;
        public float rotationSpeed = 10f;
        public float reachThreshold = 0.5f;
        public Vector3 spawnOffsetFromPlayer = new Vector3(0f, 8f, 0f);
        public SpriteDirection defaultDirection = SpriteDirection.Up;
        public float customRotationOffset = 0f; // Bù thêm góc xoay nếu sprite vẫn bị lệch

        // Sprite direction enum (cũng dùng chung cho ArrowGuideMover)
        public enum SpriteDirection { Up, Right, Down, Left }

        // Private state
        private enum GuideState { Inactive, Following, MovingToTarget, ReachedTarget, MaxTier }
        private GuideState currentState;

        // Cached components
        private GameObject guideObject;
        private SpriteRenderer guideRenderer;
        private ArrowGuideMover mover;
        private Transform target;
        private Transform playerTransform;
        private Camera mainCamera;

        // Runtime data
        private float currentLifetime;
        private Vector3 baseScale;
        private Vector3 anchorPosition; // Dùng cho shake khi reached target

        // External references
        private BuildingManager buildingManager;

        // Coroutine helper
        private IEnumerator customMoveCoroutine;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            mainCamera = Camera.main;
            buildingManager = FindObjectOfType<BuildingManager>();
            mover = GetComponent<ArrowGuideMover>();
        }

        private void Start()
        {
            if (Utility.IsTutorialScene()) return;

            // Kiểm tra xem đã hiển thị arrow cho world này chưa
            var worldInfo = Script.Save_Controller.WorldFileManager.Instance?.worldInfo;
            if (worldInfo != null && !worldInfo.HasShowGuideArrow)
            {
                if (Utility.GetTownCenterTier() == 0)
                {
                    StartCoroutine(ShowArrowWithDelay(10f));
                }
            }
        }

        private IEnumerator ShowArrowWithDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            
            // Re-check flag sau khi delay: worldInfo có thể đã được load xong và flag được set
            // (Start() có thể chạy khi worldInfo chưa sẵn sàng)
            var wi = Script.Save_Controller.WorldFileManager.Instance?.worldInfo;
            if (wi != null && wi.HasShowGuideArrow)
            {
                Debug.Log("[ArrowGuideManager] Auto-show bị hủy: HasShowGuideArrow đã = true sau delay.");
                yield break;
            }
            
            ShowArrow();
        }

        private void Update()
        {
            if (currentState == GuideState.Inactive || guideObject == null) return;

            // Lifetime
            currentLifetime -= Time.deltaTime;
            if (currentLifetime <= 0f)
            {
                HideGuide();
                return;
            }

            // Xử lý theo state (gọi mover)
            switch (currentState)
            {
                case GuideState.Following:
                    mover.UpdateMovement(); // mover tự follow player
                    break;
                case GuideState.MovingToTarget:
                    mover.UpdateMovement();
                    if (mover.HasReachedTarget())
                        SwitchToReachedTarget();
                    break;
                case GuideState.ReachedTarget:
                    // Không di chuyển nữa, chỉ chờ animation
                    break;
                case GuideState.MaxTier:
                    mover.UpdateMovement(); // vẫn follow nhưng hiển thị X sprite
                    break;
            }

            // Animation chung (shake, pulse)
            HandleShakeAnimation();
            HandlePulseAnimation();
        }

        // ========== PUBLIC API ==========
        public void ShowArrow()
        {
            if (guidePrefab == null || buildingManager?.currentTownCenter == null) return;
            Transform townCenter = buildingManager.currentTownCenter.transform;
            Transform player = GetPlayerTransform();
            if (player == null) return;

            InitializeGuideIfNeeded();
            if (guideObject == null) return;

            guideObject.transform.localScale = guideScale;
            baseScale = guideScale;
            currentLifetime = lifetime;
            target = townCenter;
            playerTransform = player;
            guideObject.SetActive(true);

            bool canUpgrade = CanTownCenterBeUpgraded();
            if (!canUpgrade)
            {
                SetSprite(xSprite);
                currentState = GuideState.MaxTier;
                mover.Initialize(player, target, false); // follow player
            }
            else
            {
                SetSprite(arrowSprite);
                bool inView = IsTargetInCameraView();
                currentState = inView ? GuideState.MovingToTarget : GuideState.Following;
                mover.Initialize(player, target, currentState == GuideState.MovingToTarget);
            }

            StopCustomMoveCoroutine();

            // Đánh dấu đã hiển thị arrow và lưu lại vào worldInfo
            var worldInfo = Script.Save_Controller.WorldFileManager.Instance?.worldInfo;
            if (worldInfo != null && !worldInfo.HasShowGuideArrow)
            {
                worldInfo.HasShowGuideArrow = true;
                Script.Save_Controller.WorldFileManager.Instance.SaveCurrentWorldInfo();
                Debug.Log("[ArrowGuideManager] First time arrow shown. Flag saved to worldInfo.");
            }
        }

        public void ShowArrowToTarget(Vector3 targetWorldPos)
        {
            Transform player = GetPlayerTransform();
            if (player == null) return;
            InitializeGuideIfNeeded();
            if (guideObject == null) return;

            GameObject tempTarget = new GameObject("TempArrowTarget");
            tempTarget.transform.position = targetWorldPos;
            target = tempTarget.transform;
            playerTransform = player;

            guideObject.transform.position = player.position + spawnOffsetFromPlayer;
            guideObject.transform.localScale = guideScale;
            guideObject.SetActive(true);
            baseScale = guideScale;
            currentLifetime = lifetime;
            SetSprite(arrowSprite);
            currentState = GuideState.MovingToTarget;
            mover.Initialize(player, target, true);

            StopCustomMoveCoroutine();
            customMoveCoroutine = CleanupTempTarget(tempTarget);
            StartCoroutine(customMoveCoroutine);
        }

        public void SetTarget(Transform newTarget)
        {
            if (newTarget == null) return;
            InitializeGuideIfNeeded();
            if (guideObject == null) return;

            target = newTarget;
            playerTransform = GetPlayerTransform();
            if (!guideObject.activeInHierarchy)
            {
                guideObject.transform.position = playerTransform.position + spawnOffsetFromPlayer;
                guideObject.SetActive(true);
            }
            currentState = IsTargetInCameraView() ? GuideState.MovingToTarget : GuideState.Following;
            SetSprite(arrowSprite);
            currentLifetime = lifetime;
            mover.SetTarget(playerTransform, target, currentState == GuideState.MovingToTarget);
            StopCustomMoveCoroutine();
        }

        public void HideGuide()
        {
            if (guideObject != null) guideObject.SetActive(false);
            currentState = GuideState.Inactive;
            StopCustomMoveCoroutine();
        }

        public void SetInfiniteLifetime(bool infinite)
        {
            currentLifetime = infinite ? float.PositiveInfinity : lifetime;
        }

        public void SetTargetPosition(Vector3 worldPosition)
        {
            GameObject temp = new GameObject("TempArrowTarget");
            temp.transform.position = worldPosition;
            SetTarget(temp.transform);
            StopCustomMoveCoroutine();
            customMoveCoroutine = CleanupTempTarget(temp);
            StartCoroutine(customMoveCoroutine);
        }

        // ========== PRIVATE METHODS ==========
        private void InitializeGuideIfNeeded()
        {
            if (guideObject == null && guidePrefab != null)
            {
                guideObject = Instantiate(guidePrefab);
                guideObject.SetActive(false);
                guideRenderer = guideObject.GetComponent<SpriteRenderer>();
                
                // Gán transform của mũi tên cho mover xử lý
                if (mover != null)
                {
                    mover.SetObjectToMove(guideObject.transform);
                }

                if (guideRenderer == null)
                {
                    Debug.LogError($"[ArrowGuideManager] Guide Prefab '{guidePrefab.name}' thiếu SpriteRenderer!");
                    Destroy(guideObject);
                    guideObject = null;
                }
            }
        }

        private Transform GetPlayerTransform()
        {
            if (playerTransform == null)
            {
                var pc = Controller.PlayerController.PlayerController.Instance;
                if (pc?.player != null)
                    playerTransform = pc.player.transform;
            }
            return playerTransform;
        }

        private bool CanTownCenterBeUpgraded()
        {
            if (buildingManager?.currentTownCenter == null) return false;
            var status = buildingManager.currentTownCenter.GetComponent<TownCenterStatus>();
            if (status == null) return false;
            var dataList = Script.Game.Structures.TownCenter.TownCenterDataList.Instance;
            return dataList != null && dataList.TryGetByTier(status.tier + 1, out _, false);
        }

        private bool IsTargetInCameraView()
        {
            if (target == null || mainCamera == null) return false;
            Vector3 viewPos = mainCamera.WorldToViewportPoint(target.position);
            return viewPos.x > 0 && viewPos.x < 1 && viewPos.y > 0 && viewPos.y < 1 && viewPos.z > 0;
        }

        private void SwitchToReachedTarget()
        {
            currentState = GuideState.ReachedTarget;
            SetSprite(coinSprite);
            anchorPosition = target != null ? target.position + spawnOffsetFromPlayer : guideObject.transform.position;
            guideObject.transform.rotation = Quaternion.identity;
        }

        private void HandleShakeAnimation()
        {
            if (currentState != GuideState.ReachedTarget || guideRenderer?.sprite != coinSprite) return;
            float shakeX = Mathf.Sin(Time.time * shakeSpeed) * shakeIntensity;
            float shakeY = Mathf.Cos(Time.time * shakeSpeed * 0.5f) * shakeIntensity;
            guideObject.transform.position = anchorPosition + new Vector3(shakeX, shakeY, 0);
        }

        private void HandlePulseAnimation()
        {
            if (baseScale == Vector3.zero) return;
            float pulse = Mathf.Sin(Time.time * pulseSpeed) * 0.5f + 0.5f;
            guideObject.transform.localScale = baseScale * (1f + (pulseMaxScale - 1f) * pulse);
        }

        private void SetSprite(Sprite sprite)
        {
            if (guideRenderer != null) guideRenderer.sprite = sprite;
        }

        private void StopCustomMoveCoroutine()
        {
            if (customMoveCoroutine != null)
            {
                StopCoroutine(customMoveCoroutine);
                customMoveCoroutine = null;
            }
        }

        private IEnumerator CleanupTempTarget(GameObject tempTarget)
        {
            yield return new WaitForSeconds(lifetime);
            if (tempTarget != null) Destroy(tempTarget);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (guideObject != null) Destroy(guideObject);
        }
    }
}