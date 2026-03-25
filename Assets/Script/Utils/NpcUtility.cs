using Script.Data_Model;
using Script.Data_Model.Status;
using UnityEngine;
using Script.Game.Mobs.Subjects;
using Script.Game.Mobs.Denizen.Core;
using Script.Game.Mobs.Denizen.Jobs.Villager;

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
                var core = go.GetComponent<DenizenCore>() ?? go.AddComponent<DenizenCore>();
                core.ChangeRole(role);
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
                        var core = denizen.GetComponent<DenizenCore>() ?? denizen.AddComponent<DenizenCore>();
                        core.ChangeRole(NPCRole.Villager);

                        DenizenStatus villagerStatus = denizen.GetComponent<DenizenStatus>();
                        if (villagerStatus != null)
                        {
                            villagerStatus.CurrentCoins = currentCoins;
                        }
                    }
                }
                else
                {
                    Debug.LogError("[Utility] Spawner instance not found! Cannot spawn using Spawner, using Instantiate fallback.");
                    GameObject denizenInstance = GameObject.Instantiate(PrefabData.Instance.denizen, archerObject.transform.position, Quaternion.identity);
                    var core = denizenInstance.GetComponent<DenizenCore>() ?? denizenInstance.AddComponent<DenizenCore>();
                    core.ChangeRole(NPCRole.Villager);
                    
                    DenizenStatus villagerStatus = denizenInstance.GetComponent<DenizenStatus>();
                    if (villagerStatus != null)
                    {
                        villagerStatus.CurrentCoins = currentCoins;
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


