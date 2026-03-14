using System.Collections.Generic;
using UnityEngine;

namespace Script.Manager.WorldManager
{
    /// <summary>
    /// Tile Conveyor Belt — mỗi layer background cuộn vô tận.
    ///
    /// Cách hoạt động:
    ///   - Các tile nằm CỐ ĐỊNH trong World Space (không trượt theo camera).
    ///   - Khi một tile ra khỏi vùng nhìn thấy, nó TỰ NHẢY sang phía đối diện.
    ///   - Kết quả: hiệu ứng cuộn vô tận như băng chuyền.
    ///
    /// Cách dùng:
    ///   1. Gắn vào GameObject rỗng.
    ///   2. Kéo sprite(s) vào [sprites]. CHÚ Ý: Pivot của sprite phải là BottomCenter.
    ///   3. Điều chỉnh [bufferTiles] nếu muốn thêm tile dự phòng mép màn hình.
    ///   4. Play — script tự spawn và recycle.
    /// </summary>
    [DefaultExecutionOrder(1000)]
    public class EndlessBackgroundLayer : MonoBehaviour
    {
        [Header("Sprites")]
        [Tooltip("Tất cả Sprites PHẢI có Pivot = BottomCenter.")]
        public List<Sprite> sprites = new List<Sprite>();
        
        [Header("Layer Config")]
        [Tooltip("Layer Sorting. NẾU để trống, script sẽ tự copy Sorting Layer và Order trong component SpriteRenderer của Game Object này.")]
        public string sortingLayerName = "Background";
        public int orderInLayer = 0;

        [Header("Buffer")]
        [Tooltip("Số tile dự phòng phía ngoài camera mỗi bên.")]
        [Min(1)] public int bufferTiles = 2;

        [Header("Grounding")]
        [Tooltip("Nếu bật, Y=0 của layer sẽ ghim vào đáy camera.")]
        public bool lockToCameraBottom = true;
        [Tooltip("Độ lệch Y so với đáy màn hình.")]
        public float yOffset = 0f;

        // ─────────────────────────────────────────────────────────
        private Camera _camera;
        private float _tileWidth;
        private float _totalWidth;           // tổng chiều rộng của tất cả tile
        private List<Transform> _tiles = new List<Transform>();

        private void Start()
        {
            _camera = Camera.main;

            if (_camera == null || sprites == null || sprites.Count == 0 || sprites[0] == null)
            {
                Debug.LogError($"[EndlessBackgroundLayer] {name}: Thiếu Camera hoặc Sprite!");
                enabled = false;
                return;
            }

            _tileWidth = sprites[0].bounds.size.x;
            if (_tileWidth <= 0f)
            {
                Debug.LogError($"[EndlessBackgroundLayer] {name}: Sprite có chiều rộng = 0!");
                enabled = false;
                return;
            }

            if (sprites[0].pivot.y > 5f)
                Debug.LogWarning($"[EndlessBackgroundLayer] {name}: Sprite '{sprites[0].name}' nên có Pivot = BottomCenter.");

            // Đồng bộ Sorting Layer từ SpriteRenderer của GameObject gốc (nếu có)
            SpriteRenderer selfSr = GetComponent<SpriteRenderer>();
            if (selfSr != null)
            {
                sortingLayerName = selfSr.sortingLayerName;
                orderInLayer = selfSr.sortingOrder;
            }

            SpawnInitialTiles();
            TurnOffSelfRenderer();
        }

        private void LateUpdate()
        {
            if (_camera == null || _tiles.Count == 0) return;

            // Cập nhật Y-Grounding của Layer root
            if (lockToCameraBottom)
            {
                float groundY = _camera.transform.position.y - _camera.orthographicSize + yOffset;
                transform.position = new Vector3(transform.position.x, groundY, transform.position.z);
            }

            RecycleTiles();
        }

        // ─────────────────────────────────────────────────────────
        // Spawn
        // ─────────────────────────────────────────────────────────

        private void SpawnInitialTiles()
        {
            float halfViewport = _camera.orthographicSize * _camera.aspect;
            // Số tile đủ phủ màn hình + 2 bên buffer
            int tilesNeeded = Mathf.CeilToInt((halfViewport * 2f) / _tileWidth) + bufferTiles * 2 + 1;

            _totalWidth = tilesNeeded * _tileWidth;

            // Đặt tile đầu tiên sát bên trái camera
            float startWorldX = _camera.transform.position.x
                              - (tilesNeeded / 2f) * _tileWidth
                              + _tileWidth * 0.5f;

            for (int i = 0; i < tilesNeeded; i++)
            {
                float worldX = startWorldX + i * _tileWidth;
                CreateTileAt(worldX);
            }
        }

        private void CreateTileAt(float worldX)
        {
            int spriteIndex = _tiles.Count % sprites.Count;

            GameObject go = new GameObject($"Tile_{_tiles.Count}");
            go.transform.SetParent(transform, worldPositionStays: false);
            // Lưu vị trí ở LOCAL space (Y = 0 để nhường cho pivot Bottom)
            go.transform.localPosition = new Vector3(worldX - transform.position.x, 0f, 0f);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprites[spriteIndex];
            sr.sortingLayerName = sortingLayerName;
            sr.sortingOrder = orderInLayer;

            _tiles.Add(go.transform);
        }

        // ─────────────────────────────────────────────────────────
        // Recycle — kiểu "băng chuyền"
        // ─────────────────────────────────────────────────────────

        private void RecycleTiles()
        {
            float camX = _camera.transform.position.x;
            float halfTotalWidth = _totalWidth / 2f;

            foreach (var tile in _tiles)
            {
                float diff = camX - tile.position.x;

                if (diff > halfTotalWidth)
                {
                    // Camera đi về bên PHẢI quá xa → Tile này đang rớt lại sâu tít đằng sau (bên trái)
                    // Nhảy tile này lên đằng trước (bên phải)
                    tile.position += new Vector3(_totalWidth, 0f, 0f);
                    UpdateTileSprite(tile);
                }
                else if (diff < -halfTotalWidth)
                {
                    // Camera đi về bên TRÁI quá xa → Tile này đang trồi ra phía trước mặt cam (bên phải)
                    // Kéo tile này lùi lại đằng sau (bên trái)
                    tile.position -= new Vector3(_totalWidth, 0f, 0f);
                    UpdateTileSprite(tile);
                }
            }
        }

        private void UpdateTileSprite(Transform tile)
        {
            if (sprites.Count <= 1) return;
            var sr = tile.GetComponent<SpriteRenderer>();
            if (sr != null) sr.sprite = sprites[Random.Range(0, sprites.Count)];
        }

        private void TurnOffSelfRenderer()
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null) sr.enabled = false;
        }
    }
}
