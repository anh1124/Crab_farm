namespace Script.Data_Model.Interfaces
{
    
    public interface ICoinable 
    {
        int Coin { get; set; }
        int MaxCoins { get; set; }
        void SubtractCoin(int amount);//bớt coin
        void AddCoin(int amount);//thêm coin
     
    }
}
