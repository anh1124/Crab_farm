// SummonPortal.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Script.Game.Enemy;
using Script.Manager.GamePlayManager;

public class SummondPortal : MonoBehaviour
{
    public float spawnInterval = 2f; // Thời gian giữa mỗi lần spawn
    private List<EnemyWaveData> enemyWaveData;
   
    //mỗi enemy trong list này sẽ thêm 1 cái ví dụ L1,R1 để xác định nó ở cổng trái hay cổng phải và ở vị trí nào
    //khi nó chết tì truyền vào lại hàm enemyOndie để xóa cái id đấy trong listnayf đi 
    
    
    private void Start()
    {
        // Lấy danh sách enemy từ EnemyWaveManager
        enemyWaveData = EnemyWaveManager.Instance.currentWaveEnemies;
    }
    public void StartSpawning()
    {
            StartCoroutine(SpawnEnemies());
    }
    private IEnumerator SpawnEnemies()
    {
        foreach (var enemyData in enemyWaveData)
        {
            for (int i = 0; i < enemyData.count; i++)
            {
                SpawnEnemy(enemyData.enemyData);
                yield return new WaitForSeconds(spawnInterval);
            }
        }
        
       
    }
    private void SpawnEnemy(TotalEnemyList.EnemyDataBase enemyData)
    {
        // Spawn enemy
        GameObject enemy = Instantiate(enemyData.enemyPrefab, gameObject.transform.position, Quaternion.identity);
    }
}