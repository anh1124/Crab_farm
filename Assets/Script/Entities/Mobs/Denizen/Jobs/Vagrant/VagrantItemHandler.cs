using UnityEngine;
using Script.Game.Mobs.Denizen.Core;
using Script.Game.Item.Coin;
using Script.Game.Item;
using Script.Game.Mobs.Subjects;

namespace Script.Game.Mobs.Denizen.Jobs.Vagrant
{
    public class VagrantItemHandler : DenizenItemHandler
    {
        public VagrantItemHandler(DenizenContext context) : base(context) { }

        public override bool HandleItem(GameObject item)
        {
            var coin = item.GetComponent<PhysicalItem>();
            if (coin != null && coin.itemType == ItemType.Coin && coin.IsAvailable())
            {
                Debug.Log($"[VagrantItemHandler] Coin detected at {coin.transform.position}. Picking up and changing role to Villager.");
                Context.Status.coinTarget = null; // Quan trọng: Clear target để tránh jitter
                Context.Status.AddCoin(1);

                // Đổi nghề sang Villager
                if (Context.RoleController != null)
                {
                    Context.RoleController.ChangeRole(NPCRole.Villager);
                }
                else
                {
                    Debug.LogError("[VagrantItemHandler] RoleController missing! Cannot change to Villager.");
                }
                return true;
            }
            return false;
        }

        public void OnCoinChanged(int newCoins)
        {
            if (newCoins > 0)
            {
                Context.RoleController.ChangeRole(NPCRole.Villager);
            }
        }
    }
}
