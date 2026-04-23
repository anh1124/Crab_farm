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
        private IArrowPool arrowPool;
        
        public void Setup(IArrowPool pool)
        {
            arrowPool = pool;
        }

        [Header("Projectile Settings")]
        [SerializeField] private float projectileSpeed = 15f;
        [SerializeField] private float gravityScale = 2f;

        [Header("Low Arc (ưu tiên - bắn thẳng nhanh)")]
        [SerializeField] private float lowMinAngle  = 30f;
        [SerializeField] private float lowMaxAngle  = 45f;
        [SerializeField] private float lowAngleStep =  5f;

        [Header("High Arc (fallback khi bị tường chặn)")]
        [SerializeField] private float highMinAngle  = 60f;
        [SerializeField] private float highMaxAngle  = 80f;
        [SerializeField] private float highAngleStep =  5f;

        [Header("Combat Stats")]
        [SerializeField] private float accuracy = 0.5f; // 0.0 – 1.0

        [Header("Wall Detection")]
        [SerializeField] private LayerMask wallLayer;

        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;

        [Header("Visuals")]
        [SerializeField] private float stickImpactOffset = 0.4f;

        /// <summary>
        /// Số bước simulate khi kiểm tra va chạm tường trong IsTrajectoryBlocked.
        /// Tăng để chính xác hơn, giảm để tiết kiệm CPU.
        /// </summary>
        [Header("Trajectory Simulation")]
        [SerializeField] private int trajectorySimSteps = 24;
        [SerializeField] private float simulatedArrowRadius = 0.25f;
        #endregion

        #region Internal State & Cache
        private float   lastSolveTime = -999f;
        private Vector2 lastTargetPos;
        private float   cachedAngle;
        private float   cachedSpeed;

        private float _attackCooldownTimer = 999f;
        public  bool  IsAttacking { get; private set; }
        #endregion

        #region Unity Lifecycle
        private void Update()
        {
            if (!IsAttacking)
                _attackCooldownTimer += Time.deltaTime;
        }
        #endregion

        #region Public API
        public bool CanShoot(float attackCooldown)
            => !IsAttacking && _attackCooldownTimer >= attackCooldown;

        public void StartShooting()
        {
            IsAttacking = true;
            _attackCooldownTimer = 0f;
        }

        public void ShootAt(Vector2 targetPosition)
        {
            IsAttacking = false;

            if (arrowPool == null)
            {
                Debug.LogError($"[ArcherShooting] ArrowPool is null on {gameObject.name}!");
                return;
            }

            if (wallLayer.value == 0)
                Debug.LogError($"[ArcherShooting] Wall Layer is not set on {gameObject.name}!");

            bool needsRecalculate = (Time.time - lastSolveTime > 0.5f) ||
                                    (Vector2.Distance(lastTargetPos, targetPosition) > 1f);

            if (needsRecalculate)
            {
                SolveTrajectory(targetPosition);
                lastSolveTime = Time.time;
                lastTargetPos = targetPosition;
            }

            float finalAngle = cachedAngle;
            // Clamp speed: tránh mũi tên vô lý khi target quá gần (speed → ∞)
            float finalSpeed = Mathf.Min(cachedSpeed, projectileSpeed * 3f);

            if (accuracy < 1.0f)
                finalAngle += Random.Range(-5f, 5f) * (1f - accuracy);

            GameObject arrowObj = arrowPool.GetArrow();
            if (arrowObj == null) return;

            arrowObj.transform.position = firePoint.position;

            Vector2 direction = CalculateDirection(finalAngle, targetPosition);
            Vector2 velocity  = direction * finalSpeed;

            ArrowController arrow = arrowObj.GetComponent<ArrowController>();
            if (arrow != null)
                arrow.Initialize(velocity, finalAngle, gravityScale, stickImpactOffset, gameObject);
        }
        #endregion

        #region Internal Logic

        // ─────────────────────────────────────────────────────────────────────
        // SOLVE TRAJECTORY
        //
        // TẦNG 1 : Góc thấp (lowMinAngle – lowMaxAngle)  – flat, đến đích nhanh.
        // TẦNG 2 : Góc cao  (highMinAngle – highMaxAngle) – vòng cung vượt tường.
        // TẦNG 3 : Không có góc nào không va chạm tường → fallback thông minh:
        //          tìm góc tối thiểu (từ lowMaxAngle+step đến 89°) mà mũi tên
        //          vượt qua đỉnh bức tường gần nhất, speed vẫn tính để trúng
        //          enemy → angle & speed nhất quán trên cùng một parabol.
        //          Nếu vẫn không được → hard-fallback 85°.
        //
        // THAY ĐỔI CHÍNH SO VỚI BẢN CŨ:
        //   [1] Bỏ FindValidAngles + probe speed * 1.5f (không nhất quán vật lý).
        //       Thay bằng TryScanAngles: mỗi góc tính CalculateLaunchSpeed trước,
        //       rồi simulate với CHÍNH speed đó qua IsTrajectoryBlocked.
        //   [2] Tầng 3 bắt đầu từ lowMaxAngle + lowAngleStep (thay vì highMinAngle)
        //       để lấp đầy khoảng trống góc 46°–59° bị bỏ sót trước đây.
        // ─────────────────────────────────────────────────────────────────────
        private void SolveTrajectory(Vector2 targetPosition)
        {
            float actualGravity = Physics2D.gravity.y * gravityScale; // luôn âm

            // === TẦNG 1: Góc thấp ===
            if (TryScanAngles(targetPosition, actualGravity,
                              lowMinAngle, lowMaxAngle, lowAngleStep,
                              preferLowest: true,
                              out float angle1, out float speed1))
            {
                cachedAngle = angle1;
                cachedSpeed = speed1;
                if (showDebugInfo)
                    Debug.Log($"[ArcherShooting] LOW ARC {cachedAngle}° | speed={cachedSpeed:F1}");
                return;
            }

            // === TẦNG 2: Góc cao ===
            if (TryScanAngles(targetPosition, actualGravity,
                              highMinAngle, highMaxAngle, highAngleStep,
                              preferLowest: false,
                              out float angle2, out float speed2))
            {
                cachedAngle = angle2;
                cachedSpeed = speed2;
                if (showDebugInfo)
                    Debug.Log($"[ArcherShooting] HIGH ARC {cachedAngle}° | speed={cachedSpeed:F1}");
                return;
            }

            // === TẦNG 3: Fallback thông minh – vượt tường gần nhất ===
            if (FindNearestWall(targetPosition, out Vector2 wallHitPoint, out Collider2D wallCollider))
            {
                // Bắt đầu từ lowMaxAngle + step để bao phủ luôn khoảng 46°–59°
                float fallbackStartAngle = lowMaxAngle + lowAngleStep;

                if (TryFindAngleClearingWall(targetPosition, actualGravity,
                                             wallHitPoint, wallCollider,
                                             fallbackStartAngle,
                                             out float angle3, out float speed3))
                {
                    cachedAngle = angle3;
                    cachedSpeed = speed3;
                    if (showDebugInfo)
                        Debug.Log($"[ArcherShooting] FALLBACK WALL-CLEAR: angle={cachedAngle}° | speed={cachedSpeed:F1}");
                    return;
                }
            }

            // === TẦNG 4: Thử quét góc cao 70°-89° (không cần tính wall clear) ===
            if (TryScanAngles(targetPosition, actualGravity,
                              70f, 89f, 5f,
                              preferLowest: false,
                              out float angle4, out float speed4))
            {
                cachedAngle = angle4;
                cachedSpeed = speed4;
                if (showDebugInfo)
                    Debug.Log($"[ArcherShooting] HIGH ARC FALLBACK {cachedAngle}° | speed={cachedSpeed:F1}");
                return;
            }

            // Hard fallback: 70° với speed cố định (góc cao dễ vượt tường hơn)
            cachedAngle = 70f;
            cachedSpeed = Mathf.Min(projectileSpeed * 1.5f, 25f);
            Debug.LogWarning($"[ArcherShooting] HARD FALLBACK 70° fixed speed={cachedSpeed:F1} (không tìm thấy quỹ đạo hợp lệ, target={targetPosition})");
        }

        // ─────────────────────────────────────────────────────────────────────
        // TryScanAngles
        //
        // Quét từng góc trong [minAngle, maxAngle] theo bước step.
        // Với MỖI góc:
        //   1. CalculateLaunchSpeed → speed chính xác để trúng target.
        //   2. IsTrajectoryBlocked  → simulate parabol với CHÍNH speed đó.
        //
        // Speed dùng để kiểm tra và speed thực tế bắn ra là MỘT → nhất quán vật lý.
        // ─────────────────────────────────────────────────────────────────────
        private bool TryScanAngles(Vector2 targetPos, float actualGravity,
                                   float minAngle, float maxAngle, float step,
                                   bool preferLowest,
                                   out float bestAngle, out float bestSpeed)
        {
            bestAngle = 0f;
            bestSpeed = 0f;

            var candidates = new List<(float angle, float speed)>();

            for (float angle = minAngle; angle <= maxAngle + 0.001f; angle += step)
            {
                float speed = TrajectoryPredictor.CalculateLaunchSpeed(
                    firePoint.position, targetPos, angle, actualGravity);
                if (speed <= 0f) continue;

                if (!IsTrajectoryBlocked(targetPos, angle, speed, actualGravity))
                    candidates.Add((angle, speed));
            }

            if (candidates.Count == 0) return false;

            // Tầng 1: ưu tiên góc nhỏ nhất (phẳng, nhanh)
            // Tầng 2: chọn ngẫu nhiên để tạo đa dạng
            var chosen = preferLowest
                ? candidates[0]
                : candidates[Random.Range(0, candidates.Count)];

            bestAngle = chosen.angle;
            bestSpeed = chosen.speed;
            return true;
        }

        // ─────────────────────────────────────────────────────────────────────
        // IsTrajectoryBlocked
        //
        // Simulate quỹ đạo parabol bằng Linecast từng đoạn nhỏ.
        // Flight time ước tính từ chiều ngang (dx / vx).
        // Trả về true nếu bất kỳ đoạn nào va chạm wallLayer.
        // ─────────────────────────────────────────────────────────────────────
        private bool IsTrajectoryBlocked(Vector2 targetPos, float angle,
                                         float speed, float actualGravity)
        {
            Vector2 origin    = firePoint.position;
            Vector2 launchDir = CalculateDirection(angle, targetPos);
            Vector2 vel       = launchDir * speed;

            float dx = Mathf.Abs(targetPos.x - origin.x);
            float vx = Mathf.Abs(vel.x);

            // Enemy thẳng đứng → không có tường ngang để kiểm tra
            if (vx < 0.001f) return false;

            float flightTime = dx / vx;
            Vector2 prev = origin;

            for (int i = 1; i <= trajectorySimSteps; i++)
            {
                float t  = flightTime * i / trajectorySimSteps;
                float px = origin.x + vel.x * t;
                float py = origin.y + vel.y * t + 0.5f * actualGravity * t * t;
                Vector2 curr = new Vector2(px, py);

                Vector2 direction = (curr - prev).normalized;
                float distance = Vector2.Distance(prev, curr);

                RaycastHit2D hit = Physics2D.CircleCast(
                    prev, simulatedArrowRadius, direction, distance, wallLayer);
                if (hit.collider != null) return true;

                prev = curr;
            }

            return false;
        }

        // ─────────────────────────────────────────────────────────────────────
        // FindNearestWall
        //
        // Raycast thẳng từ firePoint đến target.
        // Trả về hit.point (bề mặt tường thực tế, không phải bounds.center)
        // → dx trong TryFindAngleClearingWall chính xác hơn.
        // ─────────────────────────────────────────────────────────────────────
        private bool FindNearestWall(Vector2 targetPosition,
                                     out Vector2 wallHitPoint,
                                     out Collider2D wallCollider)
        {
            wallHitPoint = Vector2.zero;
            wallCollider = null;

            Vector2 origin    = firePoint.position;
            Vector2 direction = targetPosition - origin;
            float   distance  = direction.magnitude;

            RaycastHit2D hit = Physics2D.Raycast(
                origin, direction.normalized, distance, wallLayer);

            if (hit.collider == null) return false;

            wallHitPoint = hit.point;
            wallCollider = hit.collider;
            return true;
        }

        // ─────────────────────────────────────────────────────────────────────
        // TryFindAngleClearingWall
        //
        // Quét góc từ startAngle đến 89° (bước highAngleStep).
        // Với mỗi góc:
        //   1. CalculateLaunchSpeed → speed để trúng enemy.
        //   2. Tính Y của mũi tên tại X của tường (công thức parabol).
        //   3. Nếu Y > wallTopY → mũi tên vượt qua đỉnh tường → chọn.
        //
        // Angle & speed nhất quán: cùng một parabol, không tách hai bước.
        //
        // Dấu: actualGravity âm → 0.5f * actualGravity * t² tự kéo Y xuống.
        // dx = Mathf.Abs(...) → t > 0 dù enemy ở trái hay phải.
        // ─────────────────────────────────────────────────────────────────────
        private bool TryFindAngleClearingWall(Vector2 targetPos, float actualGravity,
                                              Vector2 wallHitPoint, Collider2D wallCollider,
                                              float startAngle,
                                              out float bestAngle, out float bestSpeed)
        {
            bestAngle = 0f;
            bestSpeed = 0f;

            float wallTopY = wallCollider.bounds.max.y;
            float originX  = firePoint.position.x;
            float originY  = firePoint.position.y;

            float dx = Mathf.Abs(wallHitPoint.x - originX);
            if (dx < 0.01f) return false; // tường ngay tại firePoint

            for (float angle = startAngle; angle <= 89f; angle += highAngleStep)
            {
                float rad   = angle * Mathf.Deg2Rad;
                float speed = TrajectoryPredictor.CalculateLaunchSpeed(
                    firePoint.position, targetPos, angle, actualGravity);
                if (speed <= 0f) continue;

                float vx = speed * Mathf.Cos(rad);
                float vy = speed * Mathf.Sin(rad);

                float t       = dx / vx;                                          // t > 0 vì dx > 0, vx > 0
                float yAtWall = originY + vy * t + 0.5f * actualGravity * t * t; // actualGravity âm → kéo xuống

                if (yAtWall > wallTopY)
                {
                    bestAngle = angle;
                    bestSpeed = speed;
                    return true;
                }
            }

            return false;
        }

        // ─────────────────────────────────────────────────────────────────────
        // CalculateDirection
        // ─────────────────────────────────────────────────────────────────────
        private Vector2 CalculateDirection(float angle, Vector2 targetPos)
        {
            float angleRad = angle * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));

            if (targetPos.x < firePoint.position.x)
                dir.x = -dir.x;

            return dir;
        }
        #endregion
    }
}
