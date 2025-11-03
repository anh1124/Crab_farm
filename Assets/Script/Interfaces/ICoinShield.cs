namespace Script.Interfaces
{
    // Entities that can use coin as a one-hit shield
    public interface ICoinShield
    {
        // Current coin count available to use as shield
        int CurrentCoin { get; set; }
        // Whether entity has any coin to shield
        bool HasCoinShield { get; }
        // Consume one coin for shielding; returns true if consumed
        bool ConsumeCoinShield();
    }
}