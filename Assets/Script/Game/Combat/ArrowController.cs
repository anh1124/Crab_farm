using UnityEngine;
using Script.Data_Model; // For PrefabData
using Script.Utils; // For DamageHandler and CombatUtility
using Script.Interfaces; // For IDamageable

namespace Script.Game.Combat
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class ArrowController : MonoBehaviour
    {
        /*
         * =========================================================================
         * QUY TẮC VẬN HÀNH MŨI TÊN (ARROW COMBAT DISCIPLINE):
         * 1. GÂY SÁT THƯƠNG KHU TRÚ (LOCALITY DAMAGE):
         *    - Chỉ gây dame nếu IDamageable hoặc DenizenStatus nằm TRÊN CÙNG GameObject với Collider.
         *    - KHÔNG tự động tìm lên Root/Parent (Để tránh bắn nhầm vào các bộ phận không máu).
         * 
         * 2. BỘ LỌC TÁC CHIẾN (COMBAT FILTERING):
         *    - CHỈ gây dame nếu Tag của đối tượng nằm trong danh sách 'ArcherTargets' (Enemy, Animal, v.v.).
         *    - TUYỆT ĐỐI không gây dame lên đồng minh (IsAlly == true), ví dụ: Wall, Player.
         * 
         * 3. CƠ CHẾ CẮM TÊN (STICKY PHYSICS):
         *    - CHỈ cắm tên (Sticky) khi xảy ra va chạm vật lý thực sự (Collision physic2d).
         *    - KHÔNG cắm tên khi đi xuyên qua các vùng Trigger (Vùng nhận diện, vùng kiểm tra).
         * =========================================================================
         */
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
        
        [Header("Lifetime")]
        [SerializeField] private float stuckLifetime = 3f;  // Lifetime AFTER stuck
        [SerializeField] private float maxAirTime = 4f;     // Fallback if flies too long
        #endregion
        
        #region State
        private bool hasDealtDamage = false;
        private bool isActive = true;
        private bool isStuck = false;
        private float lifeTimer = 0f;           
        private float timeSinceSpawn = 0f;      
        
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
        
        
        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!isActive) return;
            // VA CHẠM VẬT LÝ: Có cả gây dame và cắm tên
            HandleHit(collision.gameObject, isPhysicalHit: true, collision: collision, collider: null);
        }

        private void OnTriggerEnter2D(Collider2D collider)
        {
            if (!isActive) return;
            // TRIGGER: Chỉ gây dame, KHÔNG cắm tên (Sticky)
            HandleHit(collider.gameObject, isPhysicalHit: false, collision: null, collider: collider);
        }

        private void HandleHit(GameObject hitObject, bool isPhysicalHit, Collision2D collision = null, Collider2D collider = null)
        {
            if (!isActive) return;

            // Dừng ngay lập tức
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;

            // 1. LUỒNG GÂY SÁT THƯƠNG (Chỉ thực hiện 1 lần)
            if (!hasDealtDamage)
            {
                TryDealDamage(hitObject);
            }

            // 2. LUỒNG CẮM TÊN (STICKY)
            // CHÚ Ý: Chỉ Sticky nếu là va chạm vật lý (Collision), không Sticky lên Trigger
            if (enableSticking && !isStuck && isPhysicalHit)
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
            spriteRenderer.flipY = false;
        }

        private void UpdateRotation()
        {
            float angle = Mathf.Atan2(rb.velocity.y, rb.velocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle - spriteBaseAngle);
        }

        private void DeactivatePhysics()
        {
            isActive = false;
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.simulated = false; // Disable physics simulation
            
            if (col != null) col.enabled = false; // Ensure collider is ALWAYS disabled
        }

        private void TryDealDamage(GameObject hitObject)
        {
            if (hitObject == null) return;

            // QUY TẮC: Chỉ gây dame nếu IDamageable/DenizenStatus nằm CÙNG GameObject với Collider (không Resolve lên cha)
            var damageable = hitObject.GetComponent<IDamageable>();
            var denizenStatus = hitObject.GetComponent<Script.Data_Model.Status.DenizenStatus>();
            
            if (damageable == null && denizenStatus == null) return;

            // KIỂM TRA TAG: Đối tượng phải thuộc danh sách mục tiêu săn bắn hợp lệ
            string targetTag = hitObject.tag;
            bool isValidCombatTarget = Script.Utils.TargetLists.ArcherTargets.Contains(targetTag);
            
            // CHỐNG BẮN NHẦM: Không gây dame lên đồng minh (Wall, Player, Archer...)
            bool isAlly = Script.Utils.CombatUtility.IsAlly(hitObject);

            if (isValidCombatTarget && !isAlly)
            {
                if (DamageHandler.ApplyDamage(hitObject, (float)damage, instigator))
                {
                    hasDealtDamage = true;
                    if (PrefabData.Instance != null && PrefabData.Instance.debugArrow)
                        Debug.Log($"[Arrow] Dealt {damage} damage to {hitObject.name} (Tag: {targetTag})");
                }
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
