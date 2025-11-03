using Script.Data_Model;

namespace Script.Data_Model.Interfaces
{
    public interface IDataListBootstrap
    {
        int Order { get; }
        void InitializeData(PrefabData context);
    }
}


