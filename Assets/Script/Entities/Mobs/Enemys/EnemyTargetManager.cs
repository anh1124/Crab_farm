using UnityEngine;

namespace Script.Game.Enemy
{
    public class EnemyTargetManager : MonoBehaviour
    {
        public static EnemyTargetManager Instance { get; private set; }

        [Header("Debug Info")]
        [SerializeField] private GameObject _primaryTarget;

        [Header("Layer Settings")]
        [Tooltip("Layer mà Enemy sẽ quét tia phát hiện vật thể")]
        public LayerMask detectionLayer = ~0;

        public bool HasTarget => _primaryTarget != null;
        public GameObject PrimaryTarget => _primaryTarget;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
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
    }
}
