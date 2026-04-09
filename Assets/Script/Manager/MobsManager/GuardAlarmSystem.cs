using System.Collections.Generic;
using UnityEngine;
using Script.Game.Enemy;
using Script.Game.Enemy.Core;
using Script.Manager.WorldManager;

namespace Script.Game.Mobs.MobsManager
{
    public class GuardAlarmSystem : MonoBehaviour
    {
        [Header("Runtime State")]
        public bool isEnemyAttack = false;
        
        [Header("Debug")]
        public List<GameObject> detectedEnemies = new List<GameObject>();
        
        private const float ALARM_COOLDOWN_DURATION = 30f;
        
        [Header("Alarm Status")]
        public float alarmTimer = 0f;

        private GuardManager guardManager;

        private void Start()
        {
            guardManager = FindObjectOfType<GuardManager>();
        }

        public void RegisterEnemy(GameObject enemy)
        {
            if (enemy == null) return;

            var enemyStatus = enemy.GetComponent<EnemyStatus>();
            if (enemyStatus == null)
            {
                Debug.LogWarning($"[GuardAlarmSystem] Rejected enemy {enemy.name}: Missing EnemyStatus component!");
                return;
            }

            detectedEnemies.RemoveAll(x => x == null || !x.activeInHierarchy);

            if (detectedEnemies.Contains(enemy)) return;
            detectedEnemies.Add(enemy);

            var enemyController = enemy.GetComponent<EnemyAIController>();
            if (enemyController != null)
            {
                enemyController.OnDespawn += HandleEnemyDespawn;
            }

            alarmTimer = ALARM_COOLDOWN_DURATION;

            if (!isEnemyAttack)
            {
                isEnemyAttack = true;
                if (guardManager) guardManager.DebugLog("[GuardAlarmSystem] ⚔️ ALARM ACTIVATED - Mobilizing Archers!");
                if (guardManager && guardManager.Squad) guardManager.Squad.NotifyAllArchersValues(true);
            }
        }
        
        private void HandleEnemyDespawn(EnemyAIController enemyController)
        {
            if (enemyController == null) return;

            enemyController.OnDespawn -= HandleEnemyDespawn;

            if (detectedEnemies.Contains(enemyController.gameObject))
            {
                detectedEnemies.Remove(enemyController.gameObject);
            }

            detectedEnemies.RemoveAll(x => x == null || !x.activeInHierarchy);

            if (detectedEnemies.Count == 0)
            {
                // Logic đếm ngược đã được xử lý trong Update
            }
        }

        private void Update()
        {
            if (!isEnemyAttack) return;

            // Dọn dẹp danh sách kẻ địch bị null hoặc inactive định kỳ (mỗi frame hoặc giãn cách)
            detectedEnemies.RemoveAll(x => x == null || !x.activeInHierarchy);

            if (detectedEnemies.Count == 0)
            {
                if (alarmTimer > 0)
                {
                    alarmTimer -= Time.deltaTime;
                    if (alarmTimer <= 0)
                    {
                        alarmTimer = 0;
                        ClearAlarm();
                    }
                }
            }
            else
            {
                // Luôn reset timer về mức tối đa nếu vẫn còn địch trong tầm quét
                alarmTimer = ALARM_COOLDOWN_DURATION;
            }
        }

        public void ClearAlarm()
        {
            foreach (var enemy in detectedEnemies)
            {
                if (enemy == null) continue;
                var controller = enemy.GetComponent<EnemyAIController>();
                if (controller != null)
                {
                    controller.OnDespawn -= HandleEnemyDespawn;
                }
            }

            isEnemyAttack = false;
            detectedEnemies.Clear();
            alarmTimer = 0f;
            if (guardManager) guardManager.DebugLog("✅ Alarm cleared - Returning to peacetime duties");
            
            if (guardManager)
            {
                if (guardManager.Wall != null) guardManager.Wall.UpdateDefensiveWalls();
                if (guardManager.Squad != null) guardManager.Squad.RebalanceSquads();
                if (guardManager.Squad != null) guardManager.Squad.NotifyAllArchersValues(false);
            }
        }

        public void DebugClearEnemies()
        {
            if (detectedEnemies.Count > 0)
            {
                if (guardManager) guardManager.DebugLog("[GuardAlarmSystem] 🛠️ Debug Clear ALL Enemies");
                var enemies = new List<GameObject>(detectedEnemies);
                foreach (var enemy in enemies)
                {
                    if (enemy != null)
                    {
                        var behavior = enemy.GetComponent<EnemyAIController>();
                        if (behavior != null)
                        {
                             behavior.OnDespawn -= HandleEnemyDespawn;
                        }
                    }
                }
                detectedEnemies.Clear();
                // Logic đếm ngược đã được xử lý trong Update
            }
        }

        private void OnDestroy()
        {
            detectedEnemies.Clear();
        }
    }
}
