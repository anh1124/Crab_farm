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

        private MonoBehaviour _ownerNpc;
        private IItemHandle _itemReceiver;
        private bool _isPlayer;
        private bool _isEnemy;
        private PlayerBehavior _playerBehavior; // Cache PlayerBehavior if it's the player
        
        private void Awake()
        {
            // Tự động lấy script có chức năng xử lý item trong gameobject này hoặc parent cha
            _itemReceiver = GetComponentInParent<IItemHandle>();
            if (_itemReceiver != null)
            {
                _ownerNpc = _itemReceiver as MonoBehaviour;
                
                // Cache trạng thái phe phái để dùng trong CheckItemRule không tốn CPU
                _isPlayer = _ownerNpc.CompareTag("Player") || CompareTag("Player");
                _isEnemy = _ownerNpc.CompareTag("Enemy") || CompareTag("Enemy");
                
                if (_isPlayer)
                {
                    _playerBehavior = _ownerNpc.GetComponent<PlayerBehavior>();
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_ownerNpc == null || _itemReceiver == null) return;
            
            PhysicalItem drop = other.GetComponent<PhysicalItem>();
            if (drop == null) return;

          //  Debug.Log($"[LootPickupTrigger] TriggerEnter for {_ownerNpc.name}. Item: {drop.gameObject.name}, type: {drop.itemType}");

            // 1. Kiểm tra trạng thái item xem có được nhặt không (FaceID, Cooldown, Thể Loại)
            if (!CheckItemRule(drop)) 
                return;

            // Tối ưu cực mạnh cho Player: Nếu full túi thì skip gọi HandleItem luôn
            if (_isPlayer && _playerBehavior != null)
            {
                if (drop.itemType == ItemType.Coin && _playerBehavior.Coin >= _playerBehavior.MaxCoins)
                    return;
            }

            // 2. Trao quyền Quyết Định (Decision Making) cho Não bộ NPC
            bool canPick = _itemReceiver.HandleItem(other.gameObject);
          //  Debug.Log($"[LootPickupTrigger] {_ownerNpc.name} HandleItem returned: {canPick}");

            // 3. Nếu Não bộ đồng ý nhận: Tiến hành đánh dấu chủ quyền & Hủy vật phẩm
            if (canPick)
            {
                drop.pickedBy = _ownerNpc.gameObject;
                drop.StopAllCoroutines();
                other.gameObject.SetActive(false); // Hoặc Destroy(other.gameObject);
                Destroy(other.gameObject, 0.1f);
            }
        }
        
        private bool CheckItemRule(PhysicalItem drop)
        {
            // Xác thực xem có ai đã đặt trước item này trong pickerby chưa
            if (drop.pickedBy != null) return false;

            // Kiểm tra thể loại và Cho phép
            if (drop.itemType == ItemType.Coin && !canPickupCoin) return false;
            if (drop.itemType != ItemType.Coin && !canPickupTool) return false;

            if (_isPlayer && !drop.playerPickupAble) return false;
            if (_isEnemy && !drop.enemyPickupAble) return false;
            if (!_isPlayer && !_isEnemy && !drop.denizensPickupAble) return false;

            return true;
        }
    }
}
