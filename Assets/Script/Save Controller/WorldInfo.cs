using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldInfo 
{
    //đây là class hỗ trợ save thôi
    public string Name;  // Đổi từ thuộc tính thành trường công khai
    public int Seed;
    public System.DateTime CreationDate;

    public int GameHour;
    public int GameMinute;
    public int GameDay;
    public int GameMonth;
    public int GameYear;

    public WorldInfo(string name, System.DateTime creationDate, int seed,
        int gameHour = 0, int gameMinute = 0, int gameDay = 1,
        int gameMonth = 1, int gameYear = 1)
    {
        Name = name;
        CreationDate = creationDate;
        Seed = seed;
        GameHour = gameHour;
        GameMinute = gameMinute;
        GameDay = gameDay;
        GameMonth = gameMonth;
        GameYear = gameYear;
    }
}
