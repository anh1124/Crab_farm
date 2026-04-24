using UnityEngine;
using Script.Data_Model; // For PrefabData
using Script.Utils; // For DamageHandler and CombatUtility
using Script.Interfaces; // For IDamageable

namespace Script.Game.Combat
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(Audio.AudioEmitter))]
    [DefaultExecutionOrder(500)]
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
        private IArrowPool pool;
        private Audio.AudioEmitter audioEmitter;
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

        private enum FlightMode
        {
            Ballistic = 0,
            GuidedParabola = 1,
        }

        private FlightMode _flightMode = FlightMode.Ballistic;

        // Guided parabola flight (precomputed samples for constant-speed travel)
        private Vector2[] _guidedPoints;
        private float[] _guidedCumDist;
        private float _guidedTotalDist;
        private float _guidedTravelDist;
        private int _guidedSampleIndex;
        private float _guidedSpeed;
        private bool _debugHitLogs;
        private bool _hasLoggedPhysicalHit;
        [SerializeField] private bool useParentingForSticky = true;
        #endregion
        
        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            col = GetComponent<Collider2D>(); // Get Collider
            spriteRenderer = GetComponent<SpriteRenderer>();
            audioEmitter = GetComponent<Audio.AudioEmitter>();
        }
        
        public void Initialize(Vector2 velocity, float launchAngle, float gravityScaleValue, float stickOffsetOverride = -1f, GameObject instigator = null, bool debugHitLogs = false, float damageValue = -1f)
        {
            this.instigator = instigator;
            // [FIX] Nhận damage từ archer (từ SO qua DenizenStatus)
            if (damageValue > 0) this.damage = Mathf.RoundToInt(damageValue);
            hasDealtDamage = false;
            isActive = true;
            isStuck = false;
            stuckTarget = null;
            lifeTimer = 0f;          
            timeSinceSpawn = 0f;     
            _debugHitLogs = debugHitLogs;
            _hasLoggedPhysicalHit = false;

            _flightMode = FlightMode.Ballistic;
            _guidedPoints = null;
            _guidedCumDist = null;
            _guidedTotalDist = 0f;
            _guidedTravelDist = 0f;
            _guidedSampleIndex = 0;
            _guidedSpeed = 0f;
            
            // Set dynamic properties
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.simulated = true; // CRITICAL: Re-enable physics simulation (disabled in DeactivatePhysics)
            rb.mass = 0.0001f; 
            rb.velocity = velocity;
            rb.gravityScale = gravityScaleValue; 
            rb.angularVelocity = 0f;
            
            if (stickOffsetOverride >= 0f) this.stickImpactOffset = stickOffsetOverride;
            
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

        public void InitializeGuidedParabola(Vector2 origin, Vector2 target, float apexY, float travelSpeed, int sampleCount = 48, float stickOffsetOverride = -1f, GameObject instigator = null, bool debugHitLogs = false, float damageValue = -1f)
        {
            this.instigator = instigator;
            // [FIX] Nhận damage từ archer (từ SO qua DenizenStatus)
            if (damageValue > 0) this.damage = Mathf.RoundToInt(damageValue);
            hasDealtDamage = false;
            isActive = true;
            isStuck = false;
            stuckTarget = null;
            lifeTimer = 0f;
            timeSinceSpawn = 0f;

            _flightMode = FlightMode.GuidedParabola;
            _debugHitLogs = debugHitLogs;
            _hasLoggedPhysicalHit = false;

            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.simulated = false; // fully scripted motion

            if (stickOffsetOverride >= 0f) this.stickImpactOffset = stickOffsetOverride;

            if (col != null)
            {
                col.enabled = true;
                col.isTrigger = true; // rely on scripted linecast hit detection
            }

            transform.position = origin;

            int n = Mathf.Clamp(sampleCount, 8, 128);
            _guidedPoints = new Vector2[n];
            _guidedCumDist = new float[n];

            for (int i = 0; i < n; i++)
            {
                float t = (float)i / (n - 1);
                _guidedPoints[i] = EvaluateYellowCurve(origin, target, apexY, t);
                if (i == 0) _guidedCumDist[i] = 0f;
                else _guidedCumDist[i] = _guidedCumDist[i - 1] + Vector2.Distance(_guidedPoints[i - 1], _guidedPoints[i]);
            }

            _guidedTotalDist = _guidedCumDist[n - 1];
            _guidedTravelDist = 0f;
            _guidedSampleIndex = 0;
            _guidedSpeed = Mathf.Max(0f, travelSpeed);

            if (_guidedTotalDist <= 0.001f)
            {
                ReturnToPool();
                return;
            }

            _guidedTravelDist = Mathf.Min(_guidedTotalDist, _guidedSpeed * Time.deltaTime);
            Vector2 initialVel = GetGuidedTangentVelocity(Mathf.Max(0.01f, _guidedSpeed));
            SetInitialRotation(initialVel);

            if (PrefabData.Instance != null && PrefabData.Instance.debugArrow)
                Debug.Log($"[Arrow GuidedInit] origin={origin} target={target} apexY={apexY:F2} speed={travelSpeed:F2} dist={_guidedTotalDist:F2}");
        }

        public void InitializeGuidedTwoPhase(Vector2 origin, Vector2 baseTarget, float baseApexY, Vector2 finalTarget, float travelSpeed, int sampleCount = 64, float stickOffsetOverride = -1f, GameObject instigator = null, bool debugHitLogs = false, float damageValue = -1f)
        {
            // Phase 1: origin -> baseTarget (yellow), with apex at midpoint-x and y=baseApexY.
            // Phase 2: apexPoint (from phase 1) -> finalTarget (red), vertex fixed at apexPoint.
            this.instigator = instigator;
            // [FIX] Nhận damage từ archer (từ SO qua DenizenStatus)
            if (damageValue > 0) this.damage = Mathf.RoundToInt(damageValue);
            hasDealtDamage = false;
            isActive = true;
            isStuck = false;
            stuckTarget = null;
            lifeTimer = 0f;
            timeSinceSpawn = 0f;

            _flightMode = FlightMode.GuidedParabola;
            _debugHitLogs = debugHitLogs;
            _hasLoggedPhysicalHit = false;

            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.simulated = false;

            if (stickOffsetOverride >= 0f) this.stickImpactOffset = stickOffsetOverride;

            if (col != null)
            {
                col.enabled = true;
                col.isTrigger = true; // linecast-driven hit
            }

            transform.position = origin;

            int n = Mathf.Clamp(sampleCount, 16, 160);
            int n1 = Mathf.Clamp(n / 2, 8, n - 8);
            int n2 = n - n1 + 1; // +1 because apex is shared

            Vector2 apexPoint = GetBaseApexPoint(origin, baseTarget, baseApexY);

            _guidedPoints = new Vector2[n1 + n2 - 1];
            _guidedCumDist = new float[_guidedPoints.Length];

            // phase 1 includes apex at end
            for (int i = 0; i < n1; i++)
            {
                float t01 = (float)i / (n1 - 1);
                float t = t01 * 0.5f; // [0..0.5] reach apex exactly
                _guidedPoints[i] = EvaluateYellowCurve(origin, baseTarget, baseApexY, t);
            }

            // phase 2 starts at apexPoint (skip duplicate apex index 0)
            for (int i = 1; i < n2; i++)
            {
                float t = (float)i / (n2 - 1);
                _guidedPoints[n1 - 1 + i] = EvaluateDescentParabola(apexPoint, finalTarget, t);
            }

            for (int i = 0; i < _guidedPoints.Length; i++)
            {
                if (i == 0) _guidedCumDist[i] = 0f;
                else _guidedCumDist[i] = _guidedCumDist[i - 1] + Vector2.Distance(_guidedPoints[i - 1], _guidedPoints[i]);
            }

            _guidedTotalDist = _guidedCumDist[_guidedCumDist.Length - 1];
            _guidedTravelDist = 0f;
            _guidedSampleIndex = 0;
            _guidedSpeed = Mathf.Max(0f, travelSpeed);

            if (_guidedTotalDist <= 0.001f || _guidedSpeed <= 0.001f)
            {
                ReturnToPool();
                return;
            }

            Vector2 initialVel = (_guidedPoints[Mathf.Min(1, _guidedPoints.Length - 1)] - _guidedPoints[0]).normalized * _guidedSpeed;
            SetInitialRotation(initialVel);

            if (PrefabData.Instance != null && PrefabData.Instance.debugArrow)
                Debug.Log($"[Arrow Guided2PhaseInit] baseTarget={baseTarget} apexY={baseApexY:F2} finalTarget={finalTarget} speed={travelSpeed:F2} dist={_guidedTotalDist:F2}");
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
                if (stuckTarget == null)
                {
                    Debug.LogWarning($"[ArrowController] {name}: StuckTarget là null nhưng arrow vẫn stuck! Trả về pool.");
                    ReturnToPool();
                    return;
                }
                
                if (!stuckTarget.gameObject.activeInHierarchy)
                {
                    Debug.Log($"[ArrowController] {name}: StuckTarget {stuckTarget.name} đã inactive, trả arrow về pool.");
                    ReturnToPool();
                    return;
                }

                if (!useParentingForSticky)
                {
                    Vector3 intendedPos = stuckTarget.TransformPoint(stuckLocalPosition);
                    Vector3 actualPos = transform.position;
                    if (_debugHitLogs && Vector3.Distance(intendedPos, actualPos) > 0.01f)
                    {
                        Debug.LogWarning(
                            $"[Stick Jitter] Frame:{Time.frameCount} | Intended:{intendedPos} | Actual:{actualPos} | Delta:{(intendedPos - actualPos)}"
                        );
                    }

                    transform.position = intendedPos;
                    transform.rotation = Quaternion.Euler(0, 0, stuckTarget.eulerAngles.z + stuckLocalRotationDelta);
                }

                lifeTimer -= Time.deltaTime;
                if (lifeTimer <= 0f)
                {
                    ReturnToPool();
                    return;
                }
            }
            
            if (!isActive) return;

            if (!isStuck && Time.frameCount % 2 == 0 && rb.velocity.sqrMagnitude > minVelocityToRotate * minVelocityToRotate)
                UpdateRotation();
        }

        private void LateUpdate()
        {
            if (!isActive) return;
            if (isStuck) return;

            // Run guided flight after other Updates to avoid 1-frame stick jitter.
            if (_flightMode == FlightMode.GuidedParabola)
            {
                UpdateGuidedFlight();
            }
        }
        
        
        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!isActive) return;
            if (_debugHitLogs && !_hasLoggedPhysicalHit)
            {
                _hasLoggedPhysicalHit = true;
                bool willTryDamage = !hasDealtDamage;
                var cp = collision.contactCount > 0 ? collision.GetContact(0) : default;
                Vector2 point = collision.contactCount > 0 ? cp.point : (Vector2)transform.position;
                Vector2 normal = collision.contactCount > 0 ? cp.normal : Vector2.zero;
                Debug.Log(
                    $"[Arrow Hit] (Physics2D Collision)\n" +
                    $"- arrow: {name}\n" +
                    $"- hit: {collision.gameObject.name} (tag={collision.gameObject.tag}, layer={collision.gameObject.layer})\n" +
                    $"- point: {point} | normal: {normal}\n" +
                    $"- willTryDamage: {willTryDamage} | damage: {(willTryDamage ? damage : 0)}\n" +
                    $"- instigator: {(instigator != null ? instigator.name : "null")}"
                );
            }
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

            // Phát âm thanh Arrow_Hit
            audioEmitter?.PlaySound("Arrow_Hit");

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

        private void UpdateGuidedFlight()
        {
            if (_guidedPoints == null || _guidedCumDist == null || _guidedPoints.Length < 2)
            {
                ReturnToPool();
                return;
            }

            float travelSpeed = _guidedSpeed;
            if (travelSpeed <= 0.001f)
            {
                ReturnToPool();
                return;
            }

            Vector2 prevPos = transform.position;
            _guidedTravelDist += travelSpeed * Time.deltaTime;

            if (_guidedTravelDist >= _guidedTotalDist)
            {
                Vector2 endPos = _guidedPoints[_guidedPoints.Length - 1];

                // Final hit check on the last segment to avoid "hit then instantly pool".
                if (TryHandleGuidedHit(prevPos, endPos))
                    return;

                transform.position = endPos;
                ReturnToPool();
                return;
            }

            // Find segment for current distance
            while (_guidedSampleIndex < _guidedCumDist.Length - 2 && _guidedCumDist[_guidedSampleIndex + 1] < _guidedTravelDist)
                _guidedSampleIndex++;

            float d0 = _guidedCumDist[_guidedSampleIndex];
            float d1 = _guidedCumDist[_guidedSampleIndex + 1];
            float segT = (d1 - d0) > 0.0001f ? Mathf.InverseLerp(d0, d1, _guidedTravelDist) : 0f;
            Vector2 nextPos = Vector2.Lerp(_guidedPoints[_guidedSampleIndex], _guidedPoints[_guidedSampleIndex + 1], segT);

            if (TryHandleGuidedHit(prevPos, nextPos))
                return;

            transform.position = nextPos;

            Vector2 tangentVel = (nextPos - prevPos) / Mathf.Max(0.0001f, Time.deltaTime);
            if (tangentVel.sqrMagnitude > minVelocityToRotate * minVelocityToRotate)
                SetInitialRotation(tangentVel);
        }

        private bool TryHandleGuidedHit(Vector2 from, Vector2 to)
        {
            // Scripted collision: cast using the arrow's own collider shape to prevent 1-frame penetration.
            if (!TryGetGuidedHit(from, to, out RaycastHit2D hit))
                return false;

            if (hit.collider == null || hit.collider.transform.root == transform.root)
                return false;

            // Trigger should sticky immediately if it's a valid combat target (Enemy, Animal...) and not ally.
            GameObject hitObj = hit.collider.gameObject;
            bool isValidCombatTarget = Script.Utils.TargetLists.ArcherTargets.Contains(hitObj.tag);
            bool isAlly = Script.Utils.CombatUtility.IsAlly(hitObj);
            bool shouldStickOnTrigger = hit.collider.isTrigger && isValidCombatTarget && !isAlly;

            bool dealtDamageNow = false;
            if (!hasDealtDamage && (shouldStickOnTrigger || !hit.collider.isTrigger))
                dealtDamageNow = TryDealDamage(hitObj);

            // Ignore non-combat triggers (sensors, pickup triggers, etc.)
            if (hit.collider.isTrigger && !shouldStickOnTrigger && !dealtDamageNow && !hasDealtDamage)
                return false;

            Vector2 travelDir = (to - from);
            if (travelDir.sqrMagnitude < 0.000001f) travelDir = Vector2.right;
            travelDir.Normalize();

            // Place the arrow at the cast contact position, then pull back a bit along travel direction.
            // This keeps the arrow outside the hit surface without "snap back".
            float dist = Vector2.Distance(from, to);
            float contactDist = Mathf.Clamp(hit.distance, 0f, dist);
            float pullBack = Mathf.Max(0f, stickImpactOffset);
            float finalDist = Mathf.Max(0f, contactDist - pullBack);
            Vector2 finalPos = from + travelDir * finalDist;
            transform.position = finalPos;

            // In guided mode, if we dealt damage (common for enemy trigger colliders), we still want sticky & cooldown.
            bool treatAsPhysicalForSticky = !hit.collider.isTrigger || shouldStickOnTrigger || dealtDamageNow || hasDealtDamage;

            if (_debugHitLogs && !_hasLoggedPhysicalHit)
            {
                _hasLoggedPhysicalHit = true;
                Debug.Log(
                    $"[Arrow Hit] (Guided Linecast)\n" +
                    $"- arrow: {name}\n" +
                    $"- hit: {hitObj.name} (tag={hitObj.tag}, layer={hitObj.layer}, isTrigger={hit.collider.isTrigger})\n" +
                    $"- from: {from} | to: {to} | dir: {travelDir}\n" +
                    $"- hit.distance: {hit.distance:F3} | stickImpactOffset: {stickImpactOffset:F3} | finalPos: {finalPos}\n" +
                    $"- dealtDamage: {(dealtDamageNow || hasDealtDamage)} | damage: {(dealtDamageNow || hasDealtDamage ? damage : 0)}\n" +
                    $"- stickOnTrigger: {shouldStickOnTrigger}\n" +
                    $"- instigator: {(instigator != null ? instigator.name : "null")}"
                );
            }

            HandleHit(hitObj, isPhysicalHit: treatAsPhysicalForSticky, collision: null, collider: hit.collider);

            // Hard stop guided path immediately on hit (prevents any lingering "follow trajectory" feeling).
            _guidedPoints = null;
            _guidedCumDist = null;
            _guidedTotalDist = 0f;
            return true;
        }

        private bool TryGetGuidedHit(Vector2 from, Vector2 to, out RaycastHit2D hit)
        {
            hit = default;
            Vector2 dir = to - from;
            float dist = dir.magnitude;
            if (dist < 0.0001f) return false;
            dir /= dist;

            // Prefer collider cast (shape-accurate)
            if (col != null && col.enabled)
            {
                ContactFilter2D filter = new ContactFilter2D();
                filter.useLayerMask = false; // keep consistent with previous unmasked Linecast
                filter.useTriggers = true;

                RaycastHit2D[] results = new RaycastHit2D[8];
                int count = col.Cast(dir, filter, results, dist);
                float best = float.PositiveInfinity;
                RaycastHit2D bestHit = default;

                for (int i = 0; i < count; i++)
                {
                    var h = results[i];
                    if (h.collider == null) continue;
                    if (h.collider == col) continue;
                    if (h.collider.transform.root == transform.root) continue;
                    if (h.distance < best)
                    {
                        best = h.distance;
                        bestHit = h;
                    }
                }

                if (best < float.PositiveInfinity)
                {
                    hit = bestHit;
                    return true;
                }
            }

            // Fallback
            hit = Physics2D.Linecast(from, to);
            return hit.collider != null;
        }

        private Vector2 GetGuidedTangentVelocity(float travelSpeed)
        {
            if (_guidedPoints == null || _guidedPoints.Length < 2) return Vector2.right * travelSpeed;
            Vector2 dir = (_guidedPoints[Mathf.Min(1, _guidedPoints.Length - 1)] - _guidedPoints[0]).normalized;
            return dir * travelSpeed;
        }

        private static Vector2 EvaluateYellowCurve(Vector2 origin, Vector2 target, float apexY, float t)
        {
            // Quadratic Bezier that ALWAYS passes origin & target, with y(t=0.5)=apexY.
            t = Mathf.Clamp01(t);
            float xm = 0.5f * (origin.x + target.x);
            float controlY = 2f * apexY - 0.5f * (origin.y + target.y);
            Vector2 p0 = origin;
            Vector2 p1 = new Vector2(xm, controlY);
            Vector2 p2 = target;

            float u = 1f - t;
            return (u * u) * p0 + (2f * u * t) * p1 + (t * t) * p2;
        }

        private static Vector2 GetBaseApexPoint(Vector2 origin, Vector2 target, float apexY)
        {
            float xm = 0.5f * (origin.x + target.x);
            return new Vector2(xm, apexY);
        }

        private static Vector2 EvaluateDescentParabola(Vector2 apexPoint, Vector2 target, float t)
        {
            // Quadratic Bezier: curved descent from apex -> target (not a straight line).
            t = Mathf.Clamp01(t);
            float xm = 0.5f * (apexPoint.x + target.x);
            Vector2 p0 = apexPoint;
            Vector2 p1 = new Vector2(xm, apexPoint.y);
            Vector2 p2 = target;
            float u = 1f - t;
            return (u * u) * p0 + (2f * u * t) * p1 + (t * t) * p2;
        }
        
        private void TryStickToTarget(Collision2D collision)
        {
            isStuck = true;
            lifeTimer = stuckLifetime; 
            
            if (col != null) col.enabled = false;

            stuckTarget = collision.rigidbody != null ? collision.rigidbody.transform : collision.transform;
            if (useParentingForSticky)
            {
                // Parent so Unity keeps it stable with animation/physics order
                transform.SetParent(stuckTarget, worldPositionStays: true);
                stuckLocalPosition = transform.localPosition;
                stuckLocalRotationDelta = transform.localEulerAngles.z;
            }
            else
            {
                stuckLocalPosition = stuckTarget.InverseTransformPoint(transform.position);
                stuckLocalRotationDelta = transform.eulerAngles.z - stuckTarget.eulerAngles.z;
            }
            
            DeactivatePhysics();
        }

        private void TryStickToTrigger(Collider2D collider)
        {
            isStuck = true;
            lifeTimer = stuckLifetime;

            if (col != null) col.enabled = false;

            stuckTarget = collider.attachedRigidbody != null ? collider.attachedRigidbody.transform : collider.transform;
            if (useParentingForSticky)
            {
                transform.SetParent(stuckTarget, worldPositionStays: true);
                stuckLocalPosition = transform.localPosition;
                stuckLocalRotationDelta = transform.localEulerAngles.z;
            }
            else
            {
                stuckLocalPosition = stuckTarget.InverseTransformPoint(transform.position);
                stuckLocalRotationDelta = transform.eulerAngles.z - stuckTarget.eulerAngles.z;
            }

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

        private bool TryDealDamage(GameObject hitObject)
        {
            if (hitObject == null) return false;

            // QUY TẮC: Chỉ gây dame nếu IDamageable/DenizenStatus nằm CÙNG GameObject với Collider (không Resolve lên cha)
            var damageable = hitObject.GetComponent<IDamageable>();
            var denizenStatus = hitObject.GetComponent<Script.Data_Model.Status.DenizenStatus>();
            
            if (damageable == null && denizenStatus == null) return false;

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
                    return true;
                }
            }
            return false;
        }
        
        private void ReturnToPool()
        {
            if (pool == null)
            {
                Debug.LogWarning($"[ArrowController] {name}: pool là null, không thể trả về pool. Destroy arrow.");
                Destroy(gameObject);
                return;
            }

            // Log nếu arrow đã tồn tại quá lâu (bị kẹt)
            if (timeSinceSpawn > maxAirTime * 0.8f)
            {
                Debug.LogWarning($"[ArrowController] {name}: Arrow bị kẹt trong không gian {timeSinceSpawn:F1}s trước khi trả về pool. Stuck={isStuck}, StuckTarget={(stuckTarget != null ? stuckTarget.name : "null")}");
            }

            isStuck = false;
            stuckTarget = null;
            _hasLoggedPhysicalHit = false;
            if (transform.parent != null) transform.SetParent(null, worldPositionStays: true);
            
            if (col != null) col.enabled = true; // Ensure collider is enabled before return (redundant safe)

            isActive = false;
            hasDealtDamage = false;
            
            gameObject.SetActive(false);
            pool.ReturnArrow(gameObject);
        }
        
        public void SetPool(IArrowPool poolRef)
        {
            pool = poolRef;
        }
    }
}
