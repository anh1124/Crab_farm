namespace Script.Data_Model.Interfaces
{
    public interface ISaveable
    {
        //interface này chỉ được để trong behavior
        
        string GetPrefabID(); // ID để xác định prefabWall cần tải
        object CaptureData(); // Trả về dữ liệu cần lưu
        void RestoreData(object data); // Khôi phục dữ liệu
    }
}