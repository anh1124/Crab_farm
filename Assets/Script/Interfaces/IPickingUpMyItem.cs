using UnityEngine;

namespace Script.Game.Item
{
    /// <summary>
    /// Item tự biết ai là người được giao đến nhặt nó.
    /// Khi người khác nhặt trước, item báo lên manager.
    /// </summary>
    public interface IPickingUpMyItem
    {
        /// <summary>GameObject của Villager được giao nhiệm vụ nhặt item này.</summary>
        GameObject AssignedCarrier { get; set; }

        /// <summary>
        /// Gọi khi một người KHÔNG phải AssignedCarrier nhặt được item này.
        /// Item sẽ báo lên ToolManager để xử lý tool bị bỏ rơi.
        /// </summary>
        void OnPickedByStranger(GameObject stranger);
    }
}
