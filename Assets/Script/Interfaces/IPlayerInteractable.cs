using Script.Game.Player;

namespace Script.Interfaces
{
    /// <summary>
    /// Interface cho các đối tượng mà Player có thể tương tác (ví dụ: CoinUser, NPC, Building).
    /// Các đối tượng implement interface này sẽ được PlayerBehavior tự động phát hiện
    /// khi Player đi vào vùng trigger của chúng.
    /// </summary>
    public interface IPlayerInteractable
    {
        void OnPlayerEnter(PlayerBehavior player);
        void OnPlayerExit(PlayerBehavior player);
        bool CanInteract();
    }
}
