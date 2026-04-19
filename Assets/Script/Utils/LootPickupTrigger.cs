using UnityEngine;
using Script.Data_Model;
using Script.Data_Model.Interfaces;
using Script.Game.Enemy;
using Script.Game.Item.Coin;
using Script.Game.Player;
using Script.Audio;

namespace Script.Utils
{
    public class LootPickupTrigger : MonoBehaviour
    {
        #region Inspector Fields

        [Header("Loại Item Có Thể Nhặt")]
        public bool canPickupCoin = true;
        public bool canPickupTool = true;

        [Header("Chế Độ Tự Động")]
        [Tooltip("Bật để tự động nhặt item khi chạm, bỏ qua IItemHandle (chỉ cần CheckItemRule pass).")]
        public bool autoPickupWithoutReceiver = false;

        [Header("Debug")]
        public bool isDebug = false;

        #endregion

        #region Private Fields

        private MonoBehaviour _ownerNpc;         // Đối tượng sở hữu trigger (thường là NPC/Player)
        private IItemHandle _itemReceiver;      // Bộ não xử lý item (nếu có)
        private bool _isPlayer;
        private bool _isEnemy;
        private PlayerBehavior _playerBehavior; // Cache riêng cho Player để kiểm tra túi đầy
        private ICoinPool _coinPool;
        private AudioEmitter _audioEmitter;

        #endregion

        #region Initialization

        public void Setup(ICoinPool coinPool)
        {
            _coinPool = coinPool;
        }

        private void Awake()
        {
            CacheReceiverAndOwner();
            CacheFactionFlags();
            // Get AudioEmitter from owner (parent) since trigger is on child GameObject
            if (_ownerNpc != null)
                _audioEmitter = _ownerNpc.GetComponent<AudioEmitter>();
        }

        /// <summary>
        /// Tìm IItemHandle trên GameObject hoặc parent, gán _ownerNpc và _itemReceiver.
        /// </summary>
        private void CacheReceiverAndOwner()
        {
            _itemReceiver = GetComponentInParent<IItemHandle>();

            if (isDebug)
                Debug.Log($"[LootPickup] Awake on {gameObject.name}. Receiver: {(_itemReceiver as MonoBehaviour)?.name ?? "NULL"}");

            if (_itemReceiver != null)
            {
                _ownerNpc = _itemReceiver as MonoBehaviour;
            }
            // Nếu không có receiver, _ownerNpc giữ null -> sẽ không thể hỏi ý kiến, chỉ dùng auto mode.
        }

        /// <summary>
        /// Xác định phe (Player/Enemy/Denizen) dựa trên tag của _ownerNpc.
        /// </summary>
        private void CacheFactionFlags()
        {
            if (_ownerNpc == null) return;

            _isPlayer = _ownerNpc.CompareTag(GameTags.Player);
            _isEnemy = _ownerNpc.CompareTag(GameTags.Enemy);

            if (_isPlayer)
            {
                _playerBehavior = _ownerNpc.GetComponent<PlayerBehavior>();
            }
        }

        #endregion

        #region Trigger Handling

        private void OnTriggerEnter2D(Collider2D other)
        {
            PhysicalItem drop = other.GetComponent<PhysicalItem>();
            if (drop == null) return;

            if (isDebug)
                Debug.Log($"[LootPickup] {GetOwnerName()} touched {drop.name}");

            // 1. Kiểm tra điều kiện cơ bản từ item
            if (!CheckItemRule(drop)) return;

            // 2. Kiểm tra đặc biệt cho Player (full coin)
            if (IsPlayerFullOfCoin(drop)) return;

            // 3. Quyết định có nhặt hay không
            bool canPick = DecidePickup(drop.gameObject);
            if (!canPick) return;

            // 4. Thực hiện nhặt
            ExecutePickup(drop, other.gameObject);
        }

        /// <summary>
        /// Kiểm tra nếu là Player và đã đầy Coin thì bỏ qua.
        /// </summary>
        private bool IsPlayerFullOfCoin(PhysicalItem drop)
        {
            if (!_isPlayer || _playerBehavior == null) return false;
            if (drop.itemType != ItemType.Coin) return false;

            return _playerBehavior.Coin >= _playerBehavior.MaxCoins;
        }

        /// <summary>
        /// Quyết định nhặt dựa trên chế độ auto hoặc hỏi IItemHandle.
        /// </summary>
        private bool DecidePickup(GameObject itemObject)
        {
            if (autoPickupWithoutReceiver)
            {
                return true; // Luôn nhặt nếu rule cho phép
            }

            // Cần có IItemHandle để hỏi
            if (_itemReceiver == null)
            {
                if (isDebug)
                    Debug.LogWarning($"[LootPickup] {gameObject.name}: Không có IItemHandle mà autoPickupWithoutReceiver = false -> không thể nhặt.");
                return false;
            }

            bool result = _itemReceiver.HandleItem(itemObject);
            if (isDebug)
                Debug.Log($"[LootPickup] {GetOwnerName()} HandleItem returned: {result}");

            return result;
        }

