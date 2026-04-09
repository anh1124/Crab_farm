namespace Script.Manager.WorldManager
{
    public interface ITechProvider
    {
        void UpdateTechStatus(bool fireArrow, bool penetrating, bool forge, bool adhesive);
        void SetFireArrowTech(bool value);
        void SetPenetratingTech(bool value);
        void SetForgeTech(bool value);
        void SetAdhesiveTech(bool value);
    }
}
