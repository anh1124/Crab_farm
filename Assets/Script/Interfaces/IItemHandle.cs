using UnityEngine;

namespace Script.Data_Model.Interfaces
{
    //script này được gắn trên behavior 
    public interface IItemHandle 
    {
        bool HandleItem(GameObject item);
    }
}
