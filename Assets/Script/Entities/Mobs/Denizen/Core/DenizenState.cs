using UnityEngine;

namespace Script.Game.Mobs.Denizen.Core
{
    /// <summary>
    /// DenizenState: Linh hồn (Soul) chia thành các trạng thái vận hành.
    /// CHỈ: Chứa các hành vi (Enter, Exit, Update).
    /// KHÔNG: Giữ dữ liệu, Không giữ tham chiếu Brain, Không giữ cờ di chuyển.
    /// Nếu cần biết về linh hồn, hãy hỏi thông qua Context.RoleController.
    /// </summary>
    public abstract class DenizenState
    {
        public virtual void Enter(DenizenContext ctx) { }
        public virtual void UpdateBehavior(DenizenContext ctx) { }
        public virtual void Update(DenizenContext ctx) { }
        public virtual void Exit(DenizenContext ctx) { }
    }
}
