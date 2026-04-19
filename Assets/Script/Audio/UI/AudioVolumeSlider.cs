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
    public class AudioVolumeSlider : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{
    // ─────────────────────────────────────────────
    //  Inspector Fields
    // ─────────────────────────────────────────────

    public enum AudioChannel { Master, BGM, ENV, SFX, UI }

    [Tooltip("Kênh âm lượng mà slider này điều khiển")]
    [SerializeField] private AudioChannel channel = AudioChannel.Master;

    public AudioChannel Channel => channel;

    [Tooltip("Toggle Mute liên kết với slider này (tùy chọn)")]
    [SerializeField] private AudioMuteToggle linkedMuteToggle;

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
    private float  _previousValue;  // Theo dõi giá trị trước đó để tránh phát khi load
    private bool   _isDragging;     // Theo dõi khi user đang drag slider
    private bool   _canvasIsReady;  // Canvas đã hoàn tất load chưa

    // Static AudioSource riêng để phát âm thanh test cho tất cả slider
    private static AudioSource _testAudioSource;

    // ─────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────

    private void Awake()
    {
        _slider = GetComponent<Slider>();
        _slider.minValue = 0f;
        _slider.maxValue = 1f;

        // Tạo static AudioSource nếu chưa có
        if (_testAudioSource == null)
        {
            GameObject testAudioObj = new GameObject("[TestAudioSource]");
            DontDestroyOnLoad(testAudioObj);
            _testAudioSource = testAudioObj.AddComponent<AudioSource>();
            _testAudioSource.playOnAwake = false;
            _testAudioSource.loop = true; // Loop khi hết âm thanh
        }
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

    // ─────────────────────────────────────────────
    //  Private Methods
    // ─────────────────────────────────────────────

    private void LoadCurrentValue()
    {
        if (AudioManager.Instance == null) return;

        // Bỏ listener tạm thời để tránh trigger event khi set value
        _slider.onValueChanged.RemoveListener(OnSliderChanged);

        _isInitializing = true;

        float savedValue = channel switch
        {
            AudioChannel.Master => AudioManager.Instance.GetMasterVolume(),
            AudioChannel.BGM    => AudioManager.Instance.GetBGMVolume(),
            AudioChannel.ENV    => AudioManager.Instance.GetENVVolume(),
            AudioChannel.SFX    => AudioManager.Instance.GetSFXVolume(),
            AudioChannel.UI     => AudioManager.Instance.GetUIVolume(),
            _                   => 1f
        };

        _slider.value   = savedValue;
        _previousValue = savedValue; // Lưu giá trị để tránh phát âm thanh khi load
        _isInitializing = false;
        _canvasIsReady = true; // Canvas đã sẵn sàng

        UpdateVolumeText(savedValue);

        // Re-add listener sau khi đã set value
        _slider.onValueChanged.AddListener(OnSliderChanged);
    }

    private void OnSliderChanged(float value)
    {
        // Bỏ qua sự kiện khi đang init (tránh gọi Set khi chưa cần)
        if (_isInitializing || AudioManager.Instance == null) return;

        switch (channel)
        {
            case AudioChannel.Master: AudioManager.Instance.SetMasterVolume(value); break;
            case AudioChannel.BGM:    AudioManager.Instance.SetBGMVolume(value);    break;
            case AudioChannel.ENV:    AudioManager.Instance.SetENVVolume(value);    break;
            case AudioChannel.SFX:    AudioManager.Instance.SetSFXVolume(value);    break;
            case AudioChannel.UI:     AudioManager.Instance.SetUIVolume(value);     break;
        }

        UpdateVolumeText(value);

        // Phát âm thanh test khi đang drag slider và value thay đổi
        if (_isDragging && Mathf.Abs(value - _previousValue) > 0.001f)
        {
            PlayTestSound(value);
            _previousValue = value;
        }

        // Tự động bật/tắt mute toggle khi slider về/trên 0
        if (linkedMuteToggle != null)
        {
            var toggleComponent = linkedMuteToggle.GetComponent<Toggle>();
            if (toggleComponent != null)
            {
                // Nếu slider về 0, tự động bật mute (không kích hoạt sự kiện)
                if (value <= 0.001f && !toggleComponent.isOn)
                {
                    toggleComponent.SetIsOnWithoutNotify(true);
                }
                // Nếu slider tăng lên trên 0, tự động tắt mute (không kích hoạt sự kiện)
                else if (value > 0.001f && toggleComponent.isOn)
                {
                    toggleComponent.SetIsOnWithoutNotify(false);
                }
            }
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // Không phát nếu đang init hoặc canvas chưa sẵn sàng
        if (_isInitializing || !_canvasIsReady) return;

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
        // PlayTestSound(_slider.value);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Khi không hover, set loop = false để âm thanh phát hết rồi dừng
        if (_testAudioSource != null && _testAudioSource.isPlaying)
        {
            _testAudioSource.loop = false;
        }
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
        // Không phát nếu đang init, key rỗng, AudioManager không tồn tại hoặc testAudioSource không tồn tại
        if (_isInitializing || string.IsNullOrEmpty(testSoundKey) || AudioManager.Instance == null || _testAudioSource == null) return;

        // Lấy AudioClip từ SoundRegistry
        AudioClip clip = AudioManager.Instance.SoundRegistry.GetClip(testSoundKey);
        if (clip == null)
        {
            Debug.LogWarning($"[AudioVolumeSlider] Không tìm thấy AudioClip với key '{testSoundKey}' trong SoundRegistry");
            return;
        }

        // Nếu cùng clip đang phát, chỉ cập nhật volume (không restart)
        if (_testAudioSource.isPlaying && _testAudioSource.clip == clip)
        {
            _testAudioSource.volume = sliderValue;
            // Đảm bảo loop = true khi đang hover
            _testAudioSource.loop = true;
            return;
        }

        // Nếu khác clip hoặc chưa phát, phát mới với loop = true
        _testAudioSource.clip = clip;
        _testAudioSource.volume = sliderValue;
        _testAudioSource.loop = true;
        _testAudioSource.Play();
    }
}
}
