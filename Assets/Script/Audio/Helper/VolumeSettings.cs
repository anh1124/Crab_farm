using UnityEngine;
using System;
using UnityEngine.Audio;

namespace Script.Audio
{
    /// <summary>
    /// Quản lý lưu/tải cài đặt âm lượng qua PlayerPrefs.
    /// Tách riêng khỏi AudioManager để dễ mở rộng thêm kênh mới
    /// (ví dụ: Dialogue, Music2) mà không cần sửa AudioManager.
    /// </summary>
    public class VolumeSettings
{
    // Tên các tham số phải TRÙNG KHỚP với tên Exposed Parameter trong Unity Audio Mixer
    public const string PARAM_MASTER = "MasterVolume";
    public const string PARAM_BGM    = "BGMVolume";
    public const string PARAM_ENV    = "ENVVolume";
    public const string PARAM_SFX    = "SFXVolume";
    public const string PARAM_UI     = "UIVolume";

    // PlayerPrefs key cho mono audio setting
    public const string KEY_MONO_AUDIO = "MonoAudio";

    private readonly AudioMixer _mixer;

    public VolumeSettings(AudioMixer mixer)
    {
        _mixer = mixer ?? throw new ArgumentNullException(nameof(mixer), "AudioMixer cannot be null");
    }

    public AudioMixer GetMixer() => _mixer;

    // ─────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────

    /// <summary>Đọc PlayerPrefs và áp dụng tất cả volume ngay khi khởi động.</summary>
    public void LoadAndApply()
    {
        float masterVol = PlayerPrefs.GetFloat(PARAM_MASTER, 1f);
        float bgmVol = PlayerPrefs.GetFloat(PARAM_BGM, 1f);
        float envVol = PlayerPrefs.GetFloat(PARAM_ENV, 0.7f);
        float sfxVol = PlayerPrefs.GetFloat(PARAM_SFX, 1f);
        float uiVol = PlayerPrefs.GetFloat(PARAM_UI, 1f);

        ApplyVolume(PARAM_MASTER, masterVol);
        ApplyVolume(PARAM_BGM,    bgmVol);
        ApplyVolume(PARAM_ENV,    envVol);
        ApplyVolume(PARAM_SFX,    sfxVol);
        ApplyVolume(PARAM_UI,     uiVol);

        // MonoAudio mặc định = false (stereo)
        if (!PlayerPrefs.HasKey(KEY_MONO_AUDIO))
        {
            PlayerPrefs.SetInt(KEY_MONO_AUDIO, 0);
            PlayerPrefs.Save();
        }
    }

    /// <summary>Đặt chế độ mono audio (tắt stereo panning).</summary>
    public void SetMonoAudio(bool enabled)
    {
        PlayerPrefs.SetInt(KEY_MONO_AUDIO, enabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    /// <summary>Lấy chế độ mono audio. Mặc định = false (stereo).</summary>
    public bool GetMonoAudio()
    {
        return PlayerPrefs.GetInt(KEY_MONO_AUDIO, 0) == 1;
    }

    /// <summary>
    /// Đặt volume cho một kênh.
    /// normalizedValue: 0.0 (tắt) → 1.0 (tối đa).
    /// Tự động lưu vào PlayerPrefs.
    /// </summary>
    public void SetVolume(string paramName, float normalizedValue)
    {
        normalizedValue = Mathf.Clamp01(normalizedValue);
        ApplyVolume(paramName, normalizedValue);
        PlayerPrefs.SetFloat(paramName, normalizedValue);
        PlayerPrefs.Save();
    }

    /// <summary>Trả về giá trị 0-1 đã lưu. Mặc định = 1 nếu chưa lưu.</summary>
    public float GetVolume(string paramName, float defaultValue = 1f)
    {
        return PlayerPrefs.GetFloat(paramName, defaultValue);
    }

    /// <summary>Đặt lại tất cả về mặc định.</summary>
    public void ResetToDefaults()
    {
        SetVolume(PARAM_MASTER, 1f);
        SetVolume(PARAM_BGM,    1f);
        SetVolume(PARAM_ENV,    0.7f);
        SetVolume(PARAM_SFX,    1f);
        SetVolume(PARAM_UI,     1f);
    }

    // ─────────────────────────────────────────────
    //  Private
    // ─────────────────────────────────────────────

    /// <summary>
    /// Chuyển 0-1 sang Decibel rồi gửi vào Audio Mixer.
    /// Công thức: dB = log10(vol) * 20
    /// Nếu vol = 0 → dB = -80 (im lặng hoàn toàn, tránh log(0) = -∞)
    /// </summary>
    private void ApplyVolume(string paramName, float normalizedValue)
    {
        float dB = (normalizedValue > AudioConstants.VOLUME_DAMPING)
            ? Mathf.Log10(normalizedValue) * 20f
            : AudioConstants.MUTED_DB;

        // Only log if AudioManager isDebug is enabled
        if (AudioManager.Instance != null && AudioManager.Instance.IsDebug)
            Debug.Log($"[VolumeSettings] Mixer instance ID: {_mixer.GetInstanceID()}, setting {paramName} = {dB} dB");
        _mixer.SetFloat(paramName, dB);
    }
}
}
