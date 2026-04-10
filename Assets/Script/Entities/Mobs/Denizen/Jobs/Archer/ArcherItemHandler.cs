using UnityEngine;
using Script.Game.Mobs.Denizen.Core;
using Script.Game.Item.Coin;
using Script.Data_Model;
using Script.Game.Item;
using Script.Game.Mobs.Subjects;

namespace Script.Game.Mobs.Denizen.Jobs.Archer
{
    public class ArcherItemHandler : DenizenItemHandler
    {
        public ArcherItemHandler(DenizenContext context) : base(context) { }

        public override bool HandleItem(GameObject item)
        {
            var pItem = item.GetComponent<PhysicalItem>();
            if (pItem == null || !pItem.IsAvailable()) return false;

            // Archer nhặt xu
            if (pItem.itemType == ItemType.Coin && !Context.Status.IsFull)
            {
                Context.Status.AddCoin(1);
                return true;
            }

            return false;
        }

        public void ProcessDamage(float damage, GameObject instigator)
        {
            if (damage <= 0) return;
            
            if (Context.Status.CurrentCoins > 1)
            {
                Context.Status.SubtractCoin(1);
                global::Spawner.Instance?.DropByDenizen(Context.transform.position, 1, Vector2.up);
            }
            else
            {
                DropBow();
                Context.Status.isFearPending = true;
                Context.RoleController.ChangeRole(NPCRole.Villager);
            }
        }

        public void OnCoinChanged(int newCoins)
        {
            if (newCoins <= 0)
            {
                DropBow();
                Context.RoleController.ChangeRole(NPCRole.Vagrant);
            }
        }

        public void DropBow()
        {
            if (PrefabData.Instance?.bow != null)
            {
                var spawner = global::Spawner.Instance != null ? global::Spawner.Instance : Object.FindObjectOfType<global::Spawner>();
                if (spawner != null)
                {
                    spawner.DropItem(
                        PrefabData.Instance.bow,
                        Context.transform.position,
                        playerPickupAble: true,
                        denizensPickupAble: true
                    );
                }
                else
                {
                    // Fallback nếu Spawner chưa sẵn sàng.
                    var bow = Object.Instantiate(PrefabData.Instance.bow, Context.transform.position + Vector3.up, Quaternion.identity);
                    var pItem = bow.GetComponent<PhysicalItem>();
                    if (pItem != null) pItem.CallFunctionCoolDown("Denizens");
                }
            }
        }
    }
}
