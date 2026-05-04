using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Script.Manager.WorldManager
{
    /// <summary>
    /// Một script duy nhất cho mọi layer background.
    ///
    /// Cách dùng:
    ///   1. Gắn script này vào một GameObject rỗng.
    ///   2. Kéo sprite(s) vào danh sách [sprites] trong Inspector. CHÚ Ý: Toàn bộ sprite phải có Pivot = Bottom hoặc BottomCenter.
    ///   3. Chỉnh parallaxFactor (0 = đứng yên trên màn hình, 1 = di chuyển trên màn hình theo camera).
    ///   4. Play — script tự tạo child, tự đo kích thước, tự recycle.
    /// </summary>
    [DefaultExecutionOrder(1000)] // Đảm bảo Parallax chạy SAU khi Camera đã hoàn thành việc di chuyển
    public class ParallaxLayer : MonoBehaviour
    {
        [Header("Sprites")]
        [Tooltip("CHÚ Ý QUAN TRỌNG: Tất cả Sprites PHẢI được thiết lập Pivot là Bottom (hoặc BottomCenter) trong Sprite Editor để viền dưới luôn bằng Y=0!")]
        public List<Sprite> sprites = new List<Sprite>();
        
        [Header("Layer Config")]
        [Tooltip("Layer Sorting. NẾU để trống, script sẽ tự copy Sorting Layer và Order trong component SpriteRenderer của Game Object này.")]
        public string sortingLayerName = "Sky";
        public int orderInLayer = 0;

        [Range(0f, 1f)]
        public float parallaxFactor = 0.3f;

        [Tooltip("Nếu bật, layer sẽ chỉ đơn giản là đi theo Camera mà không thực hiện tiling/parallax (Dùng cho lớp nền trời cố định).")]
        public bool onlyFollowCamera = false;

        [Header("Buffer")]
        [Min(1)] public int bufferTiles = 1;

        [Header("Grounding")]
        [Tooltip("Nếu bật, chân của Layer (Y=0) sẽ luôn bám theo đáy của màn hình.")]
        public bool lockToCameraBottom = true;
        [Tooltip("Độ lệch Y so với đáy màn hình (nếu lockToCameraBottom được bật).")]
        public float yOffset = 0f;

        public enum ParallaxTarget { Camera, Player }

        [Header("Target")]
        [Tooltip("Chọn mục tiêu để Parallax đi theo")]
        public ParallaxTarget targetType = ParallaxTarget.Camera;
        
        [Tooltip("Nếu được gán, Parallax sẽ đi theo transform này (Ghi đè Target Type)")]
        public Transform followTarget;

        private Camera _camera;
        private float _originCamX;
        private float _tileWidth;
        private List<Transform> _tiles = new List<Transform>();
        private List<SpriteRenderer> _childRenderers = new List<SpriteRenderer>();
        private float _totalWidth;

        private void Start()
        {
            _camera = Camera.main;

            // Bắt đầu tìm kiếm Target liên tục cho đến khi thấy
            StartCoroutine(FindTargetCoroutine());

            if (_camera == null)
            {
                enabled = false;
                return;
            }

            // Nếu không phải chế độ follow camera thuần túy, yêu cầu phải có sprite để tiling
            if (!onlyFollowCamera && (sprites == null || sprites.Count == 0 || sprites[0] == null))
            {
                Debug.LogWarning($"[ParallaxLayer] {name}: Thiếu sprites để thực hiện Parallax Tiling.");
                enabled = false;
                return;
            }

            _tileWidth = sprites[0].bounds.size.x;

            if (_tileWidth <= 0f)
            {
                Debug.LogError($"[ParallaxLayer] {name}: Sprite có chiều rộng bằng 0!");
                enabled = false;
                return;
            }

            // Cảnh báo nếu Pivot không nằm ở đáy
            if (sprites[0].pivot.y > 5f) 
            {
                Debug.LogWarning($"[ParallaxLayer] {name}: Vui lòng kiểm tra lại Pivot của Sprite '{sprites[0].name}'. Script hoạt động tốt nhất khi Pivot là Bottom hoặc BottomCenter.");
            }

            // Đồng bộ Sorting Layer từ SpriteRenderer của GameObject gốc (nếu có)
            SpriteRenderer selfSr = GetComponent<SpriteRenderer>();
            if (selfSr != null)
            {
                sortingLayerName = selfSr.sortingLayerName;
                orderInLayer = selfSr.sortingOrder;
            }

            if (!onlyFollowCamera)
            {
                SpawnInitialTiles();
            }
            TurnOffCurrentSpriteRenderer();
        }

        private IEnumerator FindTargetCoroutine()
        {
            while (followTarget == null)
            {
                if (targetType == ParallaxTarget.Camera)
                {
                    if (Camera.main != null) followTarget = Camera.main.transform;
                }
                else if (targetType == ParallaxTarget.Player)
                {
                    GameObject player = GameObject.FindGameObjectWithTag("Player");
                    if (player != null) followTarget = player.transform;
                }

                if (followTarget != null)
                {
                    // Khi đã tìm thấy target, thiết lập lại điểm gốc để tính toán Parallax
                    _originCamX = followTarget.position.x;
                    Debug.Log($"[ParallaxLayer] {name} đã tìm thấy và gán Target: {followTarget.name}");
                    yield break; // Thoát Coroutine
                }

                yield return new WaitForSeconds(1.0f);
            }
        }
        
        private void TurnOffCurrentSpriteRenderer()
        {
            SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = false;
            }
        }
        
        private void LateUpdate()
        {
            if (_camera == null || followTarget == null) return;
            if (!onlyFollowCamera && _tiles.Count == 0) return;

            Transform targetTransform = followTarget;

            if (onlyFollowCamera)
            {
                // Chỉ đi theo camera, không tính toán parallax phức tạp
                float x = targetTransform.position.x;
                float y = transform.position.y;
                if (lockToCameraBottom)
                {
                    y = _camera.transform.position.y - _camera.orthographicSize + yOffset;
                }
                transform.position = new Vector3(x, y, transform.position.z);
                return;
            }

            // 1. Tính toán vị trí X (Parallax)
            float camDelta = targetTransform.position.x - _originCamX;
            float followAmount = 1f - parallaxFactor; 
            float targetX = _originCamX + camDelta * followAmount;

            // 2. Tính toán vị trí Y (Grounding)
            float targetY = transform.position.y;
            if (lockToCameraBottom)
            {
                targetY = _camera.transform.position.y - _camera.orthographicSize + yOffset;
            }
            
            transform.position = new Vector3(targetX, targetY, transform.position.z);

            // 3. Recycle
            RecycleTiles(targetTransform);
        }

        private void SpawnInitialTiles()
        {
            float halfViewport = _camera.orthographicSize * _camera.aspect;
            int tilesNeeded = Mathf.CeilToInt((halfViewport * 2f) / _tileWidth) + bufferTiles * 2 + 1;
            
            _totalWidth = tilesNeeded * _tileWidth;

            float camLocalX = _camera.transform.position.x - transform.position.x;
            
            float startX = camLocalX - (_totalWidth / 2f) + (_tileWidth / 2f);

            for (int i = 0; i < tilesNeeded; i++)
            {
                CreateTileAt(startX + i * _tileWidth);
            }
        }

        private void CreateTileAt(float localX)
        {
            int spriteIndex = _tiles.Count % sprites.Count;
            Sprite sprite = sprites[spriteIndex];

            GameObject go = new GameObject($"Tile_{_tiles.Count}");
            go.transform.SetParent(transform, worldPositionStays: false);
            
            go.transform.localPosition = new Vector3(localX, 0f, 0f);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingLayerName = sortingLayerName;
            sr.sortingOrder = orderInLayer;

            _tiles.Add(go.transform);
            _childRenderers.Add(sr);
        }

        /// <summary>
        /// Thay đổi màu sắc cho toàn bộ các mảnh sprite của layer này.
        /// </summary>
        public void SetLayerColor(Color color)
        {
            // Nếu danh sách cache trống (do onlyFollowCamera hoặc có obj con thủ công), tự động tìm kiếm
            if (_childRenderers.Count == 0)
            {
                GetComponentsInChildren<SpriteRenderer>(includeInactive: true, _childRenderers);
            }

            foreach (var sr in _childRenderers)
            {
                if (sr != null) sr.color = color;
            }
        }

        private void RecycleTiles(Transform targetTransform)
        {
            float targetX = targetTransform.position.x;
            float camLocalX = targetX - transform.position.x;
            float halfTotalWidth = _totalWidth / 2f;

            foreach (var tile in _tiles)
            {
                float diff = camLocalX - tile.localPosition.x;

                if (diff > halfTotalWidth)
                {
                    tile.localPosition += new Vector3(_totalWidth, 0f, 0f);
                    UpdateTileSprite(tile);
                }
                else if (diff < -halfTotalWidth)
                {
                    tile.localPosition -= new Vector3(_totalWidth, 0f, 0f);
                    UpdateTileSprite(tile);
                }
            }
        }

        private void UpdateTileSprite(Transform tile)
        {
            if (sprites.Count <= 1) return;
            var sr = tile.GetComponent<SpriteRenderer>();

            int tileIndex = _tiles.IndexOf(tile);
            if (sr != null && tileIndex != -1)
            {
                sr.sprite = sprites[tileIndex % sprites.Count];
            }
        }
    }
}
