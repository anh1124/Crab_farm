using System;
using System.Collections.Generic;
using UnityEngine;

namespace Script.Audio
{
    /// <summary>
    /// Registry trung tâm — lưu danh sách tất cả AudioEmitter đang active trong Scene.
    /// Class tĩnh thuần: không cần MonoBehaviour, không cần GameObject, không cần DontDestroyOnLoad.
    ///
    /// Tại sao static thay vì Singleton MonoBehaviour?
    ///   - Không cần setup trong Editor
    ///   - Không bị phụ thuộc thứ tự Awake()
    ///   - AudioEmitter tự đăng ký/hủy đăng ký trong OnEnable/OnDisable
    ///
    /// Lưu ý Scene transition:
    ///   Khi load Scene mới, các AudioEmitter bị Destroy → OnDestroy → Unregister tự động.
    ///   _all sẽ tự dọn sạch. Không cần gọi Clear() thủ công.
    /// </summary>
    public static class AudioEmitterRegistry
    {
        private static readonly List<AudioEmitter> _all = new List<AudioEmitter>();

        /// <summary>
        /// Fired ngay sau khi một AudioEmitter mới được đăng ký.
        /// PlayerAudioZone subscribe để check ngay lập tức thay vì chờ bucket cycle.
        /// </summary>
        public static event Action<AudioEmitter> OnEmitterRegistered;

        /// <summary>Đăng ký emitter khi được enable. Gọi từ AudioEmitter.OnEnable().</summary>
        public static void Register(AudioEmitter emitter)
        {
            if (emitter != null && !_all.Contains(emitter))
            {
                _all.Add(emitter);
                OnEmitterRegistered?.Invoke(emitter);
            }
        }

        /// <summary>Hủy đăng ký khi disable hoặc destroy. Gọi từ AudioEmitter.OnDisable/OnDestroy().</summary>
        public static void Unregister(AudioEmitter emitter)
        {
            _all.Remove(emitter);
        }

        /// <summary>Toàn bộ danh sách emitter đang active — dùng bởi PlayerAudioZone.</summary>
        public static IReadOnlyList<AudioEmitter> All => _all;

        /// <summary>Debug: số lượng emitter đang đăng ký.</summary>
        public static int Count => _all.Count;
    }
}
