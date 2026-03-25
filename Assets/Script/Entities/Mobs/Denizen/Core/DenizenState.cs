using UnityEngine;

namespace Script.Game.Mobs.Denizen.Core
{
    /// <summary>
    /// Base class cho tất cả Denizen States.
    /// Plain C# class — không kế thừa MonoBehaviour → zero overhead khi inactive.
    /// Mỗi State chỉ làm đúng 1 việc.
    /// </summary>
    public abstract class DenizenState
    {
        /// <summary>
        /// Chạy 1 lần khi bắt đầu vào state. Override để init timer, set speed, reset biến.
        /// </summary>
        public virtual void Enter(DenizenContext ctx) { }

        /// <summary>
        /// AI logic tick — gọi qua NPCUpdateManager (~5-10Hz). Override bắt buộc.
        /// Chứa điều kiện chuyển state và decision logic.
        /// </summary>
        public abstract void Update(DenizenContext ctx);

        /// <summary>
        /// Smooth movement tick — gọi qua MonoBehaviour.Update (60Hz).
        /// Override nếu state cần smooth movement hoặc luôn cập nhật mỗi frame.
        /// Mặc định rỗng để tránh overhead.
        /// </summary>
        public virtual void FrameUpdate(DenizenContext ctx) { }

        /// <summary>
        /// Chạy 1 lần trước khi thoát state. Override để dọn dẹp, clear target, v.v.
        /// </summary>
        public virtual void Exit(DenizenContext ctx) { }

        /// <summary>
        /// Trả về chuỗi thông tin debug của State hiện tại.
        /// Thường gọi từ ContextMenu của Job.
        /// </summary>
        public virtual string GetDebugInfo(DenizenContext ctx) => "Chưa thiết lập debug info cho state này.";
    }
}
