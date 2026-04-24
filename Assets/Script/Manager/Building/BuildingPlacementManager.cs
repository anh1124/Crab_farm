using System.Collections.Generic;
using Script.Controller.PlayerController;
using Script.Data_Model.Interfaces;
using Script.Game.Item.Coin;
using UnityEngine;

namespace Script.Manager.Building
{
    public class BuildingPlacementManager : MonoBehaviour, IPlacementProvider
    {
    
        [Header("Visual Feedback")] 
        private readonly Color validPlacementColor = new Color(0, 1, 0, 0.3f);    // Green
        private readonly Color invalidPlacementColor = new Color(1, 0, 0, 0.3f);   // Red
        private readonly float lineWidth = 0.1f;
        private readonly int previewSortingOrder = 10;

        [Header("Placement Settings")]
        
        // Layer sẽ được quét để kiểm tra vị trí đặt công trình
        [SerializeField] private LayerMask placementLayerMask;
        
        //những tag sẽ quét, không cho đè lên nhau
        [SerializeField] private List<string> invalidPlacementTags 
            = new List<string> { "Wall", "TownCenter", "HammerVendor", "BowVendor", "Tree" };
        
        [Tooltip("Offset Y cho raycast kiểm tra khoảng cách tối thiểu." +
                 "\nDùng để di chuyển điểm xuất phát raycast lên cao hơn mặt đất." +
                 "\nGiá trị > 0 để tránh raycast chạm mặt đất.")]
        [SerializeField] private float raycastYOffset = 0.8f;

        [SerializeField] private bool showDebugLog = false;
        private GameObject townCenter;
        
        private GameObject structureToPlace;
        private GameObject previewInstance;
        private LineRenderer leftDistanceLine;
        private LineRenderer rightDistanceLine;
        private float? minimumDistance;  // Nếu prefabWall không có IMinimumDistance, giá trị này sẽ là null (hoặc ≤ 0)
        private bool isPlacing = false;
        private bool isFlipRequireCurrent = true; // Lưu trữ isFlipRequire cho building hiện tại
        private Collider2D previewCollider;

        [Header("Runtime Debug Info")]
        [SerializeField] private List<string> detectedObjects = new List<string>();

        [Header("Audio")]
        [SerializeField] private string placeBuildingSoundKey = "CoinUser_Upgrade";


        private IBuildingProvider _buildingProvider;
        private EntitySaveManager _saveManager;

        public void Setup(IBuildingProvider building, EntitySaveManager save)
        {
            _buildingProvider = building;
            _saveManager = save;
        }

        private void Awake()
        {
            InitializeVisualComponents();
        }

        private void InitializeVisualComponents()
        {
            var leftLineObj = new GameObject("LeftDistanceLine");
            var rightLineObj = new GameObject("RightDistanceLine");
        
            leftDistanceLine = leftLineObj.AddComponent<LineRenderer>();
            rightDistanceLine = rightLineObj.AddComponent<LineRenderer>();

            SetupLineRenderer(leftDistanceLine);
            SetupLineRenderer(rightDistanceLine);

            leftLineObj.transform.parent = transform;
            rightLineObj.transform.parent = transform;

            leftLineObj.SetActive(false);
            rightLineObj.SetActive(false);
        }

        private void SetupLineRenderer(LineRenderer line)
        {
            line.startWidth = lineWidth;
            line.endWidth = lineWidth;
            line.positionCount = 2;
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.sortingOrder = previewSortingOrder;
        }
        
