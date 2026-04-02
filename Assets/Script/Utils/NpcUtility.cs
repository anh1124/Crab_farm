using Script.Data_Model;
using Script.Data_Model.Status;
using UnityEngine;
using Script.Game.Mobs.Subjects;
using Script.Game.Mobs.Denizen.Core;

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

            GameObject go;
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
                var roleCtrl = go.GetComponent<DenizenRoleController>();
                if (roleCtrl != null)
                {
                    roleCtrl.ChangeRole(role);
                }
                else
                {
                    Debug.LogWarning("[NpcUtility] DenizenRoleController not found on spawned denizen!");
                }
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

                Vector3 position = archerObject.transform.position;
                Quaternion rotation = archerObject.transform.rotation;
                Vector3 scale = archerObject.transform.localScale;

                GameObject denizen = null;
                if (Spawner.Instance != null)
                {
                    denizen = Spawner.Instance.SpawnMob(position, PrefabData.Instance.denizen);
                }
                else
                {
                    denizen = GameObject.Instantiate(PrefabData.Instance.denizen, position, Quaternion.identity);
                }

                if (denizen != null)
                {
                    denizen.transform.rotation = rotation;
                    denizen.transform.localScale = scale;
                    
                    var roleCtrl = denizen.GetComponent<DenizenRoleController>();
                    if (roleCtrl != null)
                    {
                        roleCtrl.ChangeRole(NPCRole.Villager);
                    }

                    DenizenStatus villagerStatus = denizen.GetComponent<DenizenStatus>();
                    if (villagerStatus != null)
                    {
                        villagerStatus.CurrentCoins = currentCoins;
                    }
                }

                GameObject.Destroy(archerObject);
            }
        }
    }
}
