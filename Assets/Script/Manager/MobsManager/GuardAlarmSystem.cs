using System.Collections.Generic;
using UnityEngine;
using Script.Game.Enemy;
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

        private Coroutine alertCooldownCoroutine;
        private GuardManager guardManager;

        private void Start()
        {
            guardManager = GuardManager.Instance;
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

            if (alertCooldownCoroutine != null)
            {
                StopCoroutine(alertCooldownCoroutine);
                alertCooldownCoroutine = null;
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
                if (alertCooldownCoroutine == null)
                {
                    alertCooldownCoroutine = StartCoroutine(AlertCooldownRoutine());
                }
            }
        }

        private System.Collections.IEnumerator AlertCooldownRoutine()
        {
            alarmTimer = ALARM_COOLDOWN_DURATION;
            while (alarmTimer > 0)
            {
                alarmTimer -= Time.deltaTime;
                yield return null;
            }
            alarmTimer = 0f;

            if (GameTimeManager.Instance != null && GameTimeManager.Instance.IsDaytime)
            {
                ClearAlarm();
            }
            else
            {
                ClearAlarm();
            }
            
            alertCooldownCoroutine = null;
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
                
                if (alertCooldownCoroutine == null)
                {
                    alertCooldownCoroutine = StartCoroutine(AlertCooldownRoutine());
                }
            }
        }

        private void OnDestroy()
        {
            detectedEnemies.Clear();
        }
    }
}
