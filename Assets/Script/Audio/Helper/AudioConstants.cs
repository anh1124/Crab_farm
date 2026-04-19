using UnityEngine;

namespace Script.Audio
{
    /// <summary>
    /// Hằng số dùng chung cho hệ thống Audio.
    /// Thay thế magic numbers trong code để dễ maintain.
    /// </summary>
    public static class AudioConstants
    {
        // ─────────────────────────────────────────────
        //  Timing
        // ─────────────────────────────────────────────

        /// <summary>Buffer thời gian để tránh cắt tiếng đột ngột khi return AudioSource (giây)</summary>
        public const float BUFFER_TIME = 0.1f;

        /// <summary>Thời gian chờ để check PlayerTransform có được gán chưa (giây)</summary>
        public const float PLAYER_CHECK_DELAY = 3f;

        // ─────────────────────────────────────────────
        //  Distance
        // ─────────────────────────────────────────────

        /// <summary>Khoảng cách mặc định để nghe to nhất (đơn vị Unity)</summary>
        public const float DEFAULT_MIN_DISTANCE = 2f;

        /// <summary>Khoảng cách mặc định âm thanh hoàn toàn im (đơn vị Unity)</summary>
        public const float DEFAULT_MAX_DISTANCE = 15f;

        /// <summary>Volume còn lại tại maxDistance mặc định (0.3 = 30%)</summary>
        public const float DEFAULT_VOLUME_AT_MAX = 0.3f;

        // ─────────────────────────────────────────────
        //  Volume
        // ─────────────────────────────────────────────

        /// <summary>Epsilon để so sánh volume (tránh float precision errors)</summary>
        public const float VOLUME_EPSILON = 0.001f;

        /// <summary>Epsilon để convert volume sang dB (tránh log(0) = -∞)</summary>
        public const float VOLUME_DAMPING = 0.0001f;

        /// <summary>Volume khi tắt hoàn toàn (dB)</summary>
        public const float MUTED_DB = -80f;

        // ─────────────────────────────────────────────
        //  Pool
        // ─────────────────────────────────────────────

        /// <summary>Số AudioSource tạo sẵn khi khởi động</summary>
        public const int DEFAULT_POOL_SIZE = 12;

        /// <summary>Số AudioSource tối đa (dynamic expansion)</summary>
        public const int MAX_POOL_SIZE = 50;

        // ─────────────────────────────────────────────
        //  Gizmo
        // ─────────────────────────────────────────────

        /// <summary>Số segment để vẽ vòng tròn debug</summary>
        public const int GIZMO_CIRCLE_SEGMENTS = 36;
    }
}
