// ============================================================
// PATCH: PlayerInteraction.cs
// Thay thế toàn bộ method UpdateCurrentInteractable() hiện tại
// bằng version dưới đây
// ============================================================

private void UpdateCurrentInteractable()
{
    // Clean up destroyed interactables
    interactablesInRange.RemoveAll(i => i == null || (i is MonoBehaviour mb && mb == null));

    if (interactablesInRange.Count == 0)
    {
        if (currentInteractable != null)
        {
            currentInteractable.OnPlayerExit(playerBehavior);
            currentInteractable = null;
        }

        // Không còn interactable nào → tắt pulse
        PlayerGui.Instance?.StopGoldPulse();
        return;
    }

    // Tìm candidate hợp lệ gần nhất
    IPlayerInteractable candidate = null;
    for (int i = interactablesInRange.Count - 1; i >= 0; i--)
    {
        if (interactablesInRange[i].CanInteract())
        {
            candidate = interactablesInRange[i];
            break;
        }
    }

    // Switch candidate nếu khác
    if (currentInteractable != candidate)
    {
        if (currentInteractable != null)
            currentInteractable.OnPlayerExit(playerBehavior);

        currentInteractable = candidate;

        if (currentInteractable != null)
            currentInteractable.OnPlayerEnter(playerBehavior);
    }

    // ── Gold Pulse Logic ──────────────────────────────────────
    // Kiểm tra chính xác: currentInteractable có phải CoinUser
    // đang thực sự hiển thị slot không (không phải chỉ là CoinUser bất kỳ)
    bool shouldPulse = currentInteractable is Script.Game.Item.Coin.CoinUser coinUser
                       && coinUser.IsInteractionActive;

    if (shouldPulse)
        PlayerGui.Instance?.StartGoldPulse();
    else
        PlayerGui.Instance?.StopGoldPulse();
    // ─────────────────────────────────────────────────────────
}
