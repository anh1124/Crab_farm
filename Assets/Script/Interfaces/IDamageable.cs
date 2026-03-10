namespace Script.Interfaces
{
    /// <summary>
    /// Interface dành cho bất kỳ thực thể nào có khả năng nhận sát thương.
    /// Bên tấn công chỉ cần biết interface này, không quan tâm đến cơ chế
    /// bên trong (IHealth, ICoinShield, IToolShield...).
    /// Mỗi thực thể tự quyết định cách xử lý sát thương trong ReceiveDamage().
    /// </summary>
    public interface IDamageable
    {
        /// <summary>
        /// Nhận sát thương từ bên ngoài.
        /// Toàn bộ logic xử lý (rớt xu, rớt vũ khí, mất máu, chết...) 
        /// đều do thực thể này tự quyết định.
        /// </summary>
        /// <param name="damage">Lượng sát thương muốn gây ra</param>
        /// <param name="instigator">Đối tượng gây ra sát thương (có thể null)</param>
        void ReceiveDamage(int damage, UnityEngine.GameObject instigator);
    }
}
