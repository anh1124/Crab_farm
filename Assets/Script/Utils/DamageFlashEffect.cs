using UnityEngine;
using Script.Interfaces;

namespace Script.Utils
{
    /// <summary>
    /// Component xử lý flash màu đỏ khi entity nhận damage.
    /// Tự động tìm SpriteRenderer và IHealthReactive, lắng nghe OnTakeDamageEvent để flash.
    /// </summary>
    public class DamageFlashEffect : MonoBehaviour
    {
        [Header("Flash Settings")]
        [Tooltip("Màu flash trắng đầu tiên")]
        [SerializeField] private Color flashWhiteColor = new Color(1f, 1f, 1f, 0.8f);

        [Tooltip("Màu flash đỏ sau đó")]
        [SerializeField] private Color flashRedColor = new Color(1f, 0f, 0f, 0.5f);

        [Tooltip("Thời gian flash trắng (giây)")]
        [SerializeField] private float flashWhiteDuration = 0.1f;

        [Tooltip("Thời gian flash đỏ (giây)")]
        [SerializeField] private float flashRedDuration = 0.1f;
        
        [Tooltip("Sử dụng material property block để tránh tạo material mới")]
        [SerializeField] private bool usePropertyBlock = true;

        [Tooltip("Tên property color trong shader (mặc định '_Color' cho Sprite-Default)")]
        [SerializeField] private string colorPropertyName = "_Color";

        private SpriteRenderer spriteRenderer;
        private IHealthReactive healthReactive;
        private Color originalColor;
        private MaterialPropertyBlock propertyBlock;
        private bool isFlashing;
        private float flashTimer;
        private bool isFlashingWhite;

        private void Awake()
        {
            // Tìm SpriteRenderer trên chính GameObject hoặc trong hierarchy
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            if (spriteRenderer == null)
            {
                Debug.LogWarning($"[DamageFlashEffect] Không tìm thấy SpriteRenderer trên {gameObject.name} hoặc con của nó. Component sẽ không hoạt động.", this);
                enabled = false;
                return;
            }

            // Cache màu gốc khi chưa thay đổi
            originalColor = spriteRenderer.color;

            if (usePropertyBlock)
            {
                propertyBlock = new MaterialPropertyBlock();
            }
        }

        private void OnEnable()
        {
            healthReactive = GetComponentInParent<IHealthReactive>();
            if (healthReactive != null)
            {
                healthReactive.OnTakeDamageEvent += OnTakeDamage;
                Debug.Log($"[DamageFlashEffect] Hooked into IHealthReactive on {gameObject.name}", this);
            }
            else
            {
                Debug.LogWarning($"[DamageFlashEffect] No IHealthReactive found on {gameObject.name} or parent", this);
            }
        }

        private void OnDisable()
        {
            if (healthReactive != null)
            {
                healthReactive.OnTakeDamageEvent -= OnTakeDamage;
            }
            ResetColor();
        }

        private void OnTakeDamage(float damage)
        {
            if (damage <= 0f) return;
            Debug.Log($"[DamageFlashEffect] OnTakeDamage called with damage: {damage} on {gameObject.name}", this);
            TriggerFlash();
        }

        private void TriggerFlash()
        {
            if (isFlashing)
            {
                ResetColor(); // Tắt flash cũ ngay lập tức để tạo nhịp mới
            }

            isFlashing = true;
            isFlashingWhite = true;
            flashTimer = flashWhiteDuration;

            Debug.Log($"[DamageFlashEffect] Flash white on {gameObject.name}", this);
            ApplyFlashColor(flashWhiteColor);
        }

        private void Update()
        {
            if (!isFlashing) return;

            flashTimer -= Time.deltaTime;

            if (isFlashingWhite && flashTimer <= 0f)
            {
                // Chuyển từ trắng sang đỏ
                isFlashingWhite = false;
                flashTimer = flashRedDuration;

                Debug.Log($"[DamageFlashEffect] Flash red on {gameObject.name}", this);
                ApplyFlashColor(flashRedColor);
            }
            else if (!isFlashingWhite && flashTimer <= 0f)
            {
                // Kết thúc flash đỏ, reset màu gốc
                ResetColor();
                isFlashing = false;
            }
        }

        private void ApplyFlashColor(Color color)
        {
            if (spriteRenderer == null) return;

            if (usePropertyBlock && spriteRenderer.sharedMaterial != null)
            {
                spriteRenderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(colorPropertyName, color);
                spriteRenderer.SetPropertyBlock(propertyBlock);
            }
            else
            {
                spriteRenderer.color = color;
            }
        }

        private void ResetColor()
        {
            if (spriteRenderer == null) return;

            if (usePropertyBlock && spriteRenderer.sharedMaterial != null)
            {
                spriteRenderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(colorPropertyName, originalColor);
                spriteRenderer.SetPropertyBlock(propertyBlock);
            }
            else
            {
                spriteRenderer.color = originalColor;
            }
        }

        /// <summary>
        /// Public method để trigger flash manually (nếu cần)
        /// </summary>
        public void Flash()
        {
            TriggerFlash();
        }
    }
}
