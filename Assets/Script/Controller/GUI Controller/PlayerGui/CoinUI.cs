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

    [Header("Visual Settings")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color fullColor = Color.red;
    [SerializeField] private Color addColor = Color.yellow;
    [SerializeField] private Color subColor = Color.red;
    [SerializeField] private float colorFlashDuration = 0.5f;
    [SerializeField] private float shakeIntensity = 5f;
    [SerializeField] private float shakeDuration = 0.2f;

    private int _lastCoinValue = -1;
    private Coroutine _flashCoroutine;
    private Coroutine _shakeCoroutine;
    private Vector3 _originalTextPosition;
    private RectTransform _textRectTransform;

    // ─── Setup ───────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (coinText != null)
        {
            _textRectTransform = coinText.GetComponent<RectTransform>();
            _originalTextPosition = _textRectTransform.anchoredPosition;
        }
    }

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

        // Khởi tạo giá trị ban đầu
        _lastCoinValue = playerStatus.coin;
        UpdateCoinDisplay(_lastCoinValue, false);
    }

    private void OnDestroy()
    {
        if (playerStatus != null)
            playerStatus.OnCoinChanged -= OnCoinChanged;
    }

    // ─── Event Handler ────────────────────────────────────────────────────────

    private void OnCoinChanged(int currentCoin)
    {
        bool isAdd = currentCoin > _lastCoinValue;
        bool isSub = currentCoin < _lastCoinValue;

        UpdateCoinDisplay(currentCoin, true, isAdd, isSub);
        _lastCoinValue = currentCoin;
    }

    // ─── Display Logic ────────────────────────────────────────────────────────

    private void UpdateCoinDisplay(int currentCoin, bool playFeedback, bool isAdd = false, bool isSub = false)
    {
        if (coinText == null) return;

        coinText.text = currentCoin.ToString();

        // Kiểm tra trạng thái full
        bool isFull = playerStatus != null && currentCoin >= playerStatus.maxCoins;

        if (playFeedback)
        {
            if (isAdd)
            {
                TriggerFlash(addColor);
            }
            else if (isSub)
            {
                TriggerFlash(subColor);
                TriggerShake();
            }
        }
        else
        {
            // Nếu không có feedback (lúc init), đặt màu dựa trên trạng thái full
            coinText.color = isFull ? fullColor : normalColor;
        }
    }

    private void TriggerFlash(Color flashColor)
    {
        if (_flashCoroutine != null) StopCoroutine(_flashCoroutine);
        _flashCoroutine = StartCoroutine(FlashRoutine(flashColor));
    }

    private IEnumerator FlashRoutine(Color targetColor)
    {
        coinText.color = targetColor;
        
        float elapsed = 0f;
        while (elapsed < colorFlashDuration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Sau khi flash xong, quay về màu cơ bản (Red nếu full, White nếu không)
        bool isFull = playerStatus != null && playerStatus.coin >= playerStatus.maxCoins;
        coinText.color = isFull ? fullColor : normalColor;
        _flashCoroutine = null;
    }

    private void TriggerShake()
    {
        if (_shakeCoroutine != null) StopCoroutine(_shakeCoroutine);
        _shakeCoroutine = StartCoroutine(ShakeRoutine());
    }

    private IEnumerator ShakeRoutine()
    {
        if (_textRectTransform == null) yield break;

        float elapsed = 0f;
        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            Vector2 randomOffset = Random.insideUnitCircle * shakeIntensity;
            _textRectTransform.anchoredPosition = _originalTextPosition + (Vector3)randomOffset;
            yield return null;
        }

        _textRectTransform.anchoredPosition = _originalTextPosition;
        _shakeCoroutine = null;
    }
}
