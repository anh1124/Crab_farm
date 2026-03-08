using UnityEngine;
using Script.Data_Model;
using Script.Data_Model.Interfaces;
using Script.Game.Enemy;
using Script.Game.Item.Coin; // For PhysicalItem

namespace Script.Utils
{
    public class LootPickupTrigger : MonoBehaviour
    {
        [Header("owner có thể nhặt loại item nào")]
        public bool canPickupCoin = true;
        public bool canPickupTool = true;

        //NPC sở hữu logic pickup này. Tự động lấy ở GetComponentInParent nếu để trống
        private MonoBehaviour _ownerNpc;
        private IItemHandle _itemReceiver;
        
        private void Awake()
        {
            // Tự động lấy script có chức năng xử lý item trong gameobject này hoặc parent cha
            _itemReceiver = GetComponentInParent<IItemHandle>();
            if (_itemReceiver != null)
            {
                _ownerNpc = _itemReceiver as MonoBehaviour;
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
            {
              //  Debug.Log($"[LootPickupTrigger] {_ownerNpc.name} check rule failed for {drop.gameObject.name}");
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

            // Suy luận Phe từ Tag để áp dụng Cooldown nhặt
            bool isPlayer = CompareTag("Player") || (_ownerNpc != null && _ownerNpc.CompareTag("Player"));
            bool isEnemy = CompareTag("Enemy") || (_ownerNpc != null && _ownerNpc.CompareTag("Enemy"));
            
            if (isPlayer && !drop.playerPickupAble) return false;
            if (isEnemy && !drop.enemyPickupAble) return false;
            if (!isPlayer && !isEnemy && !drop.denizensPickupAble) return false;

            return true;
        }
    }
}
