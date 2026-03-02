using UnityEngine;

namespace Script.Entities.Structures.SummondPortal
{
    public class PortalVisualAnimator : MonoBehaviour
    {
        [Header("Sprite Frames")]
        [Tooltip("Số lượng frames Front và Back phải bằng nhau")]
        public Sprite[] backFramesUnactive;
        public Sprite[] frontFramesUnactive;
        public Sprite[] backFramesActive;
        public Sprite[] frontFramesActive;

        [Header("Renderers")]
        public SpriteRenderer backRenderer;
        public SpriteRenderer frontRenderer;

        [Header("Animation Settings")]
        [Tooltip("Tốc độ khung hình (FPS) cố định của cổng.")]
        public float frameRate = 12f;

        private Sprite[] currentBackFrames;
        private Sprite[] currentFrontFrames;
        private float timer;
        private int currentFrame;
        private bool isSetupValid = true;

        void Awake()
        {
            // Validate configuration
            if (backFramesUnactive == null || frontFramesUnactive == null || backFramesActive == null || frontFramesActive == null ||
                backFramesUnactive.Length == 0 || backFramesActive.Length == 0 ||
                backFramesUnactive.Length != frontFramesUnactive.Length || backFramesActive.Length != frontFramesActive.Length)
            {
                Debug.LogError($"[PortalVisualAnimator] Cấu hình frames bị lỗi trên {gameObject.name}. " +
                               $"Các mảng Frames không tồn tại hoặc độ dài Front/Back lệch nhau!");
                isSetupValid = false;
            }
            
            if (backRenderer == null || frontRenderer == null)
            {
                Debug.LogError($"[PortalVisualAnimator] Thiếu SpriteRenderer trên {gameObject.name}.");
                isSetupValid = false;
            }

            // Mặc định khởi tạo mảng ngủ
            currentBackFrames = backFramesUnactive;
            currentFrontFrames = frontFramesUnactive;
        }

        void Update()
        {
            if (!isSetupValid || currentBackFrames == null || currentFrontFrames == null) return;

            timer += Time.deltaTime;
            if (timer >= 1f / frameRate)
            {
                timer -= 1f / frameRate;
                currentFrame = (currentFrame + 1) % currentBackFrames.Length;

                // Sync tuyệt đối 2 layer trong cùng 1 frame
                backRenderer.sprite = currentBackFrames[currentFrame];
                frontRenderer.sprite = currentFrontFrames[currentFrame];
            }
        }

        public void SetDormant()
        {
            currentBackFrames = backFramesUnactive;
            currentFrontFrames = frontFramesUnactive;
            currentFrame = 0;
            timer = 0f;
        }

        public void SetActive()
        {
            currentBackFrames = backFramesActive;
            currentFrontFrames = frontFramesActive;
            currentFrame = 0;
            timer = 0f;
        }
    }
}
