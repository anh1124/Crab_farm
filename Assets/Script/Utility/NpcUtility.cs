using UnityEngine;
using Script.Data_Model;
using Script.Game.Mobs.MobsManager;
using Script.Data_Model.Status;
using Script.Game.Mobs.Subjects;

namespace Script.Utils
{
    public static class NpcUtility
    {
        public static void TransformToVillager(GameObject archerObject, int currentCoins)
        {
            if (archerObject == null || !archerObject.activeInHierarchy) return;

            if (PrefabData.Instance != null && PrefabData.Instance.village != null)
            {
                if (PrefabData.Instance.bow != null)
                {
                    GameObject.Instantiate(PrefabData.Instance.bow, archerObject.transform.position, Quaternion.identity);
                }
                else
                {
                    Debug.LogWarning("Bow prefabWall is missing in PrefabData. Cannot drop bow.");
                }

                Vector3 position = archerObject.transform.position;
                Quaternion rotation = archerObject.transform.rotation;
                Vector3 scale = archerObject.transform.localScale;

                if (Spawner.Instance != null)
                {
                    GameObject villager = Spawner.Instance.SpawnMob(position, PrefabData.Instance.village);
                    if (villager != null)
                    {
                        villager.transform.rotation = rotation;
                        villager.transform.localScale = scale;
                        VillagerBehavior villagerBehavior = villager.GetComponent<VillagerBehavior>();
                        if (villagerBehavior != null)
                        {
                            VillagerStatus villagerStatus = villager.GetComponent<VillagerStatus>();
                            if (villagerStatus != null)
                            {
                                villagerStatus.CurrentCoins = currentCoins;
                            }
                            else
                            {
                                Debug.LogWarning("[Utility] Villager does not have VillagerStatus component.");
                            }
                        }
                    }
                }
                else
                {
                    Debug.LogError("[Utility] Spawner instance not found! Cannot spawn Villager.");
                    GameObject villagerInstance = GameObject.Instantiate(PrefabData.Instance.village, archerObject.transform.position, Quaternion.identity);
                    VillagerBehavior villagerBehavior = villagerInstance.GetComponent<VillagerBehavior>();
                    if (villagerBehavior != null)
                    {
                        VillagerStatus villagerStatus = villagerInstance.GetComponent<VillagerStatus>();
                        if (villagerStatus != null)
                        {
                            villagerStatus.CurrentCoins = currentCoins;
                        }
                        else
                        {
                            Debug.LogWarning("[Utility] Villager does not have VillagerStatus component.");
                        }
                    }
                }

                GameObject.Destroy(archerObject);
            }
            else
            {
                Debug.LogError("[Utility] Villager prefabWall not found in PrefabData! Cannot transform Archer to Villager.");
            }
        }
    }
}


