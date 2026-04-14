using System;
using System.Collections.Generic;
using Script.Data_Model;
using Script.Data_Model.Interfaces;
using Script.Game.Other.Nature;
using Script.Game.Other.Nature.States;
using Script.Manager.GamePlayManager;
using Script.Manager.SaveManager;
using Script.Utils;
using UnityEngine;

namespace Script.Game.Other.Nature
{
    public class RabbitBehavior : MonoBehaviour, ISaveable, INPCBehavior, Script.Interfaces.IDamageable, Script.Interfaces.IDie
    {
        [Serializable]
        public class RabbitSaveData
        {
            public float positionX;
            public float positionY;
            public int hp;
            public string bushID;
            public float targetPositionX;
            public float targetPositionY;
        }

        [Header("Settings")]
        [SerializeField] private int hp = 1;
        [SerializeField] private int speed = 3;

        [Header("Death Settings")]
        [SerializeField] private float destroyDelay = 3f;

        [Header("Movement Flags")]
        [SerializeField] private bool canMove = true;
        [SerializeField] private bool isMove = false;

        [Header("Debug")]
        [SerializeField] private bool isDebug = false;

        [Header("Read Only")]
        [SerializeField] private string _bushID;

        private Animator _animator;
        private RabbitBushBehavior _rabbitBush;

        private bool _isDead = false;
        private bool _isDying = false;      // True khi đã bắt đầu die, bảo vệ DropCoin khỏi double-call
        private bool _coinDropped = false;  // Guard flag: đảm bảo tiền chỉ rơi 1 lần

        public bool IsDead => _isDead;

        private RabbitState _currentState;

        public string BushID => _bushID;

        // Properties for state access
        public bool IsDebug => isDebug;
        public int Speed => speed;
        public RabbitBushBehavior RabbitBush => _rabbitBush;
        public bool CanMove => canMove;
        public bool IsMove { get => isMove; set => isMove = value; }

        [SerializeField] private string _currentStateName = "None";
        [SerializeField] private Vector3 _currentTargetPosition = Vector3.zero;

        public void SetTargetPosition(Vector3 target)
        {
            _currentTargetPosition = target;
        }

        #region Debug Context

#if UNITY_EDITOR
        [ContextMenu("Debug/Show Rabbit Info")]
        public void DebugContext_ShowRabbitInfo()
        {
            string bushInfo = _rabbitBush != null
                ? $"Bush ID: {_rabbitBush.GetBushID()}, Bush Position: {_rabbitBush.transform.position}, Bush Rabbits Count: {_rabbitBush.RabbitCount}"
                : $"Bush: None (ID: {_bushID})";

            string stateTargetInfo = "";
            if (_currentState != null)
            {
                stateTargetInfo = $", State Type: {_currentState.GetType().Name}";
                if (_currentState is States.RabbitMoveNearBushState moveNearBushState)
                {
                    stateTargetInfo += $", State Target Position: {moveNearBushState.TargetPosition}";
                }
                else if (_currentState is States.RabbitMoveToHuntingAreaState moveToHuntingState)
                {
                    stateTargetInfo += $", State Target Position: {moveToHuntingState.TargetPosition}";
                }
            }

            Debug.Log($"[RabbitBehavior] Position: {transform.position}, HP: {hp}, Speed: {speed}, CanMove: {canMove}, IsMove: {isMove}, IsDead: {_isDead}, IsDying: {_isDying}, Current State: {_currentStateName}, Target Position: {_currentTargetPosition}, {bushInfo}{stateTargetInfo}");
        }
#endif

        #endregion

        private void Start()
        {
            _animator = GetComponent<Animator>();

            // Initialize từ PrefabData nếu còn giữ giá trị mặc định
            if (PrefabData.Instance != null)
            {
                if (hp == 1) hp = PrefabData.Instance.rabbitHp;
                if (speed == 1) speed = PrefabData.Instance.rabbitSpeed;
            }

            // Nếu có bushID từ save data, đăng ký với bush
            if (!string.IsNullOrEmpty(_bushID))
            {
                _rabbitBush = Script.Manager.NatureManager.RabbitBushManager.Instance?.GetBushByID(_bushID);
                if (_rabbitBush != null)
                {
                    _rabbitBush.RegisterRabbit(this);
                }
                else
                {
#if UNITY_EDITOR
                    if (isDebug) Debug.LogWarning($"[RabbitBehavior] Cannot find bush with ID '{_bushID}' after load.");
#endif
                }
            }

            ChangeState(new RabbitIdleState());

            if (NPCUpdateManager.Instance != null)
            {
                NPCUpdateManager.Instance.Register(this);
            }
        }

