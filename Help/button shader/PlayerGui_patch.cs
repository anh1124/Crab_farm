// ============================================================
// PATCH: PlayerGui.cs
// Thêm các field và method sau vào class PlayerGui
// ============================================================

// --- Gold Pulse Fields ---
// Thêm vào khu vực [Header("other Button")]

private Coroutine _goldPulseCoroutine;
private static readonly Color _goldColor    = new Color(1f, 0.84f, 0f, 1f); // #FFD700
private static readonly Color _normalColor  = Color.white;

// Cường độ vàng: 30% → 50% (Lerp từ trắng sang vàng)
private const float PulseMinIntensity = 0.3f;
private const float PulseMaxIntensity = 0.5f;
private const float PulseDuration     = 2f; // 1 chu kỳ = 2 giây

// ─────────────────────────────────────────────
//  Gold Pulse API — gọi bởi PlayerInteraction
// ─────────────────────────────────────────────

/// <summary>
/// Bắt đầu hiệu ứng nhấp nháy vàng trên nút useCoin.
/// Gọi khi player đứng gần CoinUser đang hiển thị CoinSlot.
/// </summary>
public void StartGoldPulse()
{
    if (_goldPulseCoroutine != null) return; // Đang chạy rồi, không start lại

    _goldPulseCoroutine = StartCoroutine(PulseGoldRoutine());
}

/// <summary>
/// Dừng hiệu ứng và reset màu về trắng bình thường.
/// Gọi khi player rời khỏi vùng CoinUser hoặc slot đã đủ xu.
/// </summary>
public void StopGoldPulse()
{
    if (_goldPulseCoroutine == null) return;

    StopCoroutine(_goldPulseCoroutine);
    _goldPulseCoroutine = null;

    // Reset về normalColor trắng qua ColorBlock để tránh conflict với Color Tint
    ApplyNormalColorToButton(_normalColor);
}

/// <summary>
/// Coroutine tạo hiệu ứng pulse: cường độ vàng dao động 30%–50% trong 2 giây/chu kỳ.
/// Dùng ColorBlock.normalColor để không bị Unity Color Tint override.
/// </summary>
private IEnumerator PulseGoldRoutine()
{
    while (true)
    {
        // Nửa chu kỳ đầu: tăng từ min → max
        yield return LerpNormalColor(PulseMinIntensity, PulseMaxIntensity, PulseDuration * 0.5f);

        // Nửa chu kỳ sau: giảm từ max → min
        yield return LerpNormalColor(PulseMaxIntensity, PulseMinIntensity, PulseDuration * 0.5f);
    }
}

/// <summary>
/// Lerp intensity của gold color trong khoảng thời gian duration.
/// </summary>
private IEnumerator LerpNormalColor(float fromIntensity, float toIntensity, float duration)
{
    float elapsed = 0f;

    while (elapsed < duration)
    {
        elapsed += Time.deltaTime;
        float t         = Mathf.Clamp01(elapsed / duration);
        float intensity = Mathf.Lerp(fromIntensity, toIntensity, t);
        Color blended   = Color.Lerp(_normalColor, _goldColor, intensity);

        ApplyNormalColorToButton(blended);

        yield return null;
    }
}

/// <summary>
/// Set ColorBlock.normalColor của useCoin button.
/// Dùng ColorBlock thay vì image.color trực tiếp để không bị
/// Unity Color Tint state machine override khi hover/press.
/// </summary>
private void ApplyNormalColorToButton(Color color)
{
    if (useCoin == null) return;

    ColorBlock cb    = useCoin.colors;
    cb.normalColor   = color;
    useCoin.colors   = cb;
}
