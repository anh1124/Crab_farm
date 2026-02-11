using Script.Data_Model;
using Script.Data_Model.Status;
using UnityEngine;
using Script.Game.Mobs.Subjects;

namespace Script.Utils
{
    public static class NpcUtility
    {
        public static GameObject SpawnDenizen(Vector3 position, NPCRole role)
        {
            if (PrefabData.Instance == null || PrefabData.Instance.denizen == null)
            {
                Debug.LogError("[NpcUtility] denizen not found in PrefabData!");
                return null;
            }

            GameObject go = null;
            if (Spawner.Instance != null)
            {
                go = Spawner.Instance.SpawnMob(position, PrefabData.Instance.denizen);
            }
            else
            {
                go = Object.Instantiate(PrefabData.Instance.denizen, position, Quaternion.identity);
            }

            if (go != null)
            {
                var controller = go.GetComponent<DenizenRoleController>() ?? go.AddComponent<DenizenRoleController>();
                controller.ChangeRole(role);
            }

            return go;
        }

        public static void TransformToVillager(GameObject archerObject, int currentCoins)
        {
            if (archerObject == null || !archerObject.activeInHierarchy) return;

            if (PrefabData.Instance != null && PrefabData.Instance.denizen != null)
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
                    GameObject denizen = Spawner.Instance.SpawnMob(position, PrefabData.Instance.denizen);
                    if (denizen != null)
                    {
                        denizen.transform.rotation = rotation;
                        denizen.transform.localScale = scale;
                        var controller = denizen.GetComponent<DenizenRoleController>() ?? denizen.AddComponent<DenizenRoleController>();
                        controller.ChangeRole(NPCRole.Villager);

                        VillagerBehavior villagerBehavior = denizen.GetComponent<VillagerBehavior>();
                        if (villagerBehavior != null)
                        {
                            DenizenStatus villagerStatus = denizen.GetComponent<DenizenStatus>();
                            if (villagerStatus != null)
                            {
                                villagerStatus.CurrentCoins = currentCoins;
                            }
                            else
                            {
                                Debug.LogWarning("[Utility] Villager does not have DenizenStatus component.");
                            }
                        }
                    }
                }
                else
                {
                    Debug.LogError("[Utility] Spawner instance not found! Cannot spawn using Spawner, using Instantiate fallback.");
                    GameObject denizenInstance = GameObject.Instantiate(PrefabData.Instance.denizen, archerObject.transform.position, Quaternion.identity);
                    var controller = denizenInstance.GetComponent<DenizenRoleController>() ?? denizenInstance.AddComponent<DenizenRoleController>();
                    controller.ChangeRole(NPCRole.Villager);
                    VillagerBehavior villagerBehavior = denizenInstance.GetComponent<VillagerBehavior>();
                    if (villagerBehavior != null)
                    {
                        DenizenStatus villagerStatus = denizenInstance.GetComponent<DenizenStatus>();
                        if (villagerStatus != null)
                        {
                            villagerStatus.CurrentCoins = currentCoins;
                        }
                        else
                        {
                            Debug.LogWarning("[Utility] Villager does not have DenizenStatus component.");
                        }
                    }
                }

                GameObject.Destroy(archerObject);
            }
            else
            {
                Debug.LogError("[Utility] denizen not found in PrefabData! Cannot transform Archer to Villager.");
            }
        }
    }
}


