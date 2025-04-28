using System.Collections.Generic;
using Script.Data_Model.Interfaces;
using Script.Game.Item.Coin;
using UnityEngine;
using Script; // Import Utility

namespace Script.Game.Other
{
    public class BuildingPlacementManager : MonoBehaviour
    {
        public static BuildingPlacementManager Instance { get; private set; }
    
        [Header("Visual Feedback")] 
        private readonly Color validPlacementColor = new Color(0, 1, 0, 0.3f);    // Green
        private readonly Color invalidPlacementColor = new Color(1, 0, 0, 0.3f);   // Red
        private readonly float lineWidth = 0.1f;
        private readonly int previewSortingOrder = 10;

        [Header("Placement Settings")]
        [Tooltip("Danh sách các layer hợp lệ để đặt công trình")]
        [SerializeField] private List<string> placementLayers = new List<string> { "Structure" };
        [Tooltip("Danh sách các tag không được phép đặt công trình đè lên nhau")]
        [SerializeField] private List<string> invalidPlacementTags = new List<string>(); // VD: thêm "Tree", "Wall",...
        // raycastYOffset chỉ dùng để thay đổi vị trí của 2 đường kẻ (vì chúng đang quá sát mặt đất)
        [SerializeField] private List<string> tagsToOrient = new List<string> { "Wall", "ArcherTower" };
        [SerializeField] private float raycastYOffset = 0.8f;

        [SerializeField] private bool showDebugLog = false;
        private GameObject townCenter;
        
        private LayerMask placementLayerMask;
        private GameObject structureToPlace;
        private GameObject previewInstance;
        private LineRenderer leftDistanceLine;
        private LineRenderer rightDistanceLine;
        private float? minimumDistance;  // Nếu prefab không có IMinimumDistance, giá trị này sẽ là null (hoặc ≤ 0)
        private bool isPlacing = false;
        private Collider2D previewCollider;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);

            placementLayerMask = LayerMask.GetMask(placementLayers.ToArray());
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
            var distanceComponent = structurePrefab.GetComponent<IMinimumDistance>();
            minimumDistance = distanceComponent?.MinimumDistance;

            previewInstance = Instantiate(structurePrefab);
            previewInstance.SetActive(true);

            SetupPreview(previewInstance);

