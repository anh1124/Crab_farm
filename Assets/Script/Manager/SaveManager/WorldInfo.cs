namespace Script.Save_Controller
{
    [System.Serializable]
    public class WorldInfo 
    {
        public string Name;  // Đổi từ thuộc tính thành trường công khai
        public int Seed;
        public string CreationDate;
        public string LastPlayedDate;

        public int GameHour;
        public int GameMinute;
        public int GameDay;
        public int GameMonth;
        public int GameYear;

        // Tech upgrade variables moved from PrefabData
        public bool TechFireArrow;    // arrow do fire dame
        public bool TechPenetrating;  // breaking armor
        public bool TechForge;        // new struction that give sword to upgrade Squire to Vanguard
        public bool TechAdhesive;     // make wall better

        public WorldInfo(string name, string creationDate, int seed,
            int gameHour = 0, int gameMinute = 0, int gameDay = 1,
            int gameMonth = 1, int gameYear = 1,
            bool techFireArrow = false, bool techPenetrating = false, 
            bool techForge = false, bool techAdhesive = false,
            string lastPlayedDate = null)
        {
            Name = name;
            CreationDate = creationDate;
            Seed = seed;
            GameHour = gameHour;
            GameMinute = gameMinute;
            GameDay = gameDay;
            GameMonth = gameMonth;
            GameYear = gameYear;
            TechFireArrow = techFireArrow;
            TechPenetrating = techPenetrating;
            TechForge = techForge;
            TechAdhesive = techAdhesive;
            LastPlayedDate = string.IsNullOrEmpty(lastPlayedDate) ? creationDate : lastPlayedDate;
        }
    }
}
