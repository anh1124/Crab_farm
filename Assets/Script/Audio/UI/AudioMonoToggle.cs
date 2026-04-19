using UnityEngine;
using UnityEngine.UI;

namespace Script.Audio
{
    /// <summary>
    /// Kết nối một Unity UI Toggle với MonoAudio setting trong VolumeSettings.
    /// Gắn script này vào cùng GameObject với Toggle.
    /// Tự động đọc giá trị đã lưu khi mở Settings Menu.
    ///
    /// ─── SETUP ────────────────────────────────────────────────────────────────
    /// 1. Tạo Toggle trong Canvas (Settings Panel).
    /// 2. Gắn AudioMonoToggle vào cùng GameObject đó.
    /// 3. Xong. Script tự kết nối OnValueChanged và load giá trị đã lưu.
    /// ─────────────────────────────────────────────────────────────────────────
    /// </summary>
    [RequireComponent(typeof(Toggle))]
    public class AudioMonoToggle : MonoBehaviour
    {
        // ─────────────────────────────────────────────
        //  Inspector Fields
        // ─────────────────────────────────────────────

        [Tooltip("TextMeshPro hoặc Text để hiển thị Mono/Stereo")]
        [SerializeField] private UnityEngine.UI.Text label;

        // ─────────────────────────────────────────────
        //  Private State
        // ─────────────────────────────────────────────

        private Toggle _toggle;
        private bool   _isInitializing; // Tránh vòng lặp khi set value lúc init

        // ─────────────────────────────────────────────
        //  Unity Lifecycle
        // ─────────────────────────────────────────────

        private void Awake()
        {
            _toggle = GetComponent<Toggle>();
        }

        private void OnEnable()
        {
            // Bỏ listener cũ (nếu có) để tránh trigger khi set value
            _toggle.onValueChanged.RemoveListener(OnToggleChanged);

            // Mỗi lần mở Settings Panel, load lại giá trị hiện tại
            LoadCurrentValue();

            // Đăng ký lắng nghe thay đổi
            _toggle.onValueChanged.AddListener(OnToggleChanged);
        }

        private void OnDisable()
        {
            // Bỏ listener để tránh sự kiện cũ trigger khi mở canvas lại
            if (_toggle != null)
            {
                _toggle.onValueChanged.RemoveListener(OnToggleChanged);
            }
        }

        // ─────────────────────────────────────────────
        //  Private Methods
        // ─────────────────────────────────────────────

        private void LoadCurrentValue()
        {
            if (AudioManager.Instance == null || AudioManager.Instance.VolumeSettings == null) return;

            // Bỏ listener tạm thời để tránh trigger event khi set value
            _toggle.onValueChanged.RemoveListener(OnToggleChanged);

            _isInitializing = true;

            bool savedValue = AudioManager.Instance.VolumeSettings.GetMonoAudio();

            _toggle.SetIsOnWithoutNotify(savedValue);
            _isInitializing = false;

            UpdateLabelText(savedValue);

            // Re-add listener sau khi đã set value
            _toggle.onValueChanged.AddListener(OnToggleChanged);
        }

        private void OnToggleChanged(bool value)
        {
            // Bỏ qua sự kiện khi đang init (tránh gọi Set khi chưa cần)
            if (_isInitializing || AudioManager.Instance == null || AudioManager.Instance.VolumeSettings == null) return;

            AudioManager.Instance.VolumeSettings.SetMonoAudio(value);
            UpdateLabelText(value);

            // Apply ngay cho tất cả emitter đang active (loop sounds như portal, campfire...)
            var emitters = AudioEmitterRegistry.All;
            for (int i = 0; i < emitters.Count; i++)
            {
                if (emitters[i] != null)
                    emitters[i].ApplyMonoSetting(value);
            }
        }

        private void UpdateLabelText(bool isMono)
        {
            if (label == null) return;

            label.text = isMono ? "Mono" : "Stereo";
        }
    }
}
