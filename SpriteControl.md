# DIEU KHIEN SPRITE PORTAL
**So sanh phuong phap & giai phap trien khai - Unity 2D**

---

## 1. Bai toan cu the

Portal co cau truc 2 layer de tao hieu ung nhan vat dung giua lop khoi. Ca hai layer dung cung bo 20 frame sprite (tach tu GIF). Yeu cau: hai layer luon hien thi dung frame, dong bo voi nhau, va co the thay doi toc do theo trang thai portal.

**Hierarchy:**
```
Portal (root)
- Visual
  - Back   [SpriteRenderer]  <- Sorting Order: 0
  - Front  [SpriteRenderer]  <- Sorting Order: 20

Player: Sorting Order 10  (bi kep giua Back va Front)
```

---

## 2. So sanh cac phuong phap

| Tieu chi | Animator x2 (rieng) | Animator x1 (chung) | Script tu viet | Particle System |
|---|---|---|---|---|
| Sync 2 layer | Khong dam bao | Dam bao | Dam bao | N/A |
| Do phuc tap setup | Cao | Trung binh | Thap | Cao |
| Kiem soat state (Dormant/Active) | Kho | Duoc | De, hoan toan | Rat kho |
| Thay doi frameRate runtime | Kho dong bo | Duoc | Thay 1 bien | Duoc |
| Can mo Animation Window | Co | Co | Khong | Khong |
| Overhead | 2 state machine | 1 state machine | Gan bang 0 | Cao |
| Phu hop cho portal nay | Khong | Duoc nhung thua | KHUYEN NGHI | Khong |

---

## 3. Phan tich chi tiet tung phuong phap

### 3.1 Animator rieng cho moi layer - KHONG khuyen nghi

Dat mot Animator len Back va mot Animator len Front, moi cai co AnimationClip rieng.

**Van de ky thuat:**
- Khi lag spike hoac frame drop, moi Animator co the nhay so frame khac nhau, dan den lech hien thi.
- Khi dung Time.timeScale = 0 roi resume, hai Animator khong dam bao resume dong bo.
- De doi toc do animation, phai set speed cho 2 Animator rieng, khong dam bao dong bo.
- Thu tu Update giua hai Animator khong duoc dam bao trong cung frame, co the xuat hien popping.

**Ket luan:** Khong phu hop cho bat ky hieu ung nao yeu cau nhieu layer dong bo.

---

### 3.2 Mot Animator chung bind 2 SpriteRenderer - Chap nhan duoc nhung thua

Dat mot Animator len node Visual. Trong AnimationClip, bind ca hai property: Back/SpriteRenderer.Sprite va Front/SpriteRenderer.Sprite. Unity Animation Window ho tro dieu nay.

**Uu diem:**
- Dong bo hoan toan vi chay trong mot clip.
- Co the dung state machine (Dormant, Active, Breaking) cua Animator.

**Nhuoc diem:**
- Phai mo Animation Window, tao AnimationClip thu cong, record tung frame.
- Khi muon thay sprite set, phai tao lai clip moi.
- Khong the doi frameRate bang code runtime mot cach tu nhien.
- Animator mang theo state machine, transition graph va overhead khong can thiet cho vong lap don gian.

**Ket luan:** Kha thi nhung la over-engineering. Phu hop hon khi portal co animation phuc tap ve hinh dang, khong chi toc do.

---

### 3.3 Script tu viet - KHUYEN NGHI

Mot script duy nhat gan vao node Visual, tu doi sprite cho ca Back va Front theo cung index trong Update. Khong can Animator, khong can AnimationClip.

**Tai sao day la giai phap tot nhat:**
- Sync tuyet doi: ca hai renderer nhan cung frame index trong cung 1 loi goi Update.
- Kiem soat hoan toan: frameRate, pause, reverse, speed multiplier deu la 1 bien.
- Tich hop truc tiep voi portal state: Dormant dung frameRate thap, Active dung cao.
- Khong can mo Animation Window. Drag sprite array vao Inspector la xong.
- Day la ky thuat flipbook sprite tieu chuan cho VFX 2D (khoi, lua, nuoc, portal).

---

### 3.4 Particle System - Khong phu hop

