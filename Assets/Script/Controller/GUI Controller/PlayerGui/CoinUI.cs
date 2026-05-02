using System.Collections;
using System.Collections.Generic;
using Script.Data_Model.Status;
using TMPro;
using UnityEngine;

/// <summary>
/// Quản lý UI đồng coin của Player (chỉ hiển thị Text).
/// 
/// Dùng event PlayerStatus.OnCoinChanged — không dùng Update().
/// </summary>
public class CoinUI : MonoBehaviour
{
    [Header("Components")]
    [Tooltip("TextMeshPro hiển thị số coin hiện tại")]
    public TextMeshProUGUI coinText;

    [Tooltip("Kéo thả PlayerStatus vào đây, hoặc PlayerUISetup sẽ tự gọi Setup()")]
    public PlayerStatus playerStatus;

    // ─── Setup ───────────────────────────────────────────────────────────────

    private void Start()
    {
        if (playerStatus == null)
        {
            StartCoroutine(FindPlayerStatusRoutine());
        }
    }

    private IEnumerator FindPlayerStatusRoutine()
    {
        while (playerStatus == null)
        {
            playerStatus = FindObjectOfType<PlayerStatus>();
            if (playerStatus != null)
            {
                Setup(playerStatus);
                yield break;
            }
            yield return new WaitForSeconds(2f);
        }
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
    }

    // ─── Display Logic ────────────────────────────────────────────────────────

    private void UpdateCoinText(int currentCoin)
    {
        if (coinText == null) return;
        coinText.text = currentCoin.ToString();
    }
}