        public void StartPlacement(GameObject structurePrefab)
        {
            CleanupPreview();

            structureToPlace = structurePrefab;

            // Get minimumDistance và isFlipRequire từ BuildingRegistry
            var buildingRegistry = BuildingRegistry.Instance;
            if (buildingRegistry != null)
            {
                BuildingType? buildingType = GetBuildingTypeFromPrefab(structurePrefab);
                if (buildingType.HasValue)
                {
                    minimumDistance = buildingRegistry.GetMinimumDistance(buildingType.Value);
                    isFlipRequireCurrent = buildingRegistry.GetIsFlipRequire(buildingType.Value);
                    if (showDebugLog)
                        Debug.Log($"[StartPlacement] BuildingType: {buildingType.Value}, isFlipRequire: {isFlipRequireCurrent}");
                }
                else
                {
                    Debug.LogWarning($"[BuildingPlacementManager] Cannot determine BuildingType for prefab {structurePrefab.name}. Using defaults.");
                    minimumDistance = 0f;
                    isFlipRequireCurrent = true;
                }
            }
            else
            {
                Debug.LogError("[BuildingPlacementManager] BuildingRegistry is not available in scene. Using defaults.");
                minimumDistance = 0f;
                isFlipRequireCurrent = true;
            }

            previewInstance = Instantiate(structurePrefab);
            previewInstance.SetActive(true);

            SetupPreview(previewInstance);

            // Chỉ lật preview nếu isFlipRequireCurrent = true
            if (isFlipRequireCurrent && _buildingProvider != null && _buildingProvider.currentTownCenter != null)
            {
                Vector3 playerPos = PlayerController.Instance.player.transform.position;
                Vector3 townCenterPos = _buildingProvider.currentTownCenter.transform.position;
                Utility.RotateObjectTowardsTownCenter(previewInstance, playerPos, townCenterPos);
            }
            isPlacing = true;
        }


        private void SetupPreview(GameObject preview)
        {
            // Check if this is a Wall
            var wallBehavior = preview.GetComponent<Script.Game.Structures.Wall_And_Stump.WallBehavior>();
            if (wallBehavior != null)
            {
                SetupWallPreview(preview, wallBehavior);
            }
            else
            {
                SetupGenericPreview(preview);
            }
        }

        private void SetupWallPreview(GameObject preview, Script.Game.Structures.Wall_And_Stump.WallBehavior wallBehavior)
        {
            StripAllScripts(preview);
            
            // 3. Setup Root Collider for Overlap Checks
            previewCollider = preview.GetComponent<Collider2D>();
            if (previewCollider != null)
            {
                previewCollider.isTrigger = true;
            }

            // 4. Remove Child Colliders (Blockers specific to Wall)
            foreach (Transform child in preview.transform)
            {
                var childColliders = child.GetComponents<Collider2D>();
                foreach (var col in childColliders)
                {
                    Destroy(col);
                }
            }

            // 5. Visuals
            ApplyPreviewVisuals(preview);
        }

        private void SetupGenericPreview(GameObject preview)
        {
            StripAllScripts(preview);

            // Generic Collider setup
            previewCollider = preview.GetComponent<Collider2D>();
            if (previewCollider != null)
            {
                previewCollider.isTrigger = true;
            }

            // Visuals
            ApplyPreviewVisuals(preview);
        }

        // --- Hàm tước sạch Script rác trên Preview ---
        private void StripAllScripts(GameObject previewObj)
        {
            // Quét TẤT CẢ components (bao gồm cả object cha lẫn con)
            Component[] allComponents = previewObj.GetComponentsInChildren<Component>(true);

            foreach (var comp in allComponents)
            {
                // KHÔNG TƯỚC các thẻ cơ bản dùng để render và set tọa độ:
                if (comp is Transform || comp is SpriteRenderer || comp is Collider2D)
                {
                    continue;
                }

                // Tiêu diệt sạch Behaviour (các script C#, Animator, Rigidbody2D, ParticleSystem...)
                if (comp is Behaviour behaviour)
                {
                    // Lỗi: Can't remove script while placing... Để an toàn ta Disable nó + Destroy
                    behaviour.enabled = false;
                    Destroy(behaviour);
                }
                else if (comp is Rigidbody2D || comp is ParticleSystem)
                {
                    // Rigidbody thì không phải Behaviour nhưng vẫn phải xóa để rơi ngã
                    Destroy(comp);
                }
            }
        }

        private void ApplyPreviewVisuals(GameObject preview)
        {
            var renderers = preview.GetComponentsInChildren<SpriteRenderer>();
            foreach (var renderer in renderers)
            {
                renderer.sortingOrder = previewSortingOrder;
                Color color = renderer.color;
                color.a = 0.5f;
                renderer.color = color;
            }
        }

