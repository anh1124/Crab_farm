using System;
using Script.Data_Model;
using Script.Data_Model.Interfaces;
using UnityEngine;

namespace Script.Game.Item.Coin
{
    public enum ItemType { Coin, Tool_Bow, Tool_Hammer, Tool_Scythe }

    /*
     * VẬT PHẨM VẬT LÝ (Coin, Tools)
     * - Script chính chứa data: itemType, pickedBy, pickupAble flags
     * - Logic được tách ra các component riêng:
     *     + ItemFallBehavior: Xử lý rơi, ném, physics
     *     + ItemCooldownBehavior: Xử lý countdown cooldown
     * - Suck-back logic được quản lý bởi TownCenterCoinHandler
     * - Chứa ISaveable, IPickingUpMyItem implementation
     */
    [RequireComponent(typeof(ItemFallBehavior))]// logic rơi ném vật lý
    [RequireComponent(typeof(ItemCooldownBehavior))]// tự đếm coodown khi bị nhặt
    public class PhysicalItem : MonoBehaviour, ISaveable, IPickingUpMyItem
    {
        public ItemType itemType = ItemType.Coin;
        public GameObject pickedBy = null;
        public bool IsAvailable() => pickedBy == null;

        public bool playerPickupAble = true;
        public bool denizensPickupAble = true;
        public bool enemyPickupAble = false;
        public float pickupAbleCooldown = 3f;

        // Behavior components
        private ItemFallBehavior _fallBehavior;
        private ItemCooldownBehavior _cooldownBehavior;

        // Access to fall behavior state (for save/load)
        public bool floating
        {
            get => _fallBehavior != null ? _fallBehavior.floating : false;
            set { if (_fallBehavior != null) _fallBehavior.floating = value; }
        }

        // Event for TownCenter pickup system
        public event System.Action<PhysicalItem> OnReadyForTownCenterPickup
        {
            add { if (_fallBehavior != null) _fallBehavior.OnReadyForTownCenterPickup += value; }
            remove { if (_fallBehavior != null) _fallBehavior.OnReadyForTownCenterPickup -= value; }
        }

        // IPickingUpMyItem implementation
        #region IPickingUpMyItem implementation

        public GameObject AssignedCarrier { get; set; }

        public void OnPickedByStranger(GameObject stranger)
        {
            Script.Manager.MobsManager.ToolManager.Instance?.OnUnexpectedPickup(this, stranger);
        }

        public void OnAssigneeAbandoned()
        {
            if (pickedBy != null) return;
            AssignedCarrier = null;
            Script.Manager.MobsManager.ToolManager.Instance?.OnToolAbandoned(this);
        }
        #endregion

        private void Awake()
        {
            _fallBehavior = GetComponent<ItemFallBehavior>();
            _cooldownBehavior = GetComponent<ItemCooldownBehavior>();
        }

        private void Start()
        {
            _cooldownBehavior.RestartCooldownsIfNeeded();
        }

        private void OnEnable()
        {
            ResetState();

            if (itemType != ItemType.Coin && Script.Manager.MobsManager.ToolManager.Instance != null)
            {
                Script.Manager.MobsManager.ToolManager.Instance.RegisterTool(this);
            }
        }

        private void OnDisable()
        {
            if (itemType != ItemType.Coin && Script.Manager.MobsManager.ToolManager.Instance != null)
            {
                Script.Manager.MobsManager.ToolManager.Instance.UnregisterTool(this);
            }
        }

        private void Update()
        {
            _cooldownBehavior.UpdateCooldowns();
            _fallBehavior.UpdateFallPhysics();
        }

        public void ResetState()
        {
            pickedBy = null;
            playerPickupAble = true;
            denizensPickupAble = true;
            enemyPickupAble = false;

            _fallBehavior.ResetFallState();
            _cooldownBehavior.ResetCooldowns();
        }

        // --- Public methods delegated to behaviors ---

        public void CallFunctionCoolDown(string characterType)
        {
            CallFunctionCoolDown(characterType, pickupAbleCooldown);
        }

        public void CallFunctionCoolDown(string characterType, float cooldownSeconds)
        {
            _cooldownBehavior.CallFunctionCoolDown(characterType, cooldownSeconds);
        }

        public bool CanTownCenterPickup()
        {
            return _cooldownBehavior.CanTownCenterPickup();
        }

        public void SetTownCenterDropCooldown(float seconds)
        {
            _cooldownBehavior.SetTownCenterDropCooldown(seconds);
        }

        public void InitializeThrow(Vector2 direction, float throwForce, float throwDistance)
        {
            _fallBehavior.InitializeThrow(direction, throwForce, throwDistance);
        }

        public void StopRotate()
        {
            _fallBehavior.StopRotate();
        }

        public void StartRotate()
        {
            _fallBehavior.StartRotate();
        }

        // --- Save/Load ---

        [System.Serializable]
        public class PhysicalItemData
        {
            public float positionX;
            public float positionY;
            public bool floating;
            public string itemCategory;
        }

        public string GetPrefabID()
        {
            switch (itemType)
            {
                case ItemType.Coin: return "coin";
                case ItemType.Tool_Bow: return "bow";
                case ItemType.Tool_Hammer: return "hammer";
                default: return "coin";
            }
        }

        public object CaptureData()
        {
            return new PhysicalItemData
            {
                positionX = transform.position.x,
                positionY = transform.position.y,
                floating = floating,
                itemCategory = itemType.ToString()
            };
        }

        public void RestoreData(object data)
        {
            PhysicalItemData itemData = (PhysicalItemData)data;
            transform.position = new Vector3(itemData.positionX, itemData.positionY, 0);
            floating = false;

            if (System.Enum.TryParse(itemData.itemCategory, out ItemType parsedType))
            {
                itemType = parsedType;
            }

            StopRotate();
        }
    }
}