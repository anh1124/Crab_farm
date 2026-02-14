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
            float x = Vector2.Distance(new Vector2(start.x, 0), new Vector2(end.x, 0));
            float y = end.y - start.y;
            float g = Mathf.Abs(gravity);
            float angleRad = angle * Mathf.Deg2Rad;

            float cos = Mathf.Cos(angleRad);
            float tan = Mathf.Tan(angleRad);

            // Ballistic formula: v = (x / cos) * sqrt(g / 2(x*tan - y))
            float term = 2 * (x * tan - y);
            if (term <= 0) return -1f; // Impossible to reach

            float v = (x / cos) * Mathf.Sqrt(g / term);
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

            // Increase steps slightly to ensure we cover full arc to target
            int steps = 50; 

            for (int step = 0; step < steps; step++)
            {
                Vector2 prevPos = pos;
                velocity.y += gravity * DEFAULT_TIME_STEP; // Gravity is negative
                pos += velocity * DEFAULT_TIME_STEP;

                RaycastHit2D hit = Physics2D.Linecast(prevPos, pos, wallLayer);
                if (hit.collider != null)
                {
                    return false; // Hit wall
                }
                
                // Break if we reach or pass target (simplified check)
                if (Vector2.Distance(pos, targetPos) < 1f) break;
                if (pos.y < Mathf.Min(shootPos.y, targetPos.y) - 5f) break; // Dropped below interest
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