        private void Update()
        {
            if (!isPlacing || previewInstance == null) return;

            Vector2 previewPos = PlayerController.Instance.player.transform.position;
            previewInstance.transform.position = previewPos;
            
            // Chỉ xoay preview nếu isFlipRequireCurrent = true
            if (isFlipRequireCurrent && _buildingProvider != null && _buildingProvider.currentTownCenter != null)
            {
                Vector3 playerPos = PlayerController.Instance.player.transform.position;
                Vector3 townCenterPos = _buildingProvider.currentTownCenter.transform.position;
                Utility.RotateObjectTowardsTownCenter(previewInstance, playerPos, townCenterPos);
            }
        
            bool overlapValid = CheckOverlapValidity(previewPos);
            bool leftValid = true, rightValid = true;
            if (minimumDistance is > 0)
            {
                Vector2 lineOrigin = previewPos + new Vector2(0, raycastYOffset);
                leftValid = CheckSideValidity(lineOrigin, Vector2.left, minimumDistance.Value);
                rightValid = CheckSideValidity(lineOrigin, Vector2.right, minimumDistance.Value);
            }
            bool overallValid = overlapValid && leftValid && rightValid;

            UpdateVisualFeedback(previewPos, overallValid);

            // Sync detected objects names to list for Inspector visibility
            SyncDetectedObjectsList(previewPos);

            if (minimumDistance is > 0)
            {
                Vector2 lineOrigin = previewPos + new Vector2(0, raycastYOffset);
                UpdateDistanceLines(lineOrigin, minimumDistance.Value, leftValid, rightValid, overallValid);
            }
            else
            {
                HideDistanceLines();
            }
        }
        
      

        private void UpdateVisualFeedback(Vector2 previewPos, bool overallValid)
        {
            Color feedbackColor = overallValid ? validPlacementColor : invalidPlacementColor;
            
            var renderers = previewInstance.GetComponentsInChildren<SpriteRenderer>();
            foreach (var renderer in renderers)
            {
                Color color = feedbackColor;
                color.a = 0.5f;
                renderer.color = color;
            }
        }

        private void SyncDetectedObjectsList(Vector2 previewPos)
        {
            detectedObjects.Clear();

            // 1. Check Overlaps
            if (previewCollider != null)
            {
                var overlaps = new List<Collider2D>();
                ContactFilter2D filter = new ContactFilter2D() { useTriggers = true, useLayerMask = true, layerMask = placementLayerMask };
                Physics2D.OverlapCollider(previewCollider, filter, overlaps);
                foreach (var col in overlaps)
                {
                    if (col.gameObject != previewInstance && !col.transform.IsChildOf(previewInstance.transform))
                        detectedObjects.Add($"[Overlap] {col.gameObject.name} (Tag: {col.gameObject.tag}, Layer: {LayerMask.LayerToName(col.gameObject.layer)})");
                }
            }

            // 2. Check Sides if applicable
            if (minimumDistance is > 0)
            {
                Vector2 lineOrigin = previewPos + new Vector2(0, raycastYOffset);
                AddSideHitsToList(lineOrigin, Vector2.left, minimumDistance.Value, "LeftRay");
                AddSideHitsToList(lineOrigin, Vector2.right, minimumDistance.Value, "RightRay");
            }
        }

        private void AddSideHitsToList(Vector2 origin, Vector2 direction, float distance, string prefix)
        {
            RaycastHit2D[] hits = Physics2D.RaycastAll(origin, direction, distance, placementLayerMask);
            foreach (var hit in hits)
            {
                if (hit.collider != null && hit.collider.gameObject != previewInstance && !hit.collider.transform.IsChildOf(previewInstance.transform))
                {
                    detectedObjects.Add($"[{prefix}] {hit.collider.gameObject.name} (Tag: {hit.collider.gameObject.tag}, Layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)})");
                }
            }
        }

