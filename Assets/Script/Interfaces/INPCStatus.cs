using Script.Game.Mobs.Subjects;

namespace Script.Data_Model.Interfaces
{
    public interface INPCStatus
    {
        NPCRole Role { get; }
        int CurrentCoins { get; set; }
    }
}
