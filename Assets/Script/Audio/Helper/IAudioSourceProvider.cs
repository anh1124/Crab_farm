using UnityEngine;

namespace Script.Audio
{
    /// <summary>
    /// Interface cung cấp AudioSource và SoundEntry lookup.
    /// Được dùng bởi AudioEmitter để tách coupling với AudioManager.Instance.
    /// Cho phép dependency injection và testability.
    /// </summary>
    public interface IAudioSourceProvider
    {
        /// <summary>Lấy AudioSource từ pool tại vị trí cụ thể.</summary>
        AudioSource Rent(Vector3 worldPosition);

        /// <summary>Trả AudioSource về pool.</summary>
        void Return(AudioSource source);

        /// <summary>Lấy SoundEntry theo key từ registry.</summary>
        SoundRegistry.SoundEntry GetEntry(string soundKey);
    }
}
