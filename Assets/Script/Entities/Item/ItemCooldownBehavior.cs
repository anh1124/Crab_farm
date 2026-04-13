using UnityEngine;
using Script.Game.Item.Coin;
using Script.Data_Model;

namespace Script.Game.Item
{
    /// <summary>
    /// Handles pickup cooldown timers for PhysicalItem.
    /// Uses float timers instead of coroutines for zero GC allocation.
    /// </summary>
    [RequireComponent(typeof(PhysicalItem))]
    public class ItemCooldownBehavior : MonoBehaviour
    {
        private PhysicalItem _physicalItem;
        private Collider2D _col;

        [Header("Pickup Cooldown Timers (Debug)")]
        [SerializeField] private float _playerPickupCooldown = 0f;
        [SerializeField] private float _denizenPickupCooldown = 0f;
        [SerializeField] private float _enemyPickupCooldown = 0f;
        [SerializeField] private float _townCenterPickupCooldown = 0f;
        [SerializeField] private float _townCenterPickupCooldownDebug = 0f;

        public float PlayerPickupCooldown => _playerPickupCooldown;
        public float DenizenPickupCooldown => _denizenPickupCooldown;
        public float EnemyPickupCooldown => _enemyPickupCooldown;
        public float TownCenterPickupCooldown => _townCenterPickupCooldown;

        private void Awake()
        {
            _physicalItem = GetComponent<PhysicalItem>();
            _col = GetComponent<Collider2D>();
        }

        private void Start()
        {
            _physicalItem.pickupAbleCooldown = PrefabData.Instance.timeMaxFloatCoin;
        }

        public void UpdateCooldowns()
        {
            bool anyExpired = false;

            if (_playerPickupCooldown > 0f)
            {
                _playerPickupCooldown -= Time.deltaTime;
                if (_playerPickupCooldown <= 0f)
                {
                    _playerPickupCooldown = 0f;
                    _physicalItem.playerPickupAble = true;
                    anyExpired = true;
                }
            }

            if (_denizenPickupCooldown > 0f)
            {
                _denizenPickupCooldown -= Time.deltaTime;
                if (_denizenPickupCooldown <= 0f)
                {
                    _denizenPickupCooldown = 0f;
                    _physicalItem.denizensPickupAble = true;
                    anyExpired = true;
                }
            }

            if (_enemyPickupCooldown > 0f)
            {
                _enemyPickupCooldown -= Time.deltaTime;
                if (_enemyPickupCooldown <= 0f)
                {
                    _enemyPickupCooldown = 0f;
                    _physicalItem.enemyPickupAble = true;
                    anyExpired = true;
                }
            }

            if (_townCenterPickupCooldown > 0f)
            {
                _townCenterPickupCooldown -= Time.deltaTime;
                if (_townCenterPickupCooldown <= 0f)
                {
                    _townCenterPickupCooldown = 0f;
                }
            }
            _townCenterPickupCooldownDebug = _townCenterPickupCooldown;

            // Toggle collider to re-fire OnTriggerEnter2D when cooldown expires
            if (anyExpired && _col != null)
            {
                StartCoroutine(ToggleColliderNextFrame());
            }
        }

        private System.Collections.IEnumerator ToggleColliderNextFrame()
        {
            if (_col != null)
            {
                _col.enabled = false;
            }
            yield return null;
            if (_col != null)
            {
                _col.enabled = true;
            }
        }

        public void CallFunctionCoolDown(string characterType, float cooldownSeconds)
        {
            float cooldown = Mathf.Max(0f, cooldownSeconds);
            switch (characterType)
            {
                case "Player":
                    _physicalItem.playerPickupAble = false;
                    _playerPickupCooldown = cooldown;
                    break;
                case "Denizens":
                    _physicalItem.denizensPickupAble = false;
                    _denizenPickupCooldown = cooldown;
                    break;
                case "Enemy":
                    _physicalItem.enemyPickupAble = false;
                    _enemyPickupCooldown = cooldown;
                    break;
            }
        }

        public void SetTownCenterDropCooldown(float seconds)
        {
            _townCenterPickupCooldown = Mathf.Max(0f, seconds);
        }

        public bool CanTownCenterPickup()
        {
            return _townCenterPickupCooldown <= 0f;
        }

        public void RestartCooldownsIfNeeded()
        {
            if (!_physicalItem.playerPickupAble && _playerPickupCooldown <= 0f)
                _playerPickupCooldown = _physicalItem.pickupAbleCooldown;
            if (!_physicalItem.denizensPickupAble && _denizenPickupCooldown <= 0f)
                _denizenPickupCooldown = _physicalItem.pickupAbleCooldown;
        }

        public void ResetCooldowns()
        {
            _playerPickupCooldown = 0f;
            _denizenPickupCooldown = 0f;
            _enemyPickupCooldown = 0f;
            _townCenterPickupCooldown = 0f;
        }
    }
}