        [ContextMenu("Debug Log Placement Hits (ULTRASCAN)")]
        public void DebugLogPlacementHits()
        {
            if (!isPlacing || previewInstance == null)
            {
                Debug.LogWarning("[BuildingPlacementManager] Không ở trạng thái đặt công trình, không có dữ liệu để Debug.");
                return;
            }

            Vector2 previewPos = previewInstance.transform.position;
            Vector2 lineOrigin = previewPos + new Vector2(0, raycastYOffset);

            Debug.Log("<color=magenta>=== BUILDING PLACEMENT ULTRASCAN (ALL LAYERS) ===</color>");
            Debug.Log($"Current Object: {previewInstance.name} | Active Placement LayerMask: {placementLayerMask.value}");

            // Log Overlaps - Dùng ~0 để quét sạch mọi Layer
            Debug.Log("<color=yellow>--- Overlap Hits (Universal) ---</color>");
            if (previewCollider != null)
            {
                var overlaps = new List<Collider2D>();
                // Bỏ qua filter layer để tìm mọi thứ
                ContactFilter2D filter = new ContactFilter2D() { useTriggers = true, useLayerMask = false };
                Physics2D.OverlapCollider(previewCollider, filter, overlaps);
                foreach (var col in overlaps)
                {
                    if (col.gameObject != previewInstance && !col.transform.IsChildOf(previewInstance.transform))
                    {
                        bool isInvalidTag = invalidPlacementTags.Contains(col.gameObject.tag) || 
                                         (col.transform.parent != null && invalidPlacementTags.Contains(col.transform.parent.gameObject.tag)) ||
                                         invalidPlacementTags.Contains(col.transform.root.gameObject.tag);
                        
                        bool isInSelectionLayer = (placementLayerMask.value & (1 << col.gameObject.layer)) != 0;
                        
                        string status = (isInvalidTag && isInSelectionLayer) ? "<color=red>[BLOCKING]</color>" : "<color=gray>[Detected]</color>";
                        Debug.Log($"{status} {col.gameObject.name} | Tag: {col.gameObject.tag} | Layer: {LayerMask.LayerToName(col.gameObject.layer)}");
                    }
                }
            }

            // Log Side Rays - Dùng ~0
            if (minimumDistance is > 0)
            {
                Debug.Log("<color=yellow>--- Left Ray Hits (Universal) ---</color>");
                PerformDetailedDebugRaycast(lineOrigin, Vector2.left, minimumDistance.Value);
                Debug.Log("<color=yellow>--- Right Ray Hits (Universal) ---</color>");
                PerformDetailedDebugRaycast(lineOrigin, Vector2.right, minimumDistance.Value);
            }
            Debug.Log("<color=magenta>==============================================</color>");
        }