            if (BuildingManager.instance.CurrentTownCenter != null)
            {
                Vector3 playerPos = PlayerController.PlayerController.Instance.player.transform.position;
                Vector3 townCenterPos = BuildingManager.instance.CurrentTownCenter.transform.position;
                Utility.RotateObjectTowardsTownCenter(previewInstance, playerPos, townCenterPos);
            }
            isPlacing = true;
        }


        private void SetupPreview(GameObject preview)
        {
            previewCollider = preview.GetComponent<Collider2D>();
            if (previewCollider != null)
                previewCollider.isTrigger = true;

            var coinUser = preview.GetComponent<CoinUser>();
            if (coinUser != null)
                Destroy(coinUser);

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

            Vector2 previewPos = PlayerController.PlayerController.Instance.player.transform.position;
            previewInstance.transform.position = previewPos;
            
            // Nếu tag của preview nằm trong danh sách cần định hướng, gọi hàm xoay chung
            if (BuildingManager.instance.CurrentTownCenter != null)
            {
                Vector3 playerPos = PlayerController.PlayerController.Instance.player.transform.position;
                Vector3 townCenterPos = BuildingManager.instance.CurrentTownCenter.transform.position;
                Utility.RotateObjectTowardsTownCenter(previewInstance, playerPos, townCenterPos);
            }
        
            bool overlapValid = CheckOverlapValidity(previewPos);
            bool leftValid = true, rightValid = true;
            if (minimumDistance.HasValue && minimumDistance.Value > 0)
            {
                Vector2 lineOrigin = previewPos + new Vector2(0, raycastYOffset);
                leftValid = CheckSideValidity(lineOrigin, Vector2.left, minimumDistance.Value);
                rightValid = CheckSideValidity(lineOrigin, Vector2.right, minimumDistance.Value);
            }
            bool overallValid = overlapValid && leftValid && rightValid;

            UpdateVisualFeedback(previewPos, overallValid);

            if (minimumDistance.HasValue && minimumDistance.Value > 0)
            {
                Vector2 lineOrigin = previewPos + new Vector2(0, raycastYOffset);
                UpdateDistanceLines(lineOrigin, minimumDistance.Value, leftValid, rightValid);
            }
            else
            {
                HideDistanceLines();
            }
        }
        
      

        private void UpdateVisualFeedback(Vector2 previewPos, bool overallValid)
        {
            Color feedbackColor = overallValid ? validPlacementColor : invalidPlacementColor;
            if (showDebugLog)
            {
                Debug.Log($"[UpdateVisualFeedback] PreviewPos: {previewPos}, OverallValid: {overallValid}, Color: {feedbackColor}");
            }
            
            var renderers = previewInstance.GetComponentsInChildren<SpriteRenderer>();
            foreach (var renderer in renderers)
            {
                Color color = feedbackColor;
                color.a = 0.5f;
                renderer.color = color;
            }
        }

        private void UpdateDistanceLines(Vector2 lineOrigin, float distance, bool leftValid, bool rightValid)
        {
            Color leftColor = leftValid ? validPlacementColor : invalidPlacementColor;
            Color rightColor = rightValid ? validPlacementColor : invalidPlacementColor;
            leftColor.a = 1f;
            rightColor.a = 1f;
           
            leftDistanceLine.gameObject.SetActive(true);
            leftDistanceLine.SetPosition(0, lineOrigin);
            Vector2 leftEndPos = new Vector2(lineOrigin.x - distance, lineOrigin.y);
            leftDistanceLine.SetPosition(1, leftEndPos);
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
                if (showDebugLog)
                {
                    Debug.Log($"[CheckOverlapValidity] Overlapped with: {col.gameObject.name} (Tag: {col.gameObject.tag})");
                }
                if (invalidPlacementTags.Contains(col.gameObject.tag))
                {
                    if(showDebugLog)
                    {
                        Debug.LogWarning($"[CheckOverlapValidity] Invalid overlap detected: {col.gameObject.name} with tag: {col.gameObject.tag}");
                    }
                    return false;
                }
            }

            if (minimumDistance.HasValue && minimumDistance.Value > 0)
            {
                var structures = SetUpContainer.Instance.GetObjectsFromContainerList(SetUpContainer.Instance.structureContainer);
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
                if (hit.collider != null && hit.collider.gameObject != previewInstance)
                {
                    if (invalidPlacementTags.Contains(hit.collider.gameObject.tag))
                    {
                        if (showDebugLog)
                        {
                            Debug.LogWarning($"[CheckSideValidity] Hit invalid object: {hit.collider.gameObject.name} (Tag: {hit.collider.gameObject.tag}) in direction {direction}");
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
            if (minimumDistance.HasValue && minimumDistance.Value > 0)
            {
                Vector2 lineOrigin = previewPos + new Vector2(0, raycastYOffset);
                leftValid = CheckSideValidity(lineOrigin, Vector2.left, minimumDistance.Value);
                rightValid = CheckSideValidity(lineOrigin, Vector2.right, minimumDistance.Value);
            }
            bool overallValid = overlapValid && leftValid && rightValid;

            if (overallValid)
            {
                // Just store the scale, we'll set rotation to identity
                Vector3 previewScale = previewInstance.transform.localScale;

                GameObject newStructure = SetUpContainer.Instance.InstanceAndMoveObjectToContainer(structureToPlace, previewPos);
                if (newStructure != null)
                {
                    // Force rotation to identity (no rotation) and apply only the scale from preview
                    newStructure.transform.rotation = Quaternion.identity;
                    newStructure.transform.localScale = previewScale;
                    
                    // Add debug logging
                    Debug.Log($"New structure created with rotation: {newStructure.transform.rotation.eulerAngles}, scale: {newStructure.transform.localScale}");
            
                    CleanupPreview();
                    BuildMenu.instance.ResetShowBuildMenuState();
                    BuildingManager.instance.UpdateAvailableBuildings();
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
            isPlacing = false;
        }

        private void HideDistanceLines()
        {
            if (leftDistanceLine != null)
                leftDistanceLine.gameObject.SetActive(false);
            if (rightDistanceLine != null)
                rightDistanceLine.gameObject.SetActive(false);
        }
    }
}
