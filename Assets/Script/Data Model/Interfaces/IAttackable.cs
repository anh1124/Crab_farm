namespace Script.Data_Model.Interfaces
{
    public interface IAttackable
    {
        int AttackDame { get; set; }
        float AttackSpeed { get; set; }
        void Attack();
    
    }
}