Portal 2-layer voi sprite frame chinh xac khong phai use case cua Particle System. Khong the kiem soat frame dong bo, khong phu hop voi cau truc Front/Back.

---

## 4. Giai phap trien khai: PortalVisualAnimator.cs

```csharp
using UnityEngine;

public class PortalVisualAnimator : MonoBehaviour
{
    [Header("Sprite Frames")]
    public Sprite[] backFrames;
    public Sprite[] frontFrames;
    // backFrames va frontFrames phai co cung so phan tu (20 frame)

    [Header("Renderers")]
    public SpriteRenderer backRenderer;
    public SpriteRenderer frontRenderer;

    [Header("Animation Settings")]
    public float dormantFrameRate = 6f;
    public float activeFrameRate  = 14f;

    private float frameRate;
    private float timer;
    private int currentFrame;

    void Awake()
    {
        SetDormant();
    }

    void Update()
    {
        if (backFrames == null || backFrames.Length == 0) return;

        timer += Time.deltaTime;
        if (timer >= 1f / frameRate)
        {
            timer -= 1f / frameRate;
            currentFrame = (currentFrame + 1) % backFrames.Length;

            // Ca hai renderer nhan cung frame trong cung 1 loi goi -> sync tuyet doi
            backRenderer.sprite  = backFrames[currentFrame];
            frontRenderer.sprite = frontFrames[currentFrame];
        }
    }

    // Goi tu EnemyPortalBehavior.Setup()
    public void SetDormant()
    {
        frameRate = dormantFrameRate;
        timer = 0f;
    }

    // Goi tu EnemyPortalBehavior.OnNightStart() khi portal chuyen Active
    public void SetActive()
    {
        frameRate = activeFrameRate;
        timer = 0f;
    }
}
```

---

## 5. Tich hop voi EnemyPortalBehavior

```csharp
private PortalVisualAnimator visualAnimator;

void Awake()
{
    visualAnimator = GetComponentInChildren<PortalVisualAnimator>();
}

public void Setup(int delay)
{
    // ... logic khac ...
    visualAnimator.SetDormant();
}

public void OnNightStart()
{
    if (State == PortalState.Dormant && delayDays <= 0)
    {
        State = PortalState.Active;
        visualAnimator.SetActive();
    }
}
```

---

## 6. ScreenShakeController.cs

Script gan len MainCamera, cung cap singleton de bat ky script nao cung goi duoc.

```csharp
using UnityEngine;
using System.Collections;

public class ScreenShakeController : MonoBehaviour
{
    public static ScreenShakeController Instance { get; private set; }
    private Vector3 originalPos;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        originalPos = transform.localPosition;
    }

    public void Shake(float duration, float intensity)
    {
        StopAllCoroutines();
        StartCoroutine(DoShake(duration, intensity));
    }

    IEnumerator DoShake(float duration, float intensity)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * intensity;
            float y = Random.Range(-1f, 1f) * intensity;
            transform.localPosition = originalPos + new Vector3(x, y, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.localPosition = originalPos;
    }
}
```

**Cach dung tu portal:**
```csharp
// Trong EnemyPortalBehavior.Setup():
ScreenShakeController.Instance.Shake(0.4f, 0.5f);
```

> **Luu y:** Script nay gan truc tiep len Camera transform. Neu dung Cinemachine thi can dung CinemachineImpulse thay the.

---

## 7. Checklist trien khai

- [ ] Tach 20 frame tu GIF trong Aseprite, xuat sprite sheet, slice trong Unity
- [ ] Tao hai mang sprite rieng: BackFrames[20] va FrontFrames[20]
- [ ] Hierarchy: Portal > Visual > Back (SpriteRenderer) + Front (SpriteRenderer)
- [ ] Sorting Layer: Back = 0, Player = 10, Front = 20
- [ ] Gan PortalVisualAnimator len node Visual
- [ ] Keo backRenderer va frontRenderer vao Inspector
- [ ] Keo 20 sprite vao backFrames va 20 sprite vao frontFrames
- [ ] Gan ScreenShakeController len MainCamera
- [ ] Goi visualAnimator.SetDormant() trong Setup() va SetActive() trong OnNightStart()
