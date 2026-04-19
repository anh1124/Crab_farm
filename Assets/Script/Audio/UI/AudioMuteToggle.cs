using UnityEngine;
using UnityEngine.UI;

namespace Script.Audio
{
    /// <summary>
    /// Toggle để mute/unmute kênh âm thanh.
    /// Tự động lưu trạng thái mute vào PlayerPrefs.
    ///
    /// ─── CÁCH DÙNG ────────────────────────────────────────────────────────────
    /// 1. Gắn script này vào Toggle GameObject trong UI Settings.
    /// 2. Trong Inspector:
    ///    - Chọn Channel tương ứng (Master/BGM/ENV/SFX)
    ///    - (Tùy chọn) Kéo Slider cùng hàng vào Linked Slider để đồng bộ giá trị
    /// 3. Khi người dùng bật/tắt Toggle, âm thanh sẽ được mute/unmute và trạng thái được lưu.
    /// ─────────────────────────────────────────────────────────────────────────
    /// </summary>
    [RequireComponent(typeof(Toggle))]
    public class AudioMuteToggle : MonoBehaviour
{
    public enum AudioChannel { Master, BGM, ENV, SFX }

    [Header("Cài đặt")]
    [SerializeField] private AudioChannel channel = AudioChannel.Master;
    [SerializeField] private Slider linkedSlider; // Slider cùng hàng (tùy chọn)

    private Toggle _toggle;
    private float _lastVolume = 1f; // Lưu giá trị trước khi mute

    private void Awake()
    {
        _toggle = GetComponent<Toggle>();
    }

    private void OnEnable()
    {
        // Khởi tạo _lastVolume với giá trị hiện tại từ AudioManager
        _lastVolume = GetCurrentVolume();

        string muteKey = GetMuteKey();
        bool isMuted = PlayerPrefs.GetInt(muteKey, 0) == 1;

        // Dùng SetIsOnWithoutNotify để tránh gọi OnToggleChanged
        _toggle.SetIsOnWithoutNotify(isMuted);

        if (isMuted)
        {
            // Lưu lại giá trị hiện tại trước khi mute (đã được load ở trên)
            SetVolume(0f);
            if (linkedSlider != null) linkedSlider.SetValueWithoutNotify(0f);
        }

        _toggle.onValueChanged.AddListener(OnToggleChanged);
    }

    private void OnDisable()
    {
        _toggle.onValueChanged.RemoveListener(OnToggleChanged);
    }

    private void OnToggleChanged(bool isMuted)
    {
        if (AudioManager.Instance == null) return;

        float currentVol = GetCurrentVolume();
        float newVol;

        if (isMuted)
        {
            _lastVolume = currentVol;
            newVol = 0f;
        }
        else
        {
            newVol = _lastVolume > 0.001f ? _lastVolume : 1f;
        }

        SetVolume(newVol);

        // Đồng bộ slider nếu có
        if (linkedSlider != null)
            linkedSlider.value = newVol;

        // Lưu trạng thái mute
        PlayerPrefs.SetInt(GetMuteKey(), isMuted ? 1 : 0);
        PlayerPrefs.Save();
    }

    private float GetCurrentVolume()
    {
        // Convert to AudioVolumeSlider.AudioChannel to use helper method
        AudioVolumeSlider.AudioChannel sliderChannel = channel switch
        {
            AudioChannel.Master => AudioVolumeSlider.AudioChannel.Master,
            AudioChannel.BGM    => AudioVolumeSlider.AudioChannel.BGM,
            AudioChannel.ENV    => AudioVolumeSlider.AudioChannel.ENV,
            AudioChannel.SFX    => AudioVolumeSlider.AudioChannel.SFX,
            _ => AudioVolumeSlider.AudioChannel.Master
        };

        return AudioManager.Instance.GetVolumeForChannel(sliderChannel);
    }

    private void SetVolume(float value)
    {
        // Convert to AudioVolumeSlider.AudioChannel to use helper method
        AudioVolumeSlider.AudioChannel sliderChannel = channel switch
        {
            AudioChannel.Master => AudioVolumeSlider.AudioChannel.Master,
            AudioChannel.BGM    => AudioVolumeSlider.AudioChannel.BGM,
            AudioChannel.ENV    => AudioVolumeSlider.AudioChannel.ENV,
            AudioChannel.SFX    => AudioVolumeSlider.AudioChannel.SFX,
            _ => AudioVolumeSlider.AudioChannel.Master
        };

        AudioManager.Instance.SetVolumeForChannel(sliderChannel, value);
    }

    private string GetMuteKey() => $"Mute_{channel}";
}
}
