namespace Script.Data_Model.Interfaces
{
    public interface ISaveable
    {
        string GetPrefabID(); // ID để xác định prefab cần tải
        object CaptureData(); // Trả về dữ liệu cần lưu
        void RestoreData(object data); // Khôi phục dữ liệu
    }
}