        /// <summary>
        /// Đánh dấu item đã được nhặt và hủy nó (qua Pool hoặc Destroy).
        /// </summary>
        private void ExecutePickup(PhysicalItem drop, GameObject itemObject)
        {
            Debug.Log($"[LootPickup] ExecutePickup called. itemType={drop.itemType}, _coinPool={_coinPool != null}");

            // Đánh dấu chủ sở hữu (nếu có _ownerNpc, ngược lại lấy gameObject của trigger)
            drop.pickedBy = _ownerNpc != null ? _ownerNpc.gameObject : gameObject;
            drop.StopAllCoroutines();
            itemObject.SetActive(false);

            // Phát âm thanh khi nhặt coin (cho cả Player, Denizen, Enemy)
            if (drop.itemType == ItemType.Coin)
            {
                Debug.Log($"[LootPickup] Picking up coin. _audioEmitter={_audioEmitter != null}, owner={_ownerNpc?.name}");
                if (_audioEmitter != null)
                    _audioEmitter.PlaySound("Coin_Pickup", PlaySoundConfig.Default);
                else
                    Debug.LogWarning("[LootPickup] _audioEmitter is null! Cannot play Coin_Pickup sound.");
            }

            if (drop.itemType == ItemType.Coin)
            {
                // Return coin to pool - use FindObjectOfType if _coinPool not set (for Denizens/Enemies)
                ICoinPool pool = _coinPool;
                if (pool == null)
                    pool = FindObjectOfType<Script.Game.Item.Coin.CoinPool>();

                if (pool != null)
                {
                    pool.ReturnCoin(itemObject);
                    Debug.Log($"[LootPickup] Coin returned to pool");
                }
                else
                {
                    Debug.LogWarning("[LootPickup] CoinPool not found! Destroying coin.");
                    Destroy(itemObject, 0.1f);
                }
            }
            else
            {
                Debug.Log($"[LootPickup] Not coin. itemType={drop.itemType}");
                Destroy(itemObject, 0.1f);
            }
        }

        private string GetOwnerName()
        {
            return _ownerNpc != null ? _ownerNpc.name : gameObject.name;
        }

        #endregion

        #region Rule Checking

        /// <summary>
        /// Kiểm tra các quy tắc cơ bản: ai được nhặt, loại item, trạng thái đặc biệt.
        /// </summary>
        private bool CheckItemRule(PhysicalItem drop)
        {
            // Đã có chủ nhặt trước
            if (drop.pickedBy != null)
            {
                if (isDebug) Debug.Log($"[LootPickup] CheckItemRule FAIL: drop.pickedBy = {drop.pickedBy.name}");
                return false;
            }

            // Loại item có được phép không
            if (drop.itemType == ItemType.Coin && !canPickupCoin)
            {
                if (isDebug) Debug.Log($"[LootPickup] CheckItemRule FAIL: Coin nhưng canPickupCoin = false");
                return false;
            }
            if (drop.itemType != ItemType.Coin && !canPickupTool)
            {
                if (isDebug) Debug.Log($"[LootPickup] CheckItemRule FAIL: Tool nhưng canPickupTool = false");
                return false;
            }

            // Coin đang bay (floating) và chưa cho phép nhặt
            if (drop.itemType == ItemType.Coin && drop.floating && !drop.playerPickupAble && !drop.denizensPickupAble)
            {
                if (isDebug) Debug.Log($"[LootPickup] CheckItemRule FAIL: Coin floating và không cho phép nhặt");
                return false;
            }

            // Quyền nhặt theo phe
            if (_isPlayer && !drop.playerPickupAble)
            {
                if (isDebug) Debug.Log($"[LootPickup] CheckItemRule FAIL: Player nhưng playerPickupAble = false");
                return false;
            }
            if (_isEnemy && !drop.enemyPickupAble)
            {
                if (isDebug) Debug.Log($"[LootPickup] CheckItemRule FAIL: Enemy nhưng enemyPickupAble = false. _isEnemy={_isEnemy}, enemyPickupAble={drop.enemyPickupAble}");
                return false;
            }
            if (!_isPlayer && !_isEnemy && !drop.denizensPickupAble)
            {
                if (isDebug) Debug.Log($"[LootPickup] CheckItemRule FAIL: Denizen nhưng denizensPickupAble = false");
                return false;
            }

            if (isDebug) Debug.Log($"[LootPickup] CheckItemRule PASS");
            return true;
        }

        #endregion
    }
}