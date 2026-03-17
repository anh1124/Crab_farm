using UnityEngine;
using UnityEngine.UI;
using Script.Data_Model.Status;

public class HealBarUI : MonoBehaviour
{
    [Header("Components")]
    [Tooltip("LayoutElement của object Mid bên trong Container")]
    public LayoutElement mid;

    [Tooltip("Image của Fill, dùng Image Type = Filled / Horizontal")]
    public Image fill;

    [Tooltip("Kéo thả Script PlayerStatus vào đây, hoặc PlayerBehavior sẽ tự gọi Setup()")]
    public PlayerStatus playerStatus;

    [Header("Scale Settings")]
    [Tooltip("Bao nhiêu MaxHP tương ứng với 1 đơn vị baseMidWidth.\n" +
             "Ví dụ: baseMaxHP=10, baseMidWidth sẽ lấy từ GameObject lúc Start.\n" +
             "Khi MaxHP tăng từ 10→20, Mid dài ra x2. Tối đa x maxScaleMultiplier lần.")]
    public float baseMaxHP = 10f;

    [Tooltip("Thanh máu có thể dài tối đa bao nhiêu lần so với baseMidWidth")]
    public float maxScaleMultiplier = 3f;

    [Header("Base Size (set thủ công theo kích thước thiết kế trong Prefab)")]
    [Tooltip("Chiều rộng của Mid khi MaxHP = baseMaxHP. Nhìn vào Inspector của GameObject Mid, lấy giá trị Width là bao nhiêu thì điền vào đây.")]
    public float baseMidWidth = 100f;

    [Tooltip("Chiều cao của Mid. Điền đúng Height của GameObject Mid trong Prefab.")]
    public float baseMidHeight = 20f;

    // Tự tính tối đa dựa trên baseMidWidth * maxScaleMultiplier
    private float maxMidWidth;

    private float debugLastHP    = -1f;
    private float debugLastMaxHP = -1f;

    private void InitWidths()
    {
        if (mid == null) return;

        // Dùng trực tiếp giá trị từ Inspector — đơn giản, chắc chắn, không phụ thuộc vào Layout timing
        maxMidWidth = baseMidWidth * maxScaleMultiplier;

        // Set preferredHeight để HorizontalLayoutGroup không co Mid về 0 khi bật "Control Child Size - Height"
        mid.preferredHeight = baseMidHeight;

        Debug.Log($"[HealBarUI] InitWidths: baseMidWidth={baseMidWidth}, baseMidHeight={baseMidHeight}, maxMidWidth={maxMidWidth}");
    }

    private void Start()
    {
        // Nếu PlayerBehavior chưa kịp gọi Setup() thì ẩn UI đợi
        if (playerStatus == null)
        {
            gameObject.SetActive(false);
        }
    }

    public void Setup(PlayerStatus newStatus)
    {
        playerStatus = newStatus;
        gameObject.SetActive(true);

        // Đợi 1 frame để Canvas Layout tính xong rồi mới đọc rect.width / rect.height
        StartCoroutine(InitAfterLayout());
    }

    private System.Collections.IEnumerator InitAfterLayout()
    {
        yield return null;
        InitWidths();
        UpdateHealBar();
    }

    private void Update()
    {
        UpdateHealBar();
    }

    public void UpdateHealBar()
    {
        if (playerStatus == null) return;

        float currentHP = playerStatus.currentHp;
        float maxHP     = playerStatus.maxHp;

        // Debug chỉ khi có thay đổi — tránh spam log
        if (currentHP != debugLastHP || maxHP != debugLastMaxHP)
        {
            float dbgWidth = Mathf.Clamp((maxHP / baseMaxHP) * baseMidWidth, 0f, maxMidWidth);
            Debug.Log($"[HealBarUI] Changed! HP={currentHP}/{maxHP} | baseMaxHP={baseMaxHP} | baseMidWidth={baseMidWidth} | TargetWidth={dbgWidth}");
            debugLastHP    = currentHP;
            debugLastMaxHP = maxHP;
        }

        // === Scale Mid theo MaxHP ===
        if (mid != null && baseMidWidth > 0)
        {
            // (maxHP / baseMaxHP): tỷ lệ so với HP gốc. Ví dụ maxHP=20, baseMaxHP=10 → x2
            float targetWidth = (maxHP / baseMaxHP) * baseMidWidth;
            mid.preferredWidth = Mathf.Clamp(targetWidth, 0f, maxMidWidth);
        }
        else if (mid == null)
        {
            Debug.LogWarning("[HealBarUI] 'mid' chưa được gán trong Inspector!");
        }

        // === Fill máu theo HP hiện tại ===
        if (fill != null)
        {
            fill.fillAmount = (maxHP > 0) ? Mathf.Clamp01(currentHP / maxHP) : 0f;
        }
        else
        {
            Debug.LogWarning("[HealBarUI] 'fill' chưa được gán trong Inspector!");
        }
    }
}
