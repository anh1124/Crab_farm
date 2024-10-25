using System;
using System.Collections;
using System.Collections.Generic;
using Script.Game.Enemy;
using Script.GUI_Controller.PlayerGui;
using UnityEngine;

public class EnemyWaveSystem : MonoBehaviour
{
    public static EnemyWaveSystem Instance { get; private set; }
  
    public int growthRate = 2;
    private int currentWavePower;
    
    // Lưu danh sách enemy được chọn cho wave hiện tại
    public List<EnemyWaveData> currentWaveEnemies = new List<EnemyWaveData>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        GenerateWave();
        PlayerGui.Instance.OnSkill3PressedACT += OnSkill3ButtonPressed;
    }
   
    private void OnSkill3ButtonPressed()
    {
        GenerateWave();
    }

    public void GenerateWave()
    {
        // Lấy số ngày đã trôi qua từ GameTimeManager
        int daysElapsed = GameTimeManager.Instance.GetTotalDaysElapsed();
        // Tính toán power cho wave hiện tại
        currentWavePower = CalculateWavePower(daysElapsed);
        
        // Tạo danh sách enemy cho wave
        EnemyData.Instance.SelectMonstersByPower(currentWavePower);
        //truyền cái dòng trên vào các pỏtal để nó nhận lên spawn
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
    public EnemyData.EnemyDataBase enemyData;
    public int count;
}
