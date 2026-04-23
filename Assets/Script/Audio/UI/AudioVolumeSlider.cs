using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace Script.Audio
{
    /// <summary>
    /// Kết nối một Unity UI Slider với kênh âm lượng tương ứng trong AudioManager.
    /// Gắn script này vào cùng GameObject với Slider.
    /// Tự động đọc giá trị đã lưu khi mở Settings Menu.
    ///
    /// ─── SETUP ────────────────────────────────────────────────────────────────
    /// 1. Tạo Slider trong Canvas (Settings Panel).
    /// 2. Gắn AudioVolumeSlider vào cùng GameObject đó.
    /// 3. Chọn Channel (Master/BGM/ENV/SFX/UI) trong Inspector.
    /// 4. Xong. Script tự kết nối OnValueChanged và load giá trị đã lưu.
    /// ─────────────────────────────────────────────────────────────────────────
    /// </summary>
    [RequireComponent(typeof(Slider))]
    public class AudioVolumeSlider : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler
{
    // ─────────────────────────────────────────────
    //  Inspector Fields
    // ─────────────────────────────────────────────

    [Tooltip("Kênh âm lượng mà slider này điều khiển")]
    [SerializeField] private Script.Audio.AudioChannel channel = Script.Audio.AudioChannel.Master;

    public Script.Audio.AudioChannel Channel => channel;

    [Tooltip("TextMeshPro để hiển thị % volume (tùy chọn)")]
    [SerializeField] private TextMeshProUGUI volumeText;

    [Header("Test Sound")]
    [Tooltip("Key âm thanh test từ SoundRegistry (phát khi chỉnh slider)")]
    [SerializeField] private string testSoundKey = "Test_Sound";

    // ─────────────────────────────────────────────
    //  Private State
    // ─────────────────────────────────────────────

    private Slider _slider;
    private bool   _isInitializing; // Tránh vòng lặp khi set value lúc init
    private bool   _isDragging;     // Theo dõi khi user đang drag slider

    // ─────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────

    private void Awake()
    {
        _slider = GetComponent<Slider>();
        _slider.minValue = 0f;
        _slider.maxValue = 1f;
    }

    private void OnEnable()
    {
        // Bỏ listener cũ (nếu có) để tránh trigger khi set value
        _slider.onValueChanged.RemoveListener(OnSliderChanged);

        // Mỗi lần mở Settings Panel, load lại giá trị hiện tại
        LoadCurrentValue();

        // Đăng ký lắng nghe thay đổi
        _slider.onValueChanged.AddListener(OnSliderChanged);
    }

    private void OnDisable()
    {
        // Bỏ listener để tránh sự kiện cũ trigger khi mở canvas lại
        if (_slider != null)
        {
            _slider.onValueChanged.RemoveListener(OnSliderChanged);
        }
    }

    /// <summary>
    /// Refresh slider value from AudioManager. Call this when opening Settings Panel.
    /// </summary>
    public void Refresh()
    {
        LoadCurrentValue();
    }

    // ─────────────────────────────────────────────
    //  Private Methods
    // ─────────────────────────────────────────────

    private void LoadCurrentValue()
    {
        if (AudioManager.Instance == null) return;

        // Bỏ listener tạm thời để tránh trigger event khi set value
        _slider.onValueChanged.RemoveListener(OnSliderChanged);

        _isInitializing = true;

        float savedValue = AudioManager.Instance.GetVolumeForChannel(channel);

        _slider.value   = savedValue;
        _isInitializing = false;

        UpdateVolumeText(savedValue);

        // Re-add listener sau khi đã set value
        _slider.onValueChanged.AddListener(OnSliderChanged);
    }

    private void OnSliderChanged(float value)
    {
        // Bỏ qua sự kiện khi đang init (tránh gọi Set khi chưa cần)
        if (_isInitializing || AudioManager.Instance == null) return;

        AudioManager.Instance.SetVolumeForChannel(channel, value);

        UpdateVolumeText(value);

        // Phát âm thanh test khi đang drag slider
        if (_isDragging)
        {
            PlayTestSound(value);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // Không phát nếu đang init
        if (_isInitializing) return;

        _isDragging = true;
        // Phát âm thanh test khi user bắt đầu drag
        PlayTestSound(_slider.value);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _isDragging = false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Không phát âm thanh khi hover, chỉ phát khi click/drag
    }

    private void UpdateVolumeText(float value)
    {
        if (volumeText == null) return;

        // Hiển thị dạng % (ví dụ: 75%)
        int percentage = Mathf.RoundToInt(value * 100f);
        volumeText.text = $"{percentage}%";
    }

    private void PlayTestSound(float sliderValue)
    {
        // Không phát nếu đang init, key rỗng, hoặc AudioManager không tồn tại
        if (_isInitializing || string.IsNullOrEmpty(testSoundKey) || AudioManager.Instance == null) return;

        // Sử dụng AudioManager.PlayUISound thay vì static AudioSource
        AudioManager.Instance.PlayUISound(testSoundKey, sliderValue);
    }
}
}
