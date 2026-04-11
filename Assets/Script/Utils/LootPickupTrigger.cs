using UnityEngine;
using Script.Data_Model;
using Script.Data_Model.Interfaces;
using Script.Game.Enemy;
using Script.Game.Item.Coin; // For PhysicalItem
using Script.Game.Player; // Sửa lỗi thiếu namespace PlayerBehavior

namespace Script.Utils
{
    public class LootPickupTrigger : MonoBehaviour
    {
        [Header("owner có thể nhặt loại item nào")]
        public bool canPickupCoin = true;
        public bool canPickupTool = true;
        public bool isDebug = false;

        private MonoBehaviour _ownerNpc;
        private IItemHandle _itemReceiver;
        private bool _isPlayer;
        private bool _isEnemy;
        private PlayerBehavior _playerBehavior; // Cache PlayerBehavior if it's the player
        private ICoinPool _coinPool;

        public void Setup(ICoinPool coinPool)
        {
            _coinPool = coinPool;
        }
        private void Awake()
        {
            // Tự động lấy script có chức năng xử lý item trong gameobject này hoặc parent cha
            _itemReceiver = GetComponentInParent<IItemHandle>();
            if (isDebug) 
                Debug.Log($"[LootPickup] Awake on {gameObject.name}. Found receiver: {(_itemReceiver != null ? (_itemReceiver as MonoBehaviour)?.name : "NULL")}");
            
            if (_itemReceiver != null)
            {
                _ownerNpc = _itemReceiver as MonoBehaviour;
                
                // Cache trạng thái phe phái để dùng trong CheckItemRule không tốn CPU
                _isPlayer = _ownerNpc.CompareTag(Script.Utils.GameTags.Player) || CompareTag(Script.Utils.GameTags.Player);
                _isEnemy = _ownerNpc.CompareTag(Script.Utils.GameTags.Enemy) || CompareTag(Script.Utils.GameTags.Enemy);
                
                if (_isPlayer)
                {
                    _playerBehavior = _ownerNpc.GetComponent<PlayerBehavior>();
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_ownerNpc == null || _itemReceiver == null)
            {
                if (isDebug)
                    Debug.LogWarning($"[LootPickup] {gameObject.name}: ownerNpc or itemReceiver is NULL — Awake may not have found IItemHandle.");
                return;
            }
            
            PhysicalItem drop = other.GetComponent<PhysicalItem>();
            if (drop == null) return;

            if (isDebug)
                Debug.Log($"[LootPickup] {_ownerNpc.name} touched item: {drop.name}, type={drop.itemType}, pickedBy={drop.pickedBy}, denizensPickupAble={drop.denizensPickupAble}");

            // 1. Kiểm tra trạng thái item
            bool rulePass = CheckItemRule(drop);
            if (isDebug)
                Debug.Log($"[LootPickup] CheckItemRule = {rulePass}, isPlayer={_isPlayer}, isEnemy={_isEnemy}");
            
            if (!rulePass) return;

            // Tối ưu cực mạnh cho Player: Nếu full túi thì skip gọi HandleItem luôn
            if (_isPlayer && _playerBehavior != null)
            {
                if (drop.itemType == ItemType.Coin && _playerBehavior.Coin >= _playerBehavior.MaxCoins)
                    return;
            }

            // 2. Trao quyền Quyết Định cho Não bộ NPC
            bool canPick = _itemReceiver.HandleItem(other.gameObject);
            if (isDebug)
                Debug.Log($"[LootPickup] {_ownerNpc.name} HandleItem returned: {canPick}");

            // 3. Nếu đồng ý nhận: đánh dấu & hủy vật phẩm
            if (canPick)
            {
                drop.pickedBy = _ownerNpc.gameObject;
                drop.StopAllCoroutines();
                
                // Vô hiệu hóa ngay lập tức để tránh double-trigger
                other.gameObject.SetActive(false);

                if (drop.itemType == ItemType.Coin && _coinPool != null)
                {
                    _coinPool.ReturnCoin(other.gameObject);
                }
                else
                {
                    Destroy(other.gameObject, 0.1f);
                }
            }
        }
        
        private bool CheckItemRule(PhysicalItem drop)
        {
            // Xác thực xem có ai đã đặt trước item này trong pickerby chưa
            if (drop.pickedBy != null) return false;

            // Kiểm tra thể loại và Cho phép
            if (drop.itemType == ItemType.Coin && !canPickupCoin) return false;
            if (drop.itemType != ItemType.Coin && !canPickupTool) return false;

            // Coin đang bay tới slot CoinUser: floating=true + chưa cho nhặt — không coi là loot rơi (collider cũng tắt lúc bay).
            if (drop.itemType == ItemType.Coin && drop.floating && !drop.playerPickupAble && !drop.denizensPickupAble) return false;

            if (_isPlayer && !drop.playerPickupAble) return false;
            if (_isEnemy && !drop.enemyPickupAble) return false;
            if (!_isPlayer && !_isEnemy && !drop.denizensPickupAble) return false;

            return true;
        }
    }
}
