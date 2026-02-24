using System;
using System.Collections;
using System.Collections.Generic;
using Script.Game.Enemy;
using UnityEngine;
using Script.Manager.WorldManager;

namespace Script.Manager.GamePlayManager
{
public class EnemyWaveManager : MonoBehaviour
{
    //ở đây,khi xây lửa trại thì xẽ hiện 2 cái cổng trái phải
    //tạo hàm làm việc này 
    /// <summary>
    /// script này thực hiện việc tính toán số lượng enemy theo ngày ,sinh ra cổng khi lưa trại được thắp sáng
    /// </summary>
    public static EnemyWaveManager Instance { get; private set; }
  
    public int growthRate = 2;//tỉ lệ tăng số lượng quái 2%
    private int currentWavePower;
    
    public GameObject portalSummond;//cánh cổng nguyên mẫu -spawm ra cái này
    public GameObject portalL;//cánh cổng bên trái vương quốc
    public GameObject portalR;//cánh cổng bên phải vương quốc

    public List<GameObject> portalLEnemyList;
    public List<GameObject> portalREnemyList;
    
    // Lưu danh sách enemy được chọn cho wave hiện tại
    public List<EnemyWaveData> currentWaveEnemies = new List<EnemyWaveData>();
    private void Awake()
    {
        AddSingleton();
    }

    public int EnemyRemaining()
    {
        return portalLEnemyList.Count + portalREnemyList.Count;
    }
    private void AddSingleton()
    {
        if (Instance == null)
        {
            Instance = this;
          
        }
        else
        {
            Destroy(gameObject);
        }
    }
    private void Start()
    {
        //GenerateWave();
    }
    //phải hoàn thành hàm build map mới có thể làm tiếp 
    
   //pỏtal thêm id LR
    //
    //tạo 1 danh sách lưu gameobejct cổng
    //tạo hàm thêm cổng ở 2 bên vương quốc
            //thêm player không thể đi xuyên qua cổng 
    //thêm hàm xóa cổng ở list và kiểm tra ,nếu cổng trái bị xóa thì thêm 1 cổng bên trái vương quốc cách cổng hiện tại xa hơn 1 tí
    //sau này thêm hàm gen map thì gen tiếp cái map
    
   

    [ContextMenu("Test Generate Wave")]
    public void GenerateWave()
    {
        // Lấy số ngày đã trôi qua từ GameTimeManager
        int daysElapsed = GameTimeManager.Instance.GetElapsedDays();
        // Tính toán power cho wave hiện tại
        currentWavePower = CalculateWavePower(daysElapsed);
        
        Debug.Log($"Generating Wave for Day: {daysElapsed}, Power: {currentWavePower}");

        // Tạo danh sách enemy cho wave từ TotalEnemyList
        // Lưu ý: SelectMonstersByPower trả về List<(EnemyDataBase, int)>
        var selectedMonsters = TotalEnemyList.Instance.SelectMonstersByPower(currentWavePower);
        
        // Xóa danh sách cũ trước khi thêm mới
        currentWaveEnemies.Clear();

        // Chuyển đổi sang EnemyWaveData để hiển thị trên Inspector
        foreach (var monster in selectedMonsters)
        {
            EnemyWaveData waveData = new EnemyWaveData
            {
                enemyData = monster.Data,
                count = monster.Quantity
            };
            currentWaveEnemies.Add(waveData);
            Debug.Log($"Wave Enemy: {monster.Data.enemyName}, Count: {monster.Quantity}");
        }

        // Hiện tại chỉ debug danh sách, chưa spawn
        // truyền cái dòng trên vào các pỏtal để nó nhận lên spawn (COMMENTED OUT FOR NOW)
    }

    private int CalculateWavePower(int daysElapsed)
    {
        // Công thức: power = basePower × (1 + growthRate/100)^daysElapsed
        return (int)(100 * Math.Pow(1 + (growthRate / 100f), daysElapsed)-100);
    }
    // Hàm hiển thị danh sách quái vật đã chọn
   
   

    
}

// Lớp lưu trữ thông tin enemy trong wave
[System.Serializable]
public class EnemyWaveData
{
    public TotalEnemyList.EnemyDataBase enemyData;
    public int count;
}

}
