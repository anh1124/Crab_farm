using UnityEngine;

namespace Script.Data_Model
{
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
