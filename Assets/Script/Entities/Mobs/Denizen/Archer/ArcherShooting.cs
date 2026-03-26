using UnityEngine;
using System.Collections.Generic;
using Script.Game.Combat;

namespace Script.Game.Mobs.Subjects
{
    public class ArcherShooting : MonoBehaviour
    {
        #region Fields & Settings
        [Header("References")]
        [SerializeField] private Transform firePoint;
        [SerializeField] private ArrowPool arrowPool;
        
        [Header("Projectile Settings")]
        [SerializeField] private float projectileSpeed = 15f;
        [SerializeField] private float gravityScale = 2f;
        
        [Header("Low Arc (ưu tiên - bắn thẳng nhanh)")]
        [SerializeField] private float lowMinAngle = 30f;
        [SerializeField] private float lowMaxAngle = 45f;
        [SerializeField] private float lowAngleStep = 5f;
        
        [Header("High Arc (fallback khi bị tường chặn)")]
        [SerializeField] private float highMinAngle = 60f;
        [SerializeField] private float highMaxAngle = 80f;
        [SerializeField] private float highAngleStep = 10f;
        
        [Header("Combat Stats")]
        [SerializeField] private float accuracy = 0.5f; // 0.0 - 1.0
        
        [Header("Wall Detection")]
        [SerializeField] private LayerMask wallLayer;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;
        
        [Header("Visuals")]
        [SerializeField] private float stickImpactOffset = 0.4f;
        #endregion

        #region Internal State & Cache
        // Cache kết quả tính toán
        private float lastSolveTime = -999f;
        private Vector2 lastTargetPos;
        private float cachedAngle;
        private float cachedSpeed;

        // Quản lý Cooldown & Trạng thái tấn công
        private float _attackCooldownTimer = 999f;
        public bool IsAttacking { get; private set; }
        #endregion

        #region Unity Lifecycle
        private void Update()
        {
            if (!IsAttacking)
            {
                _attackCooldownTimer += Time.deltaTime;
            }
        }
        #endregion

        #region Public API
        public bool CanShoot(float attackCooldown)
        {
            return !IsAttacking && _attackCooldownTimer >= attackCooldown;
        }

        public void StartShooting()
        {
            IsAttacking = true;
            _attackCooldownTimer = 0f;
        }
        
        public void ShootAt(Vector2 targetPosition)
        {
            IsAttacking = false; // Tên đã bắn ra -> Kết thúc tấn công, bắt đầu đếm lại cooldown
            
            // 0. Auto-find pool if missing
            if (arrowPool == null)
            {
                arrowPool = ArrowPool.Instance;
                if (arrowPool == null)
                {
                    Debug.LogError("[ArcherShooting] ArrowPool not found in scene!");
                    return;
                }
            }

            if (wallLayer.value == 0)
            {
                Debug.LogError($"[ArcherShooting] Wall Layer is not set on {gameObject.name}!");
            }

            // 1. Cache check – chỉ solve lại khi target di chuyển xa hoặc sau 0.5s
            bool needsRecalculate = (Time.time - lastSolveTime > 0.5f) || 
                                    (Vector2.Distance(lastTargetPos, targetPosition) > 1f);
            
            if (needsRecalculate)
            {
                SolveTrajectory(targetPosition);
                lastSolveTime = Time.time;
                lastTargetPos = targetPosition;
            }

            float finalAngle = cachedAngle;
            float finalSpeed = cachedSpeed;

            // Accuracy scatter – thêm độ lệch ngẫu nhiên dựa trên accuracy
            if (accuracy < 1.0f)
            {
                finalAngle += Random.Range(-5f, 5f) * (1f - accuracy);
            }

            GameObject arrowObj = arrowPool.GetArrow();
            if (arrowObj == null) return;

            arrowObj.transform.position = firePoint.position;
            
            Vector2 direction = CalculateDirection(finalAngle, targetPosition);
            Vector2 velocity = direction * finalSpeed;
            
            ArrowController arrow = arrowObj.GetComponent<ArrowController>();
            if (arrow != null)
            {
                arrow.Initialize(velocity, finalAngle, gravityScale, stickImpactOffset, gameObject);
            }
        }
        #endregion
        
        #region Internal Logic
        /// <summary>
        /// Logic chọn góc 2 tầng:
        /// TẦNG 1: Thử góc thấp (30–45°) – mũi tên bay nhanh, tới đích sớm nhất.
        /// TẦNG 2: Nếu tất cả góc thấp bị tường chặn → chuyển sang góc cao (60–80°) để bay vòng cung vượt tường.
        /// TẦNG 3: Không góc nào khả thi → fallback cứng 60°.
        /// </summary>
        private void SolveTrajectory(Vector2 targetPosition)
        {
            float actualGravity = Physics2D.gravity.y * gravityScale;
            
            // === TẦNG 1: Thử góc thấp trước ===
            List<float> lowAngles = TrajectoryPredictor.FindValidAngles(
                firePoint.position,
                targetPosition,
                projectileSpeed * 1.5f,
                actualGravity,
                lowMinAngle,
                lowMaxAngle,
                lowAngleStep,
                wallLayer
            );

            if (lowAngles.Count > 0)
            {
                // Ưu tiên góc nhỏ nhất (bay thẳng, đến đích nhanh nhất)
                lowAngles.Sort();
                cachedAngle = lowAngles[0];
                cachedSpeed = TrajectoryPredictor.CalculateLaunchSpeed(
                    firePoint.position, targetPosition, cachedAngle, actualGravity
                );
                if (showDebugInfo)
                    Debug.Log($"[ArcherShooting] LOW ARC {cachedAngle}° | speed={cachedSpeed:F1}");
                
                if (cachedSpeed <= 0) cachedSpeed = projectileSpeed;
                return;
            }

            // === TẦNG 2: Góc thấp bị chặn → fallback lên góc cao ===
            List<float> highAngles = TrajectoryPredictor.FindValidAngles(
                firePoint.position,
                targetPosition,
                projectileSpeed * 1.5f,
                actualGravity,
                highMinAngle,
                highMaxAngle,
                highAngleStep,
                wallLayer
            );

            if (highAngles.Count > 0)
            {
                // Chọn ngẫu nhiên trong danh sách góc cao để có sự dđa dạng
                cachedAngle = highAngles[Random.Range(0, highAngles.Count)];
                cachedSpeed = TrajectoryPredictor.CalculateLaunchSpeed(
                    firePoint.position, targetPosition, cachedAngle, actualGravity
                );

                if (showDebugInfo)
                    Debug.Log($"[ArcherShooting] HIGH ARC {cachedAngle}° (low blocked by wall) | speed={cachedSpeed:F1}");

                if (cachedSpeed <= 0) cachedSpeed = projectileSpeed;
                return;
            }

            // === TẦNG 3: Cả 2 đều bị chặn → fallback cứng ===
            if (showDebugInfo)
                Debug.LogWarning("[ArcherShooting] No valid angle found! Using fallback 60°.");
            cachedAngle = 60f;
            cachedSpeed = projectileSpeed;
        }
        
        private Vector2 CalculateDirection(float angle, Vector2 targetPos)
        {
            float angleRad = angle * Mathf.Deg2Rad;
            
            Vector2 dir = new Vector2(
                Mathf.Cos(angleRad),
                Mathf.Sin(angleRad)
            );
            
            // Lật hướng X nếu target ở phía trái
            Vector2 toTarget = targetPos - (Vector2)firePoint.position;
            if (toTarget.x < 0)
            {
                dir.x = -dir.x;
            }
            
            return dir;
        }
        #endregion
    }
}
