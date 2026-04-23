using UnityEngine;
using System.Collections.Generic;

namespace Script.Game.Combat
{
    /// <summary>
    /// Simulates projectile trajectory and finds valid launch angles.
    /// Based on the Arrow Trajectory Predictor Guide.
    /// </summary>
    public static class TrajectoryPredictor
    {
        // Settings
        private const int DEFAULT_SIMULATION_STEPS = 30;      // Reduced from 50 -> 30 (enough for ~2.4s flight)
        private const float DEFAULT_TIME_STEP = 0.08f;        // Reduced from 0.1 -> 0.08

        /// <summary>
        /// Finds all launch angles that do not result in a wall collision.
        /// </summary>
        /// <summary>
        /// Finds all launch angles that can reach the target without hitting walls.
        /// Calculates required speed for each angle dynamically.
        /// </summary>
        public static List<float> FindValidAngles(
            Vector2 shootPos,
            Vector2 targetPos,
            float maxSpeed,
            float gravity,
            float minAngle,
            float maxAngle,
            float angleStep,
            LayerMask wallLayer)
        {
            List<float> validAngles = new List<float>();

            for (float angle = minAngle; angle <= maxAngle; angle += angleStep)
            {
                // 1. Calculate required speed for this angle
                float requiredSpeed = CalculateLaunchSpeed(shootPos, targetPos, angle, gravity);
                
                // 2. Check if speed is within limits
                if (requiredSpeed == -1f || requiredSpeed > maxSpeed) continue;

                // 3. Check for wall collisions with this specific trajectory
                if (IsAngleValid(shootPos, targetPos, angle, requiredSpeed, gravity, wallLayer))
                {
                    validAngles.Add(angle);
                }
            }
            
            return validAngles;
        }

        public static float CalculateLaunchSpeed(Vector2 start, Vector2 end, float angle, float gravity)
        {
            float x = Mathf.Abs(end.x - start.x);
            float y = end.y - start.y;
            float g = Mathf.Abs(gravity);
            float angleRad = angle * Mathf.Deg2Rad;

            float cos = Mathf.Cos(angleRad);
            float tan = Mathf.Tan(angleRad);

            // Ballistic formula: v = (x / cos) * sqrt(g / 2(x*tan - y))
            float term = 2 * (x * tan - y);
            
            // Impossible to reach: góc quá thấp không thể đạt độ cao y
            if (term <= 0.001f) return -1f;
            
            // Tránh speed quá lớn khi term rất nhỏ (target quá gần)
            // Giới hạn term tối thiểu để speed không vượt quá 100
            const float MIN_TERM = 0.02f;
            if (term < MIN_TERM && x < 1f)
            {
                // Target quá gần, dùng công thức đơn giản: v = sqrt(g * x) với tolerance
                term = MIN_TERM;
            }

            float v = (x / cos) * Mathf.Sqrt(g / term);
            
            // Giới hạn speed tối đa để tránh giá trị vô lý
            if (v > 100f) return -1f;
            
            return v;
        }

        private static bool IsAngleValid(
            Vector2 shootPos,
            Vector2 targetPos,
            float angle,
            float speed,
            float gravity,
            LayerMask wallLayer)
        {
            Vector2 direction = AngleToDirection(angle, targetPos - shootPos);
            Vector2 velocity = direction * speed;
            Vector2 pos = shootPos;

            // 20 bước × 0.12s = bao quát 2.4s flight time
            // Linecast MỖI bước để không bỏ sót tường hẹp
            // (20 raycast thay vì 50 cũ - vẫn giảm ~60% nhưng đáng tin cậy hơn)
            const int STEPS = 20;
            const float TIME_STEP = 0.12f;

            for (int step = 0; step < STEPS; step++)
            {
                Vector2 prevPos = pos;
                velocity.y += gravity * TIME_STEP;
                pos += velocity * TIME_STEP;

                // Linecast từng đoạn nhỏ – không skip, không bỏ sót tường
                RaycastHit2D hit = Physics2D.Linecast(prevPos, pos, wallLayer);
                if (hit.collider != null) return false;
                
                // Dừng nếu đã tới gần đích hoặc rơi quá thấp
                if (Vector2.Distance(pos, targetPos) < 1.2f) break;
                if (pos.y < Mathf.Min(shootPos.y, targetPos.y) - 4f) break;
            }

            return true;
        }

        public static Vector2 SimulateLanding(
            Vector2 shootPos,
            float angle,
            float speed,
            float gravity,
            Vector2 horizontalDirection)
        {
            Vector2 direction = AngleToDirection(angle, horizontalDirection);
            Vector2 velocity = direction * speed;
            Vector2 pos = shootPos;

            for (int step = 0; step < DEFAULT_SIMULATION_STEPS; step++)
            {
                velocity.y += gravity * DEFAULT_TIME_STEP;
                pos += velocity * DEFAULT_TIME_STEP;

                if (pos.y <= shootPos.y - 2f) 
                {
                    break;
                }
            }

            return pos;
        }

        private static Vector2 AngleToDirection(float angle, Vector2 horizontalDirection)
        {
            float angleRad = angle * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));

            if (horizontalDirection.x < 0)
            {
                dir.x = -dir.x;
            }

            return dir;
        }
    }
}
