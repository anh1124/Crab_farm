using System.Collections.Generic;
using Script.Data_Model.Status;
using TMPro;
using UnityEngine;

/// <summary>
/// Quản lý UI đồng coin của Player.
/// 
/// Logic hiển thị slot:
///   - Tối đa 50 coin, 10 slot → mỗi slot đại diện cho 5 coin.
///   - slot[i] sáng (active) khi coin >= (i + 1) * coinsPerSlot
///   - Ví dụ: coin=12 → slot[0](≥5)✓, slot[1](≥10)✓, slot[2](≥15)✗ → 2 slot sáng
/// 
/// Dùng event PlayerStatus.OnCoinChanged — không dùng Update().
/// </summary>
public class CoinUI : MonoBehaviour
{
    [Header("Components")]
    [Tooltip("TextMeshPro hiển thị số coin hiện tại")]
    public TextMeshProUGUI coinText;

    [Tooltip("Danh sách 10 Coin Slot (kéo thả từ trái sang phải / từ trên xuống dưới)")]
    public List<GameObject> coinSlots = new List<GameObject>();

    [Tooltip("Kéo thả PlayerStatus vào đây, hoặc PlayerUISetup sẽ tự gọi Setup()")]
    public PlayerStatus playerStatus;

    [Header("Slot Settings")]
    [Tooltip("Số coin tối đa của Player (mặc định 50)")]
    public int maxCoins = 50;

    // Tính tự động: mỗi slot đại diện cho bao nhiêu coin
    private int coinsPerSlot => (coinSlots.Count > 0) ? Mathf.Max(1, maxCoins / coinSlots.Count) : 5;

    // ─── Setup ───────────────────────────────────────────────────────────────

    private void Start()
    {
        if (playerStatus == null)
            gameObject.SetActive(false);
    }

    /// <summary>
    /// Gọi bởi PlayerUISetup khi Player được khởi tạo.
    /// Đăng ký event và cập nhật UI lần đầu.
    /// </summary>
    public void Setup(PlayerStatus newStatus)
    {
        if (playerStatus != null)
            playerStatus.OnCoinChanged -= OnCoinChanged;

        playerStatus = newStatus;
        playerStatus.OnCoinChanged += OnCoinChanged;

        gameObject.SetActive(true);

        // Đồng bộ maxCoins với PlayerStatus nếu cần
        if (playerStatus.maxCoins != maxCoins)
            playerStatus.maxCoins = maxCoins;

        // Cập nhật UI ngay với giá trị hiện tại
        OnCoinChanged(playerStatus.coin);
    }

    private void OnDestroy()
    {
        if (playerStatus != null)
            playerStatus.OnCoinChanged -= OnCoinChanged;
    }

    // ─── Event Handler ────────────────────────────────────────────────────────

    private void OnCoinChanged(int currentCoin)
    {
        UpdateCoinText(currentCoin);
        UpdateCoinSlots(currentCoin);
    }

    // ─── Display Logic ────────────────────────────────────────────────────────

    private void UpdateCoinText(int currentCoin)
    {
        if (coinText == null) return;
        coinText.text = currentCoin.ToString();
    }

    private void UpdateCoinSlots(int currentCoin)
    {
        if (coinSlots == null || coinSlots.Count == 0) return;

        int perSlot = coinsPerSlot;

        for (int i = 0; i < coinSlots.Count; i++)
        {
            if (coinSlots[i] == null) continue;

            // Slot i sáng khi coin đủ để "lấp đầy" slot đó
            // slot[0] cần >= 5, slot[1] cần >= 10, ..., slot[9] cần >= 50
            bool shouldBeActive = currentCoin >= (i + 1) * perSlot;
            coinSlots[i].SetActive(shouldBeActive);
        }
    }

    // ─── Context Menu (Test) ─────────────────────────────────────────────────

    [ContextMenu("Test: +5 Coin")]
    private void TestAdd5()
    {
        if (playerStatus != null) playerStatus.coin = Mathf.Min(maxCoins, playerStatus.coin + 5);
    }

    [ContextMenu("Test: -5 Coin")]
    private void TestRemove5()
    {
        if (playerStatus != null) playerStatus.coin = Mathf.Max(0, playerStatus.coin - 5);
    }

    [ContextMenu("Test: Max Coin")]
    private void TestMaxCoin()
    {
        if (playerStatus != null) playerStatus.coin = maxCoins;
    }

    [ContextMenu("Test: Reset Coin")]
    private void TestResetCoin()
    {
        if (playerStatus != null) playerStatus.coin = 0;
    }
}
