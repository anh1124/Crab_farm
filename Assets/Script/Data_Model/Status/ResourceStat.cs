using System;
using UnityEngine;

namespace Script.Data_Model.Status
{
    [System.Serializable]
    public class ResourceStat
    {
        [SerializeField] private float _current;
        [SerializeField] private float _max;

        public float Current => _current;
        public float Max => _max;

        // ─── Events ───────────────────────────────────────────────────────────
        /// <summary>
        /// Bắn mỗi khi current hoặc max thay đổi (dùng để cập nhật fill % trên bar)
        /// (current, max)
        /// </summary>
        public event Action<float, float> OnChanged;

        /// <summary>
        /// Chỉ bắn khi giá trị bị trừ (phân biệt với regen)
        /// (amount)
        /// </summary>
        public event Action<float> OnReduced;

        /// <summary>
        /// Chỉ bắn khi giá trị được cộng (regen, pickup, buff, ...)
        /// (amount)
        /// </summary>
        public event Action<float> OnAdded;

        /// <summary>
        /// Bắn để trigger hiệu ứng UI nhất thời (shake, flash).
        /// Chỉ dành cho visual feedback — không đặt logic game vào handler này.
        /// Nếu cần nhiều loại effect hơn, cân nhắc tách sang UIFeedbackChannel riêng.
        /// </summary>
        public event Action OnUITrigger;

        // ─── API ──────────────────────────────────────────────────────────────
        
        /// <summary>
        /// Thay đổi giới hạn tối đa
        /// </summary>
        public void SetMax(float max, bool refillCurrent = false)
        {
            _max = Mathf.Max(max, 0f);                       // chặn max âm
            _current = refillCurrent
                ? _max
                : Mathf.Min(_current, _max);                 // clamp current về max mới
            OnChanged?.Invoke(_current, _max);
        }

        /// <summary>
        /// Cộng thêm (regen, heal, buff)
        /// </summary>
        public void Add(float amount)
        {
            if (amount <= 0) return;
            _current = Mathf.Min(_current + amount, _max);
            OnAdded?.Invoke(amount);
            OnChanged?.Invoke(_current, _max);
        }

        /// <summary>
        /// Trừ đi (damage, tiêu thụ skill)
        /// </summary>
        public void Reduce(float amount)
        {
            if (amount <= 0) return;
            _current = Mathf.Max(_current - amount, 0f);
            OnReduced?.Invoke(amount);
            OnChanged?.Invoke(_current, _max);
        }

        /// <summary>
        /// Trigger hiệu ứng UI (không thay đổi giá trị)
        /// </summary>
        public void TriggerUI()
        {
            OnUITrigger?.Invoke();
        }
    }
}
