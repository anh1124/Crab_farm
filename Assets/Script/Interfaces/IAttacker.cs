namespace Script.Data_Model.Interfaces
{
    public interface IAttacker
    {
        float AttackDame { get; set; }
        float AttackSpeed { get; set; }
        void Attack();
    }
}
