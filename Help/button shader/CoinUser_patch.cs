// ============================================================
// PATCH: CoinUser.cs
// Thêm vào sau dòng: public bool playerTriggerActive => playerTrigger;
// ============================================================

/// <summary>
/// True khi player đang trong vùng VÀ CoinSlot đang thực sự hiển thị.
/// Dùng bởi PlayerInteraction để kích hoạt GoldPulse trên useCoin button.
/// </summary>
public bool IsInteractionActive =>
    playerTrigger
    && owner != null
    && owner.CanReceiveCoins()
    && owner.GetRequiredCoinCount() > 0;
