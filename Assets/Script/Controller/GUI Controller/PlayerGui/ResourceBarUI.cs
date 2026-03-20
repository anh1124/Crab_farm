using UnityEngine;
using UnityEngine.UI;
using Script.Data_Model.Status;

namespace Script.GUI_Controller 
{
    /// <summary>
    /// Generic resource bar — hiển thị HP hoặc Mana tuỳ theo <see cref="ResourceType"/>.
    /// </summary>
    public class ResourceBarUI : MonoBehaviour
    {
        [Header("Resource Settings")]
        [Tooltip("Loại resource mà thanh này đại diện")]
        public ResourceType resourceType = ResourceType.HP;

        [Header("Components")]
        [Tooltip("LayoutElement của object Mid bên trong Container")]
        public LayoutElement mid;

        [Tooltip("Image của Fill, dùng Image Type = Filled / Horizontal")]
        public Image fill;

        [Header("Fill Style")]
        [Tooltip("Màu tô của thanh fill. Sẽ được apply lên 'fill' Image.")]
        public Color fillColor = Color.red;

        [Tooltip("GameObject chứa Image fill (dùng để đổi màu). Kéo thả fill GameObject vào đây.")]
        public GameObject fillGameObject;

        [Tooltip("Kéo thả Script PlayerStatus vào đây, hoặc hệ thống sẽ tự gọi Setup()")]
        public PlayerStatus playerStatus;

        [Header("Scale Settings")]
        [Tooltip("Nếu true: thanh sẽ co giãn theo MaxResource (như cũ). Nếu false: luôn cố định baseMidWidth.")]
        public bool IsExpandable = false;

        [Tooltip("Bao nhiêu MaxHP tương ứng với 1 đơn vị baseMidWidth.\n" +
                 "Ví dụ: baseMaxHP=10 + baseMidWidth=100 thì MaxHP 10→100px, 20→200px.")]
        public float baseMaxHP = 10f;

        [Tooltip("Thanh máu có thể dài tối đa bao nhiêu lần so với baseMidWidth")]
        public float maxScaleMultiplier = 3f;

        [Header("Base Size (set theo kích thước Mid trong Prefab)")]
        [Tooltip("Chiều rộng Mid khi MaxHP = baseMaxHP. Nhìn vào RectTransform của Mid để lấy giá trị.")]
        public float baseMidWidth = 100f;

        [Tooltip("Chiều cao của Mid. Điền đúng Height của GameObject Mid trong Prefab.")]
        public float baseMidHeight = 20f;

        private float maxMidWidth;
        private ResourceType _subscribedType;

        public enum ResourceType { HP, Mana }

        // ─── Setup ───────────────────────────────────────────────────────────────

        private void Start()
        {
            if (playerStatus == null)
                gameObject.SetActive(false);
        }

        /// <summary>
        /// Gọi bởi PlayerBehavior/PlayerUISetup khi Player được tạo.
        /// Đăng ký lắng nghe event HP/Mana và cập nhật UI lần đầu.
        /// </summary>
        public void Setup(PlayerStatus newStatus)
        {
            // Hủy đăng ký event CŨ — dùng _subscribedType để an toàn
            if (playerStatus != null)
            {
                if (_subscribedType == ResourceType.HP)
                    playerStatus.OnHPChanged -= OnResourceChanged;
                else
                    playerStatus.OnManaChanged -= OnResourceChanged;
            }

            playerStatus = newStatus;
            _subscribedType = resourceType; // chốt lại loại sẽ dùng

            // Đăng ký event mới
            if (resourceType == ResourceType.HP)
                playerStatus.OnHPChanged += OnResourceChanged;
            else
                playerStatus.OnManaChanged += OnResourceChanged;

            // Áp màu fill
            if (fill != null)
                fill.color = fillColor;

            gameObject.SetActive(true);

            // Đợi 1 frame để Canvas Layout tính xong rồi init widths
            StartCoroutine(InitAfterLayout());
        }

        private void OnDestroy()
        {
            if (playerStatus == null) return;

            if (_subscribedType == ResourceType.HP)
                playerStatus.OnHPChanged -= OnResourceChanged;
            else
                playerStatus.OnManaChanged -= OnResourceChanged;
        }

        // ─── Init ─────────────────────────────────────────────────────────────────

        private System.Collections.IEnumerator InitAfterLayout()
        {
            yield return null; // Đợi 1 frame
            InitWidths();
            
            // Cập nhật lần đầu với giá trị hiện tại
            if (_subscribedType == ResourceType.HP)
                OnResourceChanged(playerStatus.currentHp, playerStatus.maxHp);
            else if (_subscribedType == ResourceType.Mana)
                OnResourceChanged(playerStatus.currentMana, playerStatus.maxMana);
        }

        private void InitWidths()
        {
            if (mid == null) return;

            maxMidWidth = baseMidWidth * maxScaleMultiplier;
            mid.preferredHeight = baseMidHeight;
        }

        // ─── Event Handler ────────────────────────────────────────────────────────

        // Đổi tên từ OnHPChanged → OnResourceChanged để phản ánh đúng chức năng generic
        private void OnResourceChanged(float current, float max)
        {
            UpdateMidWidth(max);
            UpdateFill(current, max);
        }

        // ─── Display Logic ────────────────────────────────────────────────────────

        private void UpdateMidWidth(float maxResource)
        {
            if (mid == null)
            {
                Debug.LogWarning("[ResourceBarUI] 'mid' chưa được gán trong Inspector!");
                return;
            }

            if (!IsExpandable)
            {
                // Cố định width = baseMidWidth, không scale theo maxResource
                mid.preferredWidth = baseMidWidth;
                return;
            }

            if (baseMidWidth <= 0) return;

            float targetWidth = (maxResource / baseMaxHP) * baseMidWidth;
            mid.preferredWidth = Mathf.Clamp(targetWidth, 0f, maxMidWidth);
        }

        private void UpdateFill(float currentResource, float maxResource)
        {
            if (fill == null)
            {
                Debug.LogWarning("[ResourceBarUI] 'fill' chưa được gán trong Inspector!");
                return;
            }

            fill.fillAmount = (maxResource > 0) ? Mathf.Clamp01(currentResource / maxResource) : 0f;
        }
    }
}
