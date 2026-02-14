using UnityEngine;
using Script.Game.Enemy.Interfaces; // For IHealth
using Script.Data_Model; // For PrefabData

namespace Script.Game.Combat
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class ArrowController : MonoBehaviour
    {
        #region References
        private Rigidbody2D rb;
        private Collider2D col; // Added Collider reference
        private SpriteRenderer spriteRenderer;
        private ArrowPool pool;
        #endregion
        
        #region Settings
        [Header("Damage")]
        [SerializeField] private int damage = 1;
        
        [Header("Rotation")]
        [SerializeField] private float spriteBaseAngle = 45f;
        [SerializeField] private float minVelocityToRotate = 0.5f;
        
        [Header("Stick Behavior")]
        [SerializeField] private bool enableSticking = true;
        [SerializeField] private float stickImpactOffset = 0.4f; // Distance to pull back from impact point
        
        // [SerializeField] private LayerMask stickableLayers;  // Removed
        
        [Header("Lifetime")]
        [SerializeField] private float stuckLifetime = 3f;  // Lifetime AFTER stuck
        [SerializeField] private float maxAirTime = 6f;     // Fallback if flies too long
        #endregion
        
        #region State
        private bool hasDealtDamage = false;
        private bool isActive = true;
        private bool isStuck = false;
        private float lifeTimer = 0f;           
        private float timeSinceSpawn = 0f;      
        private float deactivateThreshold = 0.5f;
        #endregion
        
        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            col = GetComponent<Collider2D>(); // Get Collider
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
        
        public void Initialize(Vector2 velocity, float launchAngle, float gravityScaleValue, float stickOffset = 0.4f)
        {
            hasDealtDamage = false;
            isActive = true;
            isStuck = false;
            lifeTimer = 0f;          
            timeSinceSpawn = 0f;     
            
            // Set dynamic properties
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.simulated = true; // CRITICAL: Re-enable physics simulation (disabled in DeactivatePhysics)
            rb.mass = 0.0001f; 
            rb.velocity = velocity;
            rb.gravityScale = gravityScaleValue; 
            rb.angularVelocity = 0f;
            
            this.stickImpactOffset = stickOffset;
            
            if (col != null) 
            {
                col.enabled = true;
                col.isTrigger = false; 
            }
            
            // Debug initialization
            Debug.Log($"[Arrow Init] Vel: {velocity.magnitude:F2} | Angle: {launchAngle:F1}° | Gravity: {gravityScaleValue} | Pos: {transform.position}");
            
            SetInitialRotation(velocity);
        }
        
        private void Update()
        {
            timeSinceSpawn += Time.deltaTime;
            
            if (!isStuck && timeSinceSpawn > maxAirTime)
            {
                ReturnToPool();
                return;
            }
            
            if (isStuck)
            {
                lifeTimer -= Time.deltaTime;
                if (lifeTimer <= 0f)
                {
                    ReturnToPool();
                    return;
                }
            }
            
            if (!isActive) return;
            
            if (rb.velocity.sqrMagnitude > minVelocityToRotate * minVelocityToRotate)
            {
                UpdateRotation();
            }
        }
        
        private void FixedUpdate()
        {
            if (!isActive) return;
            
            if (rb.velocity.magnitude < deactivateThreshold)
            {
                // DeactivatePhysics(); // Too sensitive? Left for air drag stops
            }
        }
        
        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!isActive) return;
            
            // Debug collision
            Debug.Log($"[Arrow] Hit: {collision.gameObject.name} | Tag: {collision.gameObject.tag} | Layer: {LayerMask.LayerToName(collision.gameObject.layer)}");

            // Stop immediately to prevent bounce/rotation
            rb.velocity = Vector2.zero; 
            rb.angularVelocity = 0f;

            // Damage logic
            if (!hasDealtDamage)
            {
                TryDealDamage(collision.gameObject);
            }
            
            // Stick logic
            if (enableSticking && !isStuck)
            {
                TryStickToTarget(collision);
            }
            else
            {
                // Debug.Log("[Arrow] Sticking disabled or already stuck.");
            }
            
            // Always deactivate physics after collision logic (if not already handled)
            if (isActive)
            {
                 DeactivatePhysics();
            }
        }
        
        private void TryStickToTarget(Collision2D collision)
        {
            // Removed LayerMask check as requested by user. 
            // Now relies on Physics 2D Matrix for collision filtering.
            
            isStuck = true;
            lifeTimer = stuckLifetime; 
            
            // Debug Position BEFORE Parenting
            Vector3 beforePos = transform.position;
            Vector3 targetPos = collision.transform.position;
            
            // Disable collider
            if (col != null) col.enabled = false;

            transform.SetParent(collision.transform);
            
            // ... (Position adjustment logic)
            if (collision.contactCount > 0)
            {
                Vector2 contactPoint = collision.contacts[0].point;
                // [DISABLED] Manual shift causes visual snap. Rely on Physics stop.
                // Vector3 direction = transform.right; 
                // transform.position = (Vector3)contactPoint - direction * stickImpactOffset;
            }
            
            // Debug Position AFTER Parenting
            Vector3 afterPos = transform.position;
            
            Debug.Log($"[Arrow Stick Debug] \n" +
                      $"- Hit Object: {collision.gameObject.name} (Pos: {targetPos})\n" +
                      $"- Arrow Pos BEFORE: {beforePos}\n" +
                      $"- Arrow Pos AFTER: {afterPos}\n" +
                      $"- Shift Delta: {Vector3.Distance(beforePos, afterPos)}");

            DeactivatePhysics();
        }
        
        private void SetInitialRotation(Vector2 velocity)
        {
            float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle - spriteBaseAngle);
            spriteRenderer.flipY = false; // Reset flip
        }
        
        private void UpdateRotation()
        {
            float angle = Mathf.Atan2(rb.velocity.y, rb.velocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle - spriteBaseAngle);
            // No flipY needed for rotation-based projectile
        }
        
        private void DeactivatePhysics()
        {
            // Debug.Log("[Arrow] DeactivatePhysics called. Stopping and disabling collider.");
            isActive = false;
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.simulated = false; // Disable physics simulation
            
            if (col != null) col.enabled = false; // Ensure collider is ALWAYS disabled
        }
        
        private void TryDealDamage(GameObject target)
        {
            // Debug Log Hit
            // Debug.Log($"[Arrow] Hit: {target.name} | Tag: {target.tag}");

            // 1. Check Ally Tags (Friendly Fire Prevention)
            if (PrefabData.Instance != null)
            {
                // Only skip if ally tags matched AND friendly fire is OFF
                if (!PrefabData.Instance.allowAllyDamage && PrefabData.Instance.allyTags.Contains(target.tag))
                {
                    return; // Ignore allies
                }
            }

            // 2. Deal Damage
            IHealth health = target.GetComponent<IHealth>();
            if (health != null)
            {
                health.TakeDamage(damage);
                hasDealtDamage = true;
                Debug.Log($"[Arrow] Dealt {damage} damage to {target.name} (Tag: {target.tag})");
            }
        }
        
        private void ReturnToPool()
        {
            if (isStuck && transform.parent != null)
            {
                transform.SetParent(null);
                isStuck = false;
            }
            
            if (col != null) col.enabled = true; // Ensure collider is enabled before return (redundant safe)

            if (pool != null)
            {
                isActive = false;
                hasDealtDamage = false;
                
                gameObject.SetActive(false);
                pool.ReturnArrow(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        public void SetPool(ArrowPool poolRef)
        {
            pool = poolRef;
        }
    }
}
