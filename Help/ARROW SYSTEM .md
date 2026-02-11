

# 🏹 **ARROW SYSTEM – COMPLETE SPECIFICATION (FINAL)**

## **1. GAMEPLAY OVERVIEW**

**Core Mechanic:**
- Arrow bay theo quỹ đạo parabolic nhẹ (Kingdom-style)
- Khi hit enemy: check pierce tech
  - Có tech → xuyên qua, tiếp tục bay
  - Không có tech → soft bounce, rơi xuống, stick ground
- Damage chỉ 1 lần per enemy
- Visual feedback rõ ràng cho mọi trạng thái

---

## **2. PREFAB STRUCTURE**

```
Arrow (GameObject)
├─ Layer: Projectiles
├─ Tag: Arrow
│
├─ Transform
├─ SpriteRenderer
│  ├─ Sprite: arrow.png (tip points RIGHT)
│  ├─ Sorting Layer: Projectiles
│  └─ Order in Layer: 5
│
├─ TrailRenderer
│  ├─ Time: 0.12
│  ├─ Width: 0.05
│  ├─ Color: White → Transparent
│  └─ Emitting: false (enabled on spawn)
│
├─ CapsuleCollider2D
│  ├─ isTrigger: true
│  ├─ Direction: Horizontal
│  └─ Size: fit arrow body
│
├─ Rigidbody2D
│  ├─ Body Type: Kinematic
│  ├─ Simulated: true
│  ├─ Interpolate: Interpolate
│  └─ Collision Detection: Continuous
│
└─ ArrowController (Script)
```

---

## **3. PHYSICS LAYERS SETUP**

```
Layers:
- Projectiles (Arrow default)
- Projectiles_Bouncing (Arrow during bounce)
- Enemy
- Ground
- Player
- Shield

Physics2D Collision Matrix:

Projectiles collides with:
  ✓ Enemy
  ✓ Ground  
  ✓ Shield
  ✗ Player
  ✗ Projectiles

Projectiles_Bouncing collides with:
  ✓ Ground ONLY
  ✗ Enemy
  ✗ Everything else
```

---

## **4. STATE MACHINE**

```
States:
- Idle (in pool)
- Flying
- SoftBouncing
- Falling
- Stuck

Flow:
Idle → Flying → [Hit Enemy]
                    ├─ Has Pierce Tech → Continue Flying
                    └─ No Pierce → SoftBouncing → Falling → Stuck → Idle
```

---

## **5. FLIGHT BEHAVIOR (Kingdom-style)**

**Trajectory:**
```cs
// Parabolic với peak ở 30% đường bay
float t = flightTimer / flightDuration;
Vector2 pos = Vector2.Lerp(startPos, targetPos, t);

// Arc using AnimationCurve or formula
float arcT = Mathf.Sin(t * Mathf.PI * 0.6f); // Peak at t=0.3
pos.y += arcHeight * arcT;

transform.position = pos;
```

**Rotation:**
```cs
// Always point toward velocity direction
Vector2 velocity = (transform.position - prevPosition) / Time.deltaTime;
if(velocity.magnitude > 0.1f)
{
    float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
    transform.rotation = Quaternion.Euler(0, 0, angle);
}
```

**Parameters:**
- Base Speed: 16 units/sec
- Arc Height: 0.5 units (5-10% of flight distance)
- Flight Duration: distance / baseSpeed

---

## **6. SOFT BOUNCE BEHAVIOR**

**Trigger Condition:**
```cs
if (hit enemy && techPierceCount <= 0)
{
    StartSoftBounce();
}
```

**Bounce Animation:**
```cs
Duration: 0.15 seconds
Distance: incomingSpeed * 0.5f * duration
Direction: -incomingDirection (180° reflect)
No rotation during bounce
Collision: Ignore enemies, detect ground only
```

**Implementation:**
```cs
Vector2 incomingDir = (transform.position - prevPosition).normalized;
Vector2 bounceDir = -incomingDir;

// Clamp to prevent straight down bounce
if(bounceDir.y < -0.5f)
{
    bounceDir.y = -0.5f;
    bounceDir.Normalize();
}

float incomingSpeed = (transform.position - prevPosition).magnitude / Time.deltaTime;
float bounceDistance = incomingSpeed * 0.5f * bounceDuration;

Vector2 bounceEndPos = hitPoint + bounceDir * bounceDistance;
```