        private void OnDestroy()
        {
            // Dừng tất cả Coroutine để tránh callback sau khi destroy
            StopAllCoroutines();

            // Hủy đăng ký khỏi NPCUpdateManager
            if (NPCUpdateManager.Instance != null)
            {
                NPCUpdateManager.Instance.Unregister(this);
            }

            // Nếu đang trong quá trình chết (isDying=true) mà Scene bị unload đột ngột,
            // đảm bảo tiền đã rơi trước khi destroy (chỉ rơi 1 lần qua _coinDropped)
            if (_isDying && !_coinDropped)
            {
                ExecuteDropCoin();
            }
        }

        private void Update()
        {
            if (_isDead) return;
            _currentState?.Update(this);
        }

        public void UpdateBehavior()
        {
            if (_isDead) return;
            _currentState?.UpdateBehavior(this);
        }

        public void OnObjectsDetected(List<GameObject> detectedObjects)
        {
            // Reserved for future object detection
        }

        public void SetBush(RabbitBushBehavior bush)
        {
            _rabbitBush = bush;
        }

        public void SetBushID(string bushID)
        {
            _bushID = bushID;
        }

        #region State Machine

        public void ChangeState(RabbitState newState)
        {
            _currentState?.Exit(this);
            _currentState = newState;
            _currentState?.Enter(this);
            _currentStateName = _currentState?.GetType().Name ?? "None";
        }

        public void PlayAnimation(string animName)
        {
            if (_animator != null)
            {
                _animator.Play(animName);
            }
        }

        #endregion

        #region Death

        public void Die()
        {
            if (_isDead) return;
            _isDead = true;
            _isDying = true;

            ChangeState(new RabbitDeadState());

            // Thông báo cho Bush trước khi rơi tiền/destroy
            _rabbitBush?.RabbitDied(this);

            // Rơi tiền (bảo vệ bởi _coinDropped)
            ExecuteDropCoin();

            StartCoroutine(CleanupAndDestroy());
        }

        /// <summary>
        /// Rơi tiền — có guard flag để chắc chắn chỉ chạy 1 lần,
        /// dù Die() hay OnDestroy() gọi đến trước.
        /// </summary>
        private void ExecuteDropCoin()
        {
            if (_coinDropped) return;
            _coinDropped = true;

            if (Spawner.Instance == null)
            {
#if UNITY_EDITOR
                Debug.LogError("[RabbitBehavior] Spawner.Instance is null — không thể rơi tiền!");
#endif
                return;
            }

            Vector2 dropDirection = UnityEngine.Random.insideUnitCircle.normalized;
            if (dropDirection.sqrMagnitude < 0.01f) dropDirection = Vector2.right;

            Spawner.Instance.DropByNature(transform.position, 1, dropDirection);

#if UNITY_EDITOR
            if (isDebug) Debug.Log("[RabbitBehavior] Dropped 1 coin.");
#endif
        }

        private System.Collections.IEnumerator CleanupAndDestroy()
        {
            yield return new WaitForSeconds(destroyDelay);

            // Gỡ tất cả mũi tên khỏi thây ma để chúng tự quay về pool (dùng Utility)
            CombatUtility.UnparentAllArrows(transform);

            Destroy(gameObject);
        }

        public void ReceiveDamage(float damage, GameObject instigator)
        {
            if (_isDead) return;

#if UNITY_EDITOR
            if (isDebug) Debug.Log($"[RabbitBehavior] Received {damage} damage from {instigator?.name ?? "Unknown"}");
#endif

            hp -= Mathf.CeilToInt(damage);
            if (hp <= 0)
            {
                Die();
            }
        }

        public void TakeDamage()
        {
            ReceiveDamage(1, null);
        }

        #endregion

        #region Save/Load

        public string GetPrefabID()
        {
            return "rabbit";
        }

        public object CaptureData()
        {
            // Không serialize Rabbit đang trong quá trình chết
            if (_isDead || _isDying) return null;

            // Lấy target position từ state, tránh cast 2 lần (pattern variable)
            float targetX = 0f;
            float targetY = 0f;
            if (_currentState is States.RabbitMoveNearBushState moveState)
            {
                targetX = moveState.TargetPosition.x;
                targetY = moveState.TargetPosition.y;
            }

            return new RabbitSaveData
            {
                positionX = transform.position.x,
                positionY = transform.position.y,
                hp = hp,
                bushID = _bushID,
                targetPositionX = targetX,
                targetPositionY = targetY
            };
        }

        public void RestoreData(object data)
        {
            if (data is RabbitSaveData saveData)
            {
                transform.position = new Vector3(saveData.positionX, saveData.positionY, 0f);
                hp = saveData.hp;
                _bushID = saveData.bushID;

#if UNITY_EDITOR
                if (isDebug) Debug.Log($"[RabbitBehavior] RestoreData — bushID: {_bushID}, pos: {transform.position}");
#endif
            }
        }

        #endregion
    }
}
