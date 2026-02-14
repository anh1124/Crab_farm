using UnityEngine;
using System.Collections.Generic;
using Script.Game.Combat; // Namespace for ArrowPool, TrajectoryPredictor

namespace Script.Game.Mobs.Subjects
{
    public class ArcherShooting : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform firePoint;
        [SerializeField] private ArrowPool arrowPool;
        
        [Header("Projectile Settings")]
        [SerializeField] private float projectileSpeed = 15f;
        [SerializeField] private float gravityScale = 2f;
        
        [Header("Trajectory Settings")]
        [SerializeField] private float minAngle = 30f;
        [SerializeField] private float maxAngle = 70f;
        [SerializeField] private float angleStep = 5f;
        
        [Header("Combat Stats")]
        [SerializeField] private float accuracy = 0.5f; // 0.0 - 1.0
        
        [Header("Wall Detection")]
        [SerializeField] private LayerMask wallLayer;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true;
        
        [Header("Visuals")]
        [SerializeField] private float stickImpactOffset = 0.4f;
        
        public void ShootAt(Vector2 targetPosition)
        {
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

            // Validation for Wall Layer
            if (wallLayer.value == 0)
            {
                Debug.LogError($"[ArcherShooting] Wall Layer is not set on {gameObject.name}!");
            }

            // 1. Calculate actual gravity for simulation (Physics2D.gravity is likely -9.81)
            float actualGravity = Physics2D.gravity.y * gravityScale;
            
            // 2. Find valid angles (Now passes MAX speed instead of fixed speed)
            // We use projectileSpeed as the MAXIMUM allowed speed
            List<float> validAngles = TrajectoryPredictor.FindValidAngles(
                firePoint.position,
                targetPosition,
                projectileSpeed * 1.5f, // Allow up to 150% of base speed for high arcs
                actualGravity,  
                minAngle,
                maxAngle,
                angleStep,
                wallLayer
            );
            
            if (validAngles.Count == 0)
            {
                if (showDebugInfo) Debug.LogWarning("[ArcherShooting] No valid angles found! Using fallback 45.");
                validAngles.Add(45f);
            }
            
            // 3. Select angle based on accuracy (Simplified: Just pick random valid for now or closest to optimal)
            // Since all valid angles now GUARANTEE hitting the target (if speed is right), 
            // the 'landing distance' logic in SelectAngle suggests we might want to prioritize lower energy (speed) or specific arc.
            // For now, let's keep it simple: Select random from valid to vary shots, or sort by required speed.
            
            float selectedAngle = validAngles[Random.Range(0, validAngles.Count)];
            
            // 4. Calculate REQUIRED speed for this specific angle
            float finalSpeed = TrajectoryPredictor.CalculateLaunchSpeed(
                firePoint.position, 
                targetPosition, 
                selectedAngle, 
                actualGravity
            );
            
            // Fallback if calculation failed (shouldn't if angle came from FindValid)
            if (finalSpeed <= 0) finalSpeed = projectileSpeed;

            // Apply accuracy scatter to Speed or Angle? 
            // Let's apply slight randomness to target pos implicitly by how accurate the arrow is.
            // But here we rely on "homing" initial velocity. 
            // If accuracy < 1, we could jitter the angle or speed.
            if (accuracy < 1.0f)
            {
                selectedAngle += Random.Range(-5f, 5f) * (1f - accuracy);
            }

            // 5. Spawn arrow
            if (arrowPool == null)
            {
                Debug.LogError("[ArcherShooting] ArrowPool is missing!");
                return;
            }

            GameObject arrowObj = arrowPool.GetArrow();
            if (arrowObj == null) return; // Pool limit reached

            arrowObj.transform.position = firePoint.position;
            
            // 6. Calculate velocity
            Vector2 direction = CalculateDirection(selectedAngle, targetPosition);
            Vector2 velocity = direction * finalSpeed;
            
            // 7. Initialize arrow
            ArrowController arrow = arrowObj.GetComponent<ArrowController>();
            if (arrow != null)
            {
                arrow.Initialize(velocity, selectedAngle, gravityScale, stickImpactOffset);
            }
        }
        
        private float SelectAngle(List<float> validAngles, float accuracy, Vector2 targetPos, float actualGravity)
        {
            if (validAngles.Count == 1)
            {
                return validAngles[0];
            }
            
            // Cache landing positions
            Dictionary<float, float> angleDistances = new Dictionary<float, float>();
            
            foreach (float angle in validAngles)
            {
                Vector2 landing = TrajectoryPredictor.SimulateLanding(
                    firePoint.position,
                    angle,
                    projectileSpeed,
                    actualGravity,
                    targetPos - (Vector2)firePoint.position
                );
                
                float distance = Vector2.Distance(landing, targetPos);
                angleDistances[angle] = distance;
            }
            
            // Sort angles by distance (closest to target first)
            validAngles.Sort((a, b) => angleDistances[a].CompareTo(angleDistances[b]));
            
            // Select subset based on accuracy
            // accuracy 1.0 -> select best
            // accuracy 0.0 -> random all
            int selectCount = Mathf.CeilToInt(
                Mathf.Lerp(validAngles.Count, 1, accuracy)
            );
            
            selectCount = Mathf.Clamp(selectCount, 1, validAngles.Count);
            
            int index = Random.Range(0, selectCount);
            return validAngles[index];
        }
        
        private Vector2 CalculateDirection(float angle, Vector2 targetPos)
        {
            float angleRad = angle * Mathf.Deg2Rad;
            
            Vector2 dir = new Vector2(
                Mathf.Cos(angleRad),
                Mathf.Sin(angleRad)
            );
            
            Vector2 toTarget = targetPos - (Vector2)firePoint.position;
            if (toTarget.x < 0)
            {
                dir.x = -dir.x;
            }
            
            return dir;
        }
    }
}
