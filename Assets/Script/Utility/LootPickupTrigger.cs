using UnityEngine;
using Script.Data_Model;
using Script.Data_Model.Interfaces;
using Script.Game.Enemy;
using Script.Game.Item.Coin; // For PhysicalItem

namespace Script.Utils
{
    public class LootPickupTrigger : MonoBehaviour
    {
        public bool canPickupCoin = true;
        public bool canPickupTool = true;

        [Tooltip("NPC sở hữu logic pickup này. Tự động lấy ở GetComponentInParent nếu để trống")]
        [SerializeField] private MonoBehaviour _ownerNpc;

        private void Awake()
        {
            if (_ownerNpc == null)
            {
                // Fallback to searching component if left unassigned
                var receiver = GetComponentInParent<IItemHandle>();
                if (receiver != null)
                {
                    _ownerNpc = receiver as MonoBehaviour;
                }
                else
                {
                    _ownerNpc = GetComponentInParent<ICoinable>() as MonoBehaviour;
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Kiểm tra quyền nhặt Item theo loại
            PhysicalItem drop = other.GetComponent<PhysicalItem>();
            if (drop != null)
            {
                if (drop.itemCategory == ItemCategory.Coin && !canPickupCoin) return;
                if (drop.itemCategory != ItemCategory.Coin && !canPickupTool) return;

                // Suy luận Phe từ Tag để áp dụng Cooldown nhặt
                bool isPlayer = CompareTag("Player") || (_ownerNpc != null && _ownerNpc.CompareTag("Player"));
                bool isEnemy = CompareTag("Enemy") || (_ownerNpc != null && _ownerNpc.CompareTag("Enemy"));
                
                if (isPlayer && !drop.playerPickupAble) return;
                else if (isEnemy && !drop.enemyPickupAble) return;
                else if (!isPlayer && !isEnemy && !drop.denizensPickupAble) return;
            }

            // Kiểm tra list loot tag
            bool isEnemyLoot = CompareTag("Enemy") || (_ownerNpc != null && _ownerNpc.CompareTag("Enemy"));
            if (isEnemyLoot && PrefabData.Instance != null && PrefabData.Instance.enemyLootTags != null)
            {
                if (!PrefabData.Instance.enemyLootTags.Contains(other.tag)) return;
            }

            // Ưu tiên check IItemHandle mới (DenizenRoleController/PlayerBehavior)
            IItemHandle itemReceiver = _ownerNpc as IItemHandle;
            if (itemReceiver != null)
            {
                if (drop != null) drop.StopAllCoroutines();
                itemReceiver.HandleItem(other.gameObject);
                return;
            }

            // Kiểm tra NPC có hỗ trợ ICoinable không
            ICoinable coinReceiver = _ownerNpc as ICoinable;
            if (coinReceiver == null)
            {
                Debug.LogWarning($"[LootPickupTrigger] {gameObject.name}: Owner NPC does not implement ICoinable!");
                return;
            }

            // [Tùy enemy context] Nếu gắn lên enemy, verify greed>0
            EnemyAIController controller = _ownerNpc.GetComponent<EnemyAIController>();
            if (controller != null)
            {
                // Shieldbot ví dụ Greed=0, vậy không loot
                // lấy GreedThreshold từ EnemyStatus / Context nếu cần
                // Hiện tại controller.ResetEnemy setup context, nhưng ta chưa public ctx ra.
                // Thêm public cho GreedThreshold trong Status hoặc public GetCtx() nếu shieldbot cần.
                // Dưới đây chỉ giả định check sơ bộ qua Status nếu thêm public:
                EnemyStatus status = _ownerNpc.GetComponent<EnemyStatus>();
                // tạm thời bỏ qua bước check GreedThreshold từ status. 
                // ShieldBot không được gắn script này ở prefab là được.
            }

            // Thực hiện PickUpCoin logic
            if (drop != null) drop.StopAllCoroutines();
            coinReceiver.PickUpCoin(other.gameObject);
        }
    }
}
