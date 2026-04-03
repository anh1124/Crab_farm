using UnityEngine;
using Script.Game.Mobs.Denizen.Core;
using Script.Game.Item.Coin;
using Script.Game.Item;
using Script.Game.Mobs.Subjects;

namespace Script.Game.Mobs.Denizen.Jobs.Villager
{
    public class VillagerItemHandler : DenizenItemHandler
    {
        public VillagerItemHandler(DenizenContext context) : base(context) { }

        public override bool HandleItem(GameObject item)
        {
            var pItem = item.GetComponent<PhysicalItem>();
            if (pItem == null || !pItem.IsAvailable()) return false;

            // 1. Chuyển nghề khi nhặt vũ khí/công cụ (Logic Tiến Hóa) — Ưu tiên cao nhất
            if (pItem.itemType == ItemType.Tool_Bow)
            {
                Debug.Log($"[VillagerItemHandler] Picked up Bow! Requesting Archer role.");
                item.SetActive(false); // Ẩn vật phẩm ngay lập tức để né double-trigger
                Context.RoleController?.ChangeRole(NPCRole.Archer);
                return true;
            }
            else if (pItem.itemType == ItemType.Tool_Hammer)
            {
                Debug.Log($"[VillagerItemHandler] Picked up Hammer! Requesting Worker role.");
                item.SetActive(false);
                Context.RoleController?.ChangeRole(NPCRole.Worker);
                return true;
            }
            else if (pItem.itemType == ItemType.Tool_Scythe)
            {
                Debug.Log($"[VillagerItemHandler] Picked up Scythe! Requesting Farmer role.");
                item.SetActive(false);
                Context.RoleController?.ChangeRole(NPCRole.Farmer);
                return true;
            }

            // 2. Nếu là Tool mình đang được chỉ định nhặt (Gán từ ToolManager)
            if (pItem == Context.Status.currentTool)
            {
                Debug.Log($"[VillagerItemHandler] Picked up assigned tool: {pItem.itemType}");
                return true;
            }

            // 3. Nếu là Xu (Villager cũng nhặt được xu)
            if (pItem.itemType == ItemType.Coin && !Context.Status.IsFull)
            {
                Context.Status.AddCoin(1);
                return true;
            }

            return false;
        }
    }
}