---

## **7. FALLING BEHAVIOR**

**Physics:**
```cs
Rigidbody2D.bodyType = Dynamic
gravityScale = 2.5f
Initial velocity = Vector2.zero
angularVelocity = 0
```

**Rotation:**
```cs
// Arrow rotates to point toward falling direction
Vector2 velocity = rb.velocity;
if(velocity.magnitude > 0.1f)
{
    float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
    transform.rotation = Quaternion.Lerp(
        transform.rotation,
        Quaternion.Euler(0, 0, angle),
        Time.deltaTime * 8f // Smooth rotation
    );
}
```

**Safety:**
```cs
If falling for > 2 seconds → Force despawn
```

---

## **8. STICK TO GROUND**

**Orientation Logic:**
```cs
float angle = transform.rotation.eulerAngles.z;

// Normalize to -180 to 180
if(angle > 180) angle -= 360;

// Check if pointing down (70° to 110° or -110° to -70°)
bool isPointingDown = (angle >= 70 && angle <= 110) || 
                       (angle >= -110 && angle <= -70);

if(isPointingDown)
{
    // Stick into ground
    transform.rotation = Quaternion.Euler(0, 0, 90);
    sprite.sortingOrder = -1;
}
else
{
    // Lay flat on ground
    // Face direction based on incoming angle
    float flatAngle = (angle < 0) ? 180 : 0;
    transform.rotation = Quaternion.Euler(0, 0, flatAngle);
    sprite.sortingOrder = -1;
}
```

**Despawn:**
```cs
Wait 2 seconds → Return to pool
```

---

## **9. PIERCE TECH SYSTEM**

**Data:**
```cs
int maxPierceCount;  // From ArrowData
int currentPierceCount; // Runtime counter
HashSet<GameObject> hitEnemies; // Track hit enemies
```

**Logic:**
```cs
OnTriggerEnter2D(Enemy):
    if hitEnemies.Contains(enemy) → return // Already hit
    
    hitEnemies.Add(enemy)
    DealDamage(enemy)
    
    if currentPierceCount > 0:
        currentPierceCount--
        Continue flying
    else:
        StartSoftBounce()
```

---

## **10. COLLISION HANDLING**

**During Flight:**
```cs
Layer: Projectiles
Collides: Enemy, Ground, Shield

OnTriggerEnter2D:
    - Enemy → Damage + Pierce check
    - Ground → StickToGround()
    - Shield → Deflect (bounce with metallic FX)
```

**During Soft Bounce:**
```cs
Layer: Projectiles_Bouncing
Collides: Ground ONLY

OnTriggerEnter2D:
    - Ground → StickToGround()
```

**During Falling:**
```cs
Layer: Projectiles_Bouncing
Collides: Ground ONLY

OnCollisionEnter2D:
    - Ground → StickToGround()
```

---

## **11. VFX & SFX**

**Flight:**
- Trail active, white color

**Hit Enemy:**
- Small blood/dust particles (3-5)
- SFX: "impact_soft" (pitch 1.0-1.2)
- Trail stops

**Bounce:**
- Same FX as hit enemy
- SFX: "bounce" (pitch 1.3-1.5)

**Hit Ground:**
- Dust particles (2-4)
- SFX: "thud" (pitch 0.8-1.0)

**Shield Deflect:**
- Metallic sparks (4-6)
- SFX: "clang" (pitch 1.2-1.4)

---

## **12. POOLING SYSTEM**

**Configuration:**
```cs
Initial Pool Size: 30
Expand Step: 15
Max Pool Size: 100
```

**Operations:**
```cs
OnSpawn():
    - Reset all states
    - Clear trail
    - Reset pierce count
    - Clear hitEnemies set
    - Enable collider
    - Set layer to Projectiles

OnDespawn():
    - Disable gameObject
    - Clear trail
    - Reset rotation
    - Move to pool container
```

---

tạo các files sau:

1. **ArrowController.cs** - Main controller
2. **ArrowData.cs** - ScriptableObject config
3. **ArrowPool.cs** - Object pooling
4. **ArrowFXHandler.cs** - VFX/SFX management

