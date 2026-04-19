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
    [SerializeField] private AudioVolumeSlider.AudioChannel[] sliderChannels; // Channel tương ứng với mỗi slider
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
        for (int i = 0; i < slidersToReset.Length; i++)
        {
            var slider = slidersToReset[i];
            if (slider == null) continue;

            // Lấy channel từ mảng sliderChannels
            if (i < sliderChannels.Length)
            {
                float val = AudioManager.Instance.GetVolumeForChannel(sliderChannels[i]);
                slider.value = val;
            }
            else
            {
                slider.value = 1f; // Fallback nếu không có channel tương ứng
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
