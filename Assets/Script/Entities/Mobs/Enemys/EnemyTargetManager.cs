using System.Collections.Generic;
using UnityEngine;

namespace Script.Game.Enemy
{
    public class EnemyTargetManager : MonoBehaviour
    {
        public static EnemyTargetManager Instance { get; private set; }

        [Header("Debug Info")]
        [SerializeField] private GameObject _primaryTarget;

        [Header("Tag Whitelist - Tiến công (MoveToTarget)")]
        [Tooltip("Enemy chỉ tấn công/chặn target có tag này khi đang ở State tiến công (MoveToTarget/ShieldBotAttack)")]
        public List<string> advanceTags;

        [Header("Tag Whitelist - Rút lui (ReturnToPortal)")]
        [Tooltip("Enemy chỉ tấn công/chặn target có tag này khi đang rút về Portal")]
        public List<string> retreatTags;

        [Header("Layer Settings")]
        [Tooltip("Layer mà Enemy sẽ quét tia phát hiện vật thể")]
        public LayerMask detectionLayer = ~0;

        public bool HasTarget => _primaryTarget != null;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                
                if (advanceTags == null || advanceTags.Count == 0)
                {
                    advanceTags = new List<string>(Script.Utils.TargetLists.EnemyAdvanceTargets);
                }
                
                if (retreatTags == null || retreatTags.Count == 0)
                {
                    retreatTags = new List<string>(Script.Utils.TargetLists.EnemyRetreatTargets);
                }
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void SetPrimaryTarget(GameObject target)
        {
            _primaryTarget = target;
        }

        public GameObject GetTarget(GameObject self)
        {
            if (_primaryTarget != null && _primaryTarget.activeSelf)
            {
                return _primaryTarget;
            }
            return null;
        }

        /// <summary>
        /// Kiểm tra xem một GameObject có tag hợp lệ để tấn công khi đang tiến công không
        /// </summary>
        public bool IsValidAdvanceTarget(GameObject target)
        {
            if (target == null) return false;
            if (advanceTags == null || advanceTags.Count == 0) return true; // Không filter nếu list trống
            return advanceTags.Contains(target.tag) ||
                   (target.transform.parent != null && advanceTags.Contains(target.transform.parent.tag));
        }

        /// <summary>
        /// Kiểm tra xem một GameObject có tag hợp lệ để tấn công khi đang rút lui không
        /// </summary>
        public bool IsValidRetreatTarget(GameObject target)
        {
            if (target == null) return false;
            if (retreatTags == null || retreatTags.Count == 0) return true;
            return retreatTags.Contains(target.tag) ||
                   (target.transform.parent != null && retreatTags.Contains(target.transform.parent.tag));
        }
    }
}
