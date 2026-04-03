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
                Context.Status.coinTarget = null;
                // Thêm xu vào túi cư dân
                Context.Status.AddCoin(1);

                // Đổi nghề sang Villager
                Context.RoleController?.ChangeRole(NPCRole.Villager);
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
