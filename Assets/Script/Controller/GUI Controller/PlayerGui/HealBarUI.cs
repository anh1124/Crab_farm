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
             "Ví dụ: baseMaxHP=10 + baseMidWidth=100 thì MaxHP 10→100px, 20→200px.")]
    public float baseMaxHP = 10f;

    [Tooltip("Thanh máu có thể dài tối đa bao nhiêu lần so với baseMidWidth")]
    public float maxScaleMultiplier = 3f;

    [Header("Base Size (set theo kích thước Mid trong Prefab)")]
    [Tooltip("Chiều rộng Mid khi MaxHP = baseMaxHP. Nhìn vào RectTransform của Mid để lấy giá trị.")]
    public float baseMidWidth = 100f;

    [Tooltip("Chiều cao của Mid. Điền đúng Height của GameObject Mid trong Prefab.")]
    public float baseMidHeight = 20f;

    private float maxMidWidth;

    // ─── Setup ───────────────────────────────────────────────────────────────

    private void Start()
    {
        if (playerStatus == null)
            gameObject.SetActive(false);
    }

    /// <summary>
    /// Gọi bởi PlayerBehavior khi Player được tạo.
    /// Đăng ký lắng nghe event HP và cập nhật UI lần đầu.
    /// </summary>
    public void Setup(PlayerStatus newStatus)
    {
        // Hủy đăng ký cũ nếu có
        if (playerStatus != null)
            playerStatus.OnHPChanged -= OnHPChanged;

        playerStatus = newStatus;
        playerStatus.OnHPChanged += OnHPChanged;

        gameObject.SetActive(true);

        // Đợi 1 frame để Canvas Layout tính xong rồi init widths
        StartCoroutine(InitAfterLayout());
    }

    private void OnDestroy()
    {
        if (playerStatus != null)
            playerStatus.OnHPChanged -= OnHPChanged;
    }

    // ─── Init ─────────────────────────────────────────────────────────────────

    private System.Collections.IEnumerator InitAfterLayout()
    {
        yield return null; // Đợi 1 frame
        InitWidths();
        // Cập nhật lần đầu với giá trị hiện tại
        OnHPChanged(playerStatus.currentHp, playerStatus.maxHp);
    }

    private void InitWidths()
    {
        if (mid == null) return;

        maxMidWidth = baseMidWidth * maxScaleMultiplier;
        mid.preferredHeight = baseMidHeight;
    }

    // ─── Event Handler ────────────────────────────────────────────────────────

    private void OnHPChanged(float currentHP, float maxHP)
    {
        UpdateMidWidth(maxHP);
        UpdateFill(currentHP, maxHP);
    }

    // ─── Display Logic ────────────────────────────────────────────────────────

    private void UpdateMidWidth(float maxHP)
    {
        if (mid == null)
        {
            Debug.LogWarning("[HealBarUI] 'mid' chưa được gán trong Inspector!");
            return;
        }

        if (baseMidWidth <= 0) return;

        float targetWidth = (maxHP / baseMaxHP) * baseMidWidth;
        mid.preferredWidth = Mathf.Clamp(targetWidth, 0f, maxMidWidth);
    }

    private void UpdateFill(float currentHP, float maxHP)
    {
        if (fill == null)
        {
            Debug.LogWarning("[HealBarUI] 'fill' chưa được gán trong Inspector!");
            return;
        }

        fill.fillAmount = (maxHP > 0) ? Mathf.Clamp01(currentHP / maxHP) : 0f;
    }
}
