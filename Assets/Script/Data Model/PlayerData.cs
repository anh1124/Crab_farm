using UnityEngine;

namespace Script.Data_Model
{
    /// <summary>
    /// những data trong script này sẽ được save và load 
    /// </summary>
    [System.Serializable]
    public class PlayerData : MonoBehaviour
    {
        public new string name;
        public int hp;
        public int mp;
        public int xp;
        public int lvl;
        public string nameTileMapPlayerIn;
        public string playerCoordinates;
    }
}
