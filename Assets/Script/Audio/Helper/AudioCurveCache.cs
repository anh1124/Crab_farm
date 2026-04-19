using UnityEngine;

namespace Script.Audio
{
    /// <summary>
    /// Cache AnimationCurve để tránh tạo mới mỗi lần phát âm thanh.
    /// AnimationCurve được tạo dựa trên minDistance và maxDistance.
    /// </summary>
    public static class AudioCurveCache
    {
        private static readonly System.Collections.Generic.Dictionary<string, AnimationCurve> _curveCache
            = new System.Collections.Generic.Dictionary<string, AnimationCurve>();

        /// <summary>
        /// Lấy AnimationCurve rolloff mặc định cho khoảng cách cụ thể.
        /// Cache curve để tránh tạo mới mỗi lần.
        /// </summary>
        /// <param name="minDistance">Khoảng cách tối đa để nghe to nhất</param>
        /// <param name="maxDistance">Khoảng cách âm thanh hoàn toàn im</param>
        /// <param name="volumeAtMax">Volume còn lại tại maxDistance (mặc định 0.3 = 30%)</param>
        /// <returns>AnimationCurve đã được cache hoặc mới tạo</returns>
        public static AnimationCurve GetDefaultRolloffCurve(float minDistance, float maxDistance, float volumeAtMax = AudioConstants.DEFAULT_VOLUME_AT_MAX)
        {
            // Tạo key cache dựa trên tham số
            string cacheKey = $"{minDistance:F2}_{maxDistance:F2}_{volumeAtMax:F2}";

            if (_curveCache.TryGetValue(cacheKey, out var cachedCurve))
            {
                return cachedCurve;
            }

            // Tạo curve mới
            AnimationCurve curve = new AnimationCurve();
            curve.AddKey(0f, 1f);
            curve.AddKey(minDistance, 1f);
            curve.AddKey(maxDistance, volumeAtMax);

            _curveCache[cacheKey] = curve;
            return curve;
        }

        /// <summary>
        /// Xóa toàn bộ cache (dùng khi chuyển scene hoặc cleanup).
        /// </summary>
        public static void ClearCache()
        {
            _curveCache.Clear();
        }
    }
}
