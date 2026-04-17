using UnityEngine;

namespace Script.Audio
{
    /// <summary>
    /// Phát âm thanh mẫu từ SoundRegistry.
    /// Dùng để phát âm thanh UI khi người dùng tương tác (click, hover, drag...).
    ///
    /// ─── CÁCH DÙNG ────────────────────────────────────────────────────────────
    /// 1. Gắn script này vào Button hoặc Slider trong UI.
    /// 2. Trong Inspector, nhập key âm thanh (ví dụ: "UI_Click", "UI_Slider").
    /// 3. Kéo hàm PlaySampleSound() vào sự kiện UI:
    ///    - Button: OnClick()
    ///    - Slider: OnValueChanged() hoặc dùng Event Trigger với OnPointerUp
    /// ─────────────────────────────────────────────────────────────────────────
    /// </summary>
    public class SampleSoundPlayer : MonoBehaviour
{
    [Header("Cài đặt âm thanh")]
    [SerializeField] private string sampleSoundKey = "UI_Click";

    /// <summary>
    /// Phát âm thanh mẫu. Gọi hàm này từ sự kiện UI.
    /// </summary>
    public void PlaySampleSound()
    {
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(sampleSoundKey))
            AudioManager.Instance.PlaySFXByKey(sampleSoundKey, Vector3.zero);
    }

    /// <summary>
    /// Phát âm thanh mẫu với key khác. Hữu dụng khi muốn dùng nhiều âm thanh khác nhau.
    /// </summary>
    public void PlaySampleSound(string soundKey)
    {
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(soundKey))
            AudioManager.Instance.PlaySFXByKey(soundKey, Vector3.zero);
    }
}
}
