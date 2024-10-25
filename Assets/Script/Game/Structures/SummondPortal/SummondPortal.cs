// SummonPortal.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Script.Game.Enemy;

public class SummondPortal : MonoBehaviour
{
    public float spawnInterval = 2f; // Thời gian giữa mỗi lần spawn
    private List<EnemyWaveData> enemyWaveData;
   
    private void Start()
    {
        // Lấy danh sách enemy từ EnemyWaveSystem
        enemyWaveData = EnemyWaveSystem.Instance.currentWaveEnemies;
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
    private void SpawnEnemy(EnemyData.EnemyDataBase enemyData)
    {
        // Spawn enemy
        GameObject enemy = Instantiate(enemyData.enemyPrefab, gameObject.transform.position, Quaternion.identity);
    }
}