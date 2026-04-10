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
                bool isFearMode = IsFearMode();
                bool isPlayerThrownCoin = IsPlayerThrownCoin(coin);

                // FEAR MODE:
                // - Không nhặt coin thường
                // - Chỉ nhặt coin do player ném (playerPickupAble == false)
                if (isFearMode && !isPlayerThrownCoin)
                    return false;

                // NON-FEAR MODE:
                // - Chỉ xử lý coin dành cho denizen (coin thường)
                if (!isFearMode && !coin.denizensPickupAble)
                    return false;

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

        private bool IsFearMode()
        {
            if (Context?.Status == null) return false;
            return Context.Status.isInDanger || Context.Status.isFearPending;
        }

        private bool IsPlayerThrownCoin(PhysicalItem coin)
        {
            if (coin == null) return false;
            // Quy ước hiện tại trong project:
            // coin do player ném ra -> playerPickupAble = false
            return coin.playerPickupAble == false;
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