        private void PerformDetailedDebugRaycast(Vector2 start, Vector2 direction, float distance)
        {
            // Quét Layer ~0 (mọi thứ)
            RaycastHit2D[] hits = Physics2D.RaycastAll(start, direction, distance, ~0);
            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider != null && hit.collider.gameObject != previewInstance && !hit.collider.transform.IsChildOf(previewInstance.transform))
                {
                    bool isInvalidTag = invalidPlacementTags.Contains(hit.collider.gameObject.tag) || 
                                     (hit.collider.transform.parent != null && invalidPlacementTags.Contains(hit.collider.transform.parent.gameObject.tag)) ||
                                     invalidPlacementTags.Contains(hit.collider.transform.root.gameObject.tag);

                    bool isInSelectionLayer = (placementLayerMask.value & (1 << hit.collider.gameObject.layer)) != 0;

                    string status = (isInvalidTag && isInSelectionLayer) ? "<color=red>[BLOCKING]</color>" : "<color=gray>[Detected]</color>";
                    Debug.Log($"{status} {hit.collider.gameObject.name} | Tag: {hit.collider.gameObject.tag} | Layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)}");
                }
            }
        }

        private void UpdateDistanceLines(Vector2 lineOrigin, float distance, bool leftValid, bool rightValid, bool overallValid)
        {
            // Nếu bản thân vị trí đặt bị sai (overlap), có thể ta muốn hiện cả 2 là đỏ hoặc giữ nguyên logic side
            Color leftColor = leftValid ? validPlacementColor : invalidPlacementColor;
            Color rightColor = rightValid ? validPlacementColor : invalidPlacementColor;
            
            // Tăng Alpha để dễ nhìn hơn trên LineRenderer
            leftColor.a = 0.8f;
            rightColor.a = 0.8f;
           
            leftDistanceLine.gameObject.SetActive(true);
            leftDistanceLine.SetPosition(0, lineOrigin);
            Vector2 leftEndPos = new Vector2(lineOrigin.x - distance, lineOrigin.y);
            leftDistanceLine.SetPosition(1, leftEndPos);
            // Cập nhật cả start/end và dùng color property của material nếu cần (để chắc chắn)
            leftDistanceLine.startColor = leftDistanceLine.endColor = leftColor;

            rightDistanceLine.gameObject.SetActive(true);
            rightDistanceLine.SetPosition(0, lineOrigin);
            Vector2 rightEndPos = new Vector2(lineOrigin.x + distance, lineOrigin.y);
            rightDistanceLine.SetPosition(1, rightEndPos);
            rightDistanceLine.startColor = rightDistanceLine.endColor = rightColor;
            
            PerformDebugRaycast(lineOrigin, Vector2.left, distance);
            PerformDebugRaycast(lineOrigin, Vector2.right, distance);
            
            if (showDebugLog)
            {
                Debug.Log($"[UpdateDistanceLines] LineOrigin: {lineOrigin}, Distance: {distance}, LeftValid: {leftValid}, RightValid: {rightValid}");
                Debug.Log($"[UpdateDistanceLines] LeftLine EndPos: {leftEndPos}, RightLine EndPos: {rightEndPos}");
            }
        }

        private void PerformDebugRaycast(Vector2 start, Vector2 direction, float distance)
        {
            RaycastHit2D[] hits = Physics2D.RaycastAll(start, direction, distance, placementLayerMask);
            string debugMessage = $"[PerformDebugRaycast] Raycast from {start} in direction {direction} for distance {distance}. Hits:";
            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider != null)
                {
                    debugMessage += $"\n - Object: {hit.collider.gameObject.name} (Tag: {hit.collider.gameObject.tag})";
                }
            }

            if (showDebugLog)
            {
                Debug.Log(debugMessage);
            }
        }

        private bool CheckOverlapValidity(Vector2 previewPos)
        {
            if (previewCollider == null) return true;

            var overlaps = new List<Collider2D>();
            ContactFilter2D filter = new ContactFilter2D()
            {
                useTriggers = true,
                useLayerMask = true,
                layerMask = placementLayerMask
            };
            Physics2D.OverlapCollider(previewCollider, filter, overlaps);
        
            foreach (Collider2D col in overlaps)
            {
                // Bỏ qua chính previewInstance và các con của nó
                if (col.gameObject == previewInstance || col.transform.IsChildOf(previewInstance.transform))
                {
                    continue;
                }

                // Kiểm tra tag của bản thân object hoặc root (phòng trường hợp va chạm vào Child Collider)
                bool isInvalid = invalidPlacementTags.Contains(col.gameObject.tag) || 
                                 (col.transform.parent != null && invalidPlacementTags.Contains(col.transform.parent.gameObject.tag)) ||
                                 invalidPlacementTags.Contains(col.transform.root.gameObject.tag);

                if (isInvalid)
                {
                    if (showDebugLog)
                    {
                        Debug.LogWarning($"[CheckOverlapValidity] Invalid overlap detected: {col.gameObject.name} (Root: {col.transform.root.name}) with tag: {col.transform.root.tag}");
                    }
                    return false;
                }
            }

            if (minimumDistance is > 0 && _saveManager != null)
            {
                var structures = _saveManager.GetObjectsFromContainerList(_saveManager.structureContainer);
                foreach (var structure in structures)
                {
                    if (structure.CompareTag(previewInstance.tag))
                    {
                        float dist = Vector2.Distance(previewPos, structure.transform.position);
                        if (dist < minimumDistance.Value)
                        {
                            if (showDebugLog)
                            {
                                Debug.LogWarning($"[CheckOverlapValidity] Minimum distance violated with: {structure.name} (Distance: {dist}, Minimum: {minimumDistance.Value})");
                            }
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        private bool CheckSideValidity(Vector2 origin, Vector2 direction, float distance)
        {
            RaycastHit2D[] hits = Physics2D.RaycastAll(origin, direction, distance, placementLayerMask);
            foreach (var hit in hits)
            {
                if (hit.collider != null && hit.collider.gameObject != previewInstance && !hit.collider.transform.IsChildOf(previewInstance.transform))
                {
                    // Tương tự overlap, kiểm tra tag cả ở object và root
                    bool isInvalid = invalidPlacementTags.Contains(hit.collider.gameObject.tag) || 
                                     (hit.collider.transform.parent != null && invalidPlacementTags.Contains(hit.collider.transform.parent.gameObject.tag)) ||
                                     invalidPlacementTags.Contains(hit.collider.transform.root.gameObject.tag);

                    if (isInvalid)
                    {
                        if (showDebugLog)
                        {
                            Debug.LogWarning($"[CheckSideValidity] Hit invalid object: {hit.collider.gameObject.name} in direction {direction}");
                        }
                        return false;
                    }
                }
            }
            return true;
        }
        
        public bool TryPlaceBuilding()
        {
            if (!isPlacing || previewInstance == null || structureToPlace == null)
                return false;

            Vector2 previewPos = previewInstance.transform.position;
            bool overlapValid = CheckOverlapValidity(previewPos);
            bool leftValid = true, rightValid = true;
            if (minimumDistance is > 0)
            {
                Vector2 lineOrigin = previewPos + new Vector2(0, raycastYOffset);
                leftValid = CheckSideValidity(lineOrigin, Vector2.left, minimumDistance.Value);
                rightValid = CheckSideValidity(lineOrigin, Vector2.right, minimumDistance.Value);
            }
            bool overallValid = overlapValid && leftValid && rightValid;

            if (overallValid && _saveManager != null)
            {
                GameObject newStructure = _saveManager.InstanceAndMoveObjectToContainer(structureToPlace, previewPos);
                if (newStructure != null)
                {
                    // Phát âm thanh khi đặt công trình thành công
                    if (!string.IsNullOrEmpty(placeBuildingSoundKey) && Audio.AudioManager.Instance != null)
                    {
                        Audio.AudioManager.Instance.PlaySFXByKey(placeBuildingSoundKey, previewPos);
                    }


                    // Kiểm tra xem công trình có yêu cầu flip không

                    BuildingType? buildingType = GetBuildingTypeFromPrefab(structureToPlace);
                    bool isFlipRequire = true;
                    if (buildingType.HasValue && BuildingRegistry.Instance != null)
                    {
                        isFlipRequire = BuildingRegistry.Instance.GetIsFlipRequire(buildingType.Value);
                    }

                    // Chỉ áp dụng scale từ preview cho root, không flip children
                    // (Building Behavior sẽ tự xử lý flip trong Start())
                    newStructure.transform.rotation = Quaternion.identity;

                    if (showDebugLog)
                    {
                        Debug.Log($"[BuildingPlacementManager] New structure {newStructure.name} placed at {newStructure.transform.position}. BuildingBehavior will handle flip in Start().");
                    }

                    // [FIX] Reset HP cho wall mới build
                    var wallBehavior = newStructure.GetComponent<Script.Game.Structures.Wall_And_Stump.WallBehavior>();
                    if (wallBehavior != null)
                    {
                        wallBehavior.ResetHealthToFull();
                    }

                    CleanupPreview();
                    
                    // Notifying UI and logic - ideally these would also be injected
                    var buildMenu = FindObjectOfType<BuildMenu>();
                    if (buildMenu != null) buildMenu.ResetShowBuildMenuState();
                    
                    if (_buildingProvider != null) _buildingProvider.UpdateAvailableBuildings();
                    return true;
                }
            }
            return false;
        }

        public void CancelPlacement()
        {
            CleanupPreview();
        }

        private void CleanupPreview()
        {
            if (previewInstance != null)
            {
                Destroy(previewInstance);
                previewInstance = null;
            }
            previewCollider = null;
            HideDistanceLines();
            structureToPlace = null;
            minimumDistance = null;
            isFlipRequireCurrent = true; // Reset về default
            isPlacing = false;
        }

        private void HideDistanceLines()
        {
            if (leftDistanceLine != null)
                leftDistanceLine.gameObject.SetActive(false);
            if (rightDistanceLine != null)
                rightDistanceLine.gameObject.SetActive(false);
        }

        // Helper method to determine BuildingType from prefab
        private BuildingType? GetBuildingTypeFromPrefab(GameObject prefab)
        {
            if (prefab == null) return null;

            // Try to match prefab with PrefabData references to determine type
            if (prefab == Script.Data_Model.PrefabData.Instance.townCenterList[0])
                return BuildingType.TownCenter;
            if (prefab == Script.Data_Model.PrefabData.Instance.prefabWall)
                return BuildingType.Wall;
            if (prefab == Script.Data_Model.PrefabData.Instance.hammerVendor)
                return BuildingType.HammerVendor;
            if (prefab == Script.Data_Model.PrefabData.Instance.bowVendor)
                return BuildingType.BowVendor;

            return null;
        }
    }

}
