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

        /// <summary>
        /// Tính volume (0-1) dựa trên khoảng cách từ sourcePos đến Player.
        /// Dùng bởi AudioEmitter khi Mono mode để áp volume thủ công.
        /// </summary>
        float CalculateDistanceVolume(Vector3 sourcePos, float minDist, float maxDist);

        /// <summary>Trả về trạng thái Mono Audio hiện tại.</summary>
        bool GetMonoAudio();
    }
}
