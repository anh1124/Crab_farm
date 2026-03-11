using UnityEngine;
using Script.Data_Model; // For PrefabData
using Script.Utils; // For DamageHandler and CombatUtility

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

        // Stick tracking (parentless)
        private Transform stuckTarget;
        private Vector3 stuckLocalPosition;
        private float stuckLocalRotationDelta;

        private GameObject instigator; // Added instigator tracking
        #endregion
        
        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            col = GetComponent<Collider2D>(); // Get Collider
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
        
        public void Initialize(Vector2 velocity, float launchAngle, float gravityScaleValue, float stickOffset = 0.4f, GameObject instigator = null)
        {
            this.instigator = instigator;
            hasDealtDamage = false;
            isActive = true;
            isStuck = false;
            stuckTarget = null;
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
            if (PrefabData.Instance != null && PrefabData.Instance.debugArrow)
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
                if (stuckTarget == null || !stuckTarget.gameObject.activeInHierarchy)
                {
                    ReturnToPool();
                    return;
                }

                transform.position = stuckTarget.TransformPoint(stuckLocalPosition);
                transform.rotation = Quaternion.Euler(0, 0, stuckTarget.eulerAngles.z + stuckLocalRotationDelta);

                lifeTimer -= Time.deltaTime;
                if (lifeTimer <= 0f)
                {
                    ReturnToPool();
                    return;
                }
            }
            
            if (!isActive) return;
            
            if (!isStuck && Time.frameCount % 2 == 0 && rb.velocity.sqrMagnitude > minVelocityToRotate * minVelocityToRotate)
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
            if (PrefabData.Instance != null && PrefabData.Instance.debugArrow)
                Debug.Log($"[Arrow] Collision: {collision.gameObject.name} | Tag: {collision.gameObject.tag} | Layer: {LayerMask.LayerToName(collision.gameObject.layer)}");

            HandleHit(collision.gameObject, collision);
        }

        private void OnTriggerEnter2D(Collider2D collider)
        {
            if (!isActive) return;

            // Debug trigger
            if (PrefabData.Instance != null && PrefabData.Instance.debugArrow)
                Debug.Log($"[Arrow] Trigger: {collider.gameObject.name} | Tag: {collider.gameObject.tag} | Layer: {LayerMask.LayerToName(collider.gameObject.layer)}");

            HandleHit(collider.gameObject, null, collider);
        }

        private void HandleHit(GameObject hitObject, Collision2D collision = null, Collider2D collider = null)
        {
            if (!isActive) return;

            // Stop immediately
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;

            // Damage logic (Only once)
            if (!hasDealtDamage)
            {
                TryDealDamage(hitObject);
            }

            // Stick logic
            if (enableSticking && !isStuck)
            {
                if (collision != null) TryStickToTarget(collision);
                else if (collider != null) TryStickToTrigger(collider);
            }

            if (isActive)
            {
                DeactivatePhysics();
            }
        }
        
        private void TryStickToTarget(Collision2D collision)
        {
            isStuck = true;
            lifeTimer = stuckLifetime; 
            
            if (col != null) col.enabled = false;

            stuckTarget = collision.transform;
            stuckLocalPosition = stuckTarget.InverseTransformPoint(transform.position);
            stuckLocalRotationDelta = transform.eulerAngles.z - stuckTarget.eulerAngles.z;
            
            DeactivatePhysics();
        }

        private void TryStickToTrigger(Collider2D collider)
        {
            isStuck = true;
            lifeTimer = stuckLifetime;

            if (col != null) col.enabled = false;

            stuckTarget = collider.transform;
            stuckLocalPosition = stuckTarget.InverseTransformPoint(transform.position);
            stuckLocalRotationDelta = transform.eulerAngles.z - stuckTarget.eulerAngles.z;

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
            if (target == null) return;

            // Resolve target (e.g., if hit a trigger/blocker child)
            GameObject effectiveTarget = CombatUtility.ResolveTargetFromTrigger(target);

            // 1. Check Ally Tags (Friendly Fire Prevention)
            if (PrefabData.Instance != null && !PrefabData.Instance.allowAllyDamage)
            {
                if (CombatUtility.IsAlly(effectiveTarget))
                {
                    return; // Ignore allies
                }
            }

            // 2. Deal Damage via DamageHandler
            if (DamageHandler.ApplyDamage(effectiveTarget, damage, instigator))
            {
                hasDealtDamage = true;
                if (PrefabData.Instance != null && PrefabData.Instance.debugArrow)
                    Debug.Log($"[Arrow] Dealt {damage} damage to {effectiveTarget.name} (Tag: {effectiveTarget.tag}) via DamageHandler");
            }
        }
        
        private void ReturnToPool()
        {
            isStuck = false;
            stuckTarget = null;
            
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
