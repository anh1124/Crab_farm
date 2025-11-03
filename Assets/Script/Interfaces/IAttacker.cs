namespace Script.Data_Model.Interfaces
{
    public interface IAttacker
    {
        int AttackDame { get; set; }
        float AttackSpeed { get; set; }
        void Attack();
    }
}
