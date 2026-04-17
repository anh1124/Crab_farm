using UnityEngine;
using UnityEngine.UI;

namespace Script.Audio
{
    /// <summary>
    /// Button để reset tất cả âm lượng về mặc định.
    /// Reset cả Slider và Mute Toggle.
    ///
    /// ─── CÁCH DÙNG ────────────────────────────────────────────────────────────
    /// 1. Gắn script này vào Button "Mặc định" trong UI Settings.
    /// 2. Trong Inspector:
    ///    - Kéo tất cả Slider vào mảng Sliders To Reset
    ///    - Kéo tất cả AudioMuteToggle vào mảng Mute Toggles
    /// 3. Khi người dùng nhấn nút, tất cả âm lượng sẽ được reset về giá trị mặc định.
    /// ─────────────────────────────────────────────────────────────────────────
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class AudioResetButton : MonoBehaviour
{
    [Header("Thành phần cần reset")]
    [SerializeField] private Slider[] slidersToReset;
    [SerializeField] private AudioMuteToggle[] muteToggles;

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(ResetAll);
    }

    private void ResetAll()
    {
        if (AudioManager.Instance == null) return;

        // Reset âm lượng trong AudioManager
        AudioManager.Instance.ResetAllVolumes();

        // Reset tất cả slider về giá trị mặc định thực tế từ AudioManager
        foreach (var slider in slidersToReset)
        {
            if (slider == null) continue;
            
            // Lấy component AudioVolumeSlider để biết channel
            var volSlider = slider.GetComponent<AudioVolumeSlider>();
            if (volSlider != null)
            {
                float val = volSlider.Channel switch
                {
                    AudioVolumeSlider.AudioChannel.Master => AudioManager.Instance.GetMasterVolume(),
                    AudioVolumeSlider.AudioChannel.BGM    => AudioManager.Instance.GetBGMVolume(),
                    AudioVolumeSlider.AudioChannel.ENV    => AudioManager.Instance.GetENVVolume(), // Sẽ là 0.7
                    AudioVolumeSlider.AudioChannel.SFX    => AudioManager.Instance.GetSFXVolume(),
                    _ => 1f
                };
                slider.value = val;
            }
            else
            {
                slider.value = 1f; // Fallback nếu không có AudioVolumeSlider
            }
        }

        // Reset tất cả toggle mute về tắt
        foreach (var toggle in muteToggles)
        {
            if (toggle != null)
            {
                // Lấy component Toggle từ AudioMuteToggle
                var toggleComponent = toggle.GetComponent<Toggle>();
                if (toggleComponent != null)
                    toggleComponent.isOn = false;
            }
        }
    }
}
}
