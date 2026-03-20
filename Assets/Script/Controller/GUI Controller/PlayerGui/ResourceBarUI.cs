using System.Collections;
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

        [Header("Shake Settings")]
        [Tooltip("Biên độ rung theo trục Y (pixel)")]
        public float shakeAmount = 0.8f;

        private float maxMidWidth;
        private ResourceStat _resource;
        private bool _isShaking;

        // ─── Setup ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Gọi bởi PlayerBehavior/PlayerUISetup khi Player được tạo.
        /// Đăng ký lắng nghe event và cập nhật UI lần đầu.
        /// </summary>
        public void Setup(ResourceStat resource)
        {
            // Hủy đăng ký event CŨ
            if (_resource != null)
            {
                _resource.OnChanged   -= OnResourceChanged;
                _resource.OnUITrigger -= NotifyUITrigger;
            }

            _resource = resource;
            
            // Đăng ký event mới
            _resource.OnChanged   += OnResourceChanged;
            _resource.OnUITrigger += NotifyUITrigger;

            // Áp màu fill
            if (fill != null)
                fill.color = fillColor;

            gameObject.SetActive(true);

            // Đợi 1 frame để Canvas Layout tính xong rồi init widths
            StartCoroutine(InitAfterLayout());
        }

        private void OnDestroy()
        {
            if (_resource == null) return;
            
            _resource.OnChanged   -= OnResourceChanged;
            _resource.OnUITrigger -= NotifyUITrigger;
        }

        /// <summary>
        /// Gọi khi OnUITrigger bắn — shake nhẹ thanh bar theo trục Y.
        /// </summary>
        public void NotifyUITrigger()
        {
            if (!_isShaking)
                StartCoroutine(ShakeRoutine());
        }

        [ContextMenu("Test Shaking")]
        public void TestShaking()
        {
            NotifyUITrigger();
        }

        private IEnumerator ShakeRoutine()
        {
            _isShaking = true;
            RectTransform rt = GetComponent<RectTransform>();
            Vector2 origin = rt.anchoredPosition;

            // 3 chu kỳ rung: lên → xuống → về gốc
            float[] offsets = { shakeAmount, -shakeAmount, shakeAmount * 0.5f, -shakeAmount * 0.5f, 0f };
            foreach (float offset in offsets)
            {
                rt.anchoredPosition = origin + Vector2.up * offset;
                yield return new WaitForSecondsRealtime(0.04f);
            }

            rt.anchoredPosition = origin;
            _isShaking = false;
        }

        // ─── Init ─────────────────────────────────────────────────────────────────

        private System.Collections.IEnumerator InitAfterLayout()
        {
            yield return null; // Đợi 1 frame
            InitWidths();
            
            // Cập nhật lần đầu với giá trị hiện tại
            if (_resource != null)
            {
                OnResourceChanged(_resource.Current, _resource.Max);
            }
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
