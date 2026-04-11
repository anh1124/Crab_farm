using System;

namespace Script.Interfaces
{
    // Quản lý máu
    public interface IHealth
    {
        float CurrentHp { get; set; }
        float MaxHp { get; set; }

        void TakeDamage(float amount);
        void Die();
        void Heal(float amount);
    }

    /// <summary>
    /// Tuỳ chọn: phát sự kiện khi máu thay đổi / nhận sát thương để UI (ví dụ HPText) lắng nghe.
    /// Không phải mọi <see cref="IHealth"/> đều cần triển khai.
    /// </summary>
    public interface IHealthReactive
    {
        event Action<float, float> OnHealthChanged;
        event Action<float> OnTakeDamageEvent;
    }
}