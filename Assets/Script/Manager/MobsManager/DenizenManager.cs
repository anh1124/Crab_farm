using System.Collections.Generic;
using UnityEngine;
using Script.Game.Mobs.Subjects; // DenizenController, NPCRole

namespace Script.Game.Mobs.MobsManager
{
    public class DenizenManager : MonoBehaviour
    {
        public static DenizenManager Instance { get; private set; }

        // Danh sách tổng tất cả Denizen (Vagrant, Villager, Archer...)
        private readonly List<GameObject> allDenizens = new List<GameObject>();

        private void Awake()
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

        // API chung để đăng ký 1 Denizen
        public void RegisterDenizen(GameObject denizen)
        {
            if (denizen == null || allDenizens.Contains(denizen)) return;

            allDenizens.Add(denizen);

            var controller = denizen.GetComponent<DenizenController>();
            if (controller != null)
            {
                switch (controller.CurrentRole)
                {
                    case NPCRole.Vagrant:
                        // Hiện chưa có sub-manager riêng cho Vagrant
                        break;
                    case NPCRole.Villager:
                        if (VillagerManager.Instance != null)
                        {
                            VillagerManager.Instance.OnRegister(denizen);
                        }
                        break;
                    // Tránh tham chiếu ArcherManager nếu chưa tồn tại để không lỗi compile
                }
                
            }

            // Debug.Log($"[DenizenManager] Registered {denizen.name} as {controller?.CurrentRole}");
        }

        // API chung để hủy đăng ký 1 Denizen
        public void UnregisterDenizen(GameObject denizen)
        {
            if (denizen == null) return;
            allDenizens.Remove(denizen);

            var controller = denizen.GetComponent<DenizenController>();
            if (controller != null)
            {
                switch (controller.CurrentRole)
                {
                    case NPCRole.Vagrant:
                        break;
                    case NPCRole.Villager:
                        if (VillagerManager.Instance != null)
                        {
                            VillagerManager.Instance.OnUnregister(denizen);
                        }
                        break;
                }
            }

            // Debug.Log($"[DenizenManager] Unregistered {denizen.name}");
        }


        // Helpers
        public int GetTotalDenizens() => allDenizens.Count;

        public List<GameObject> GetDenizensByRole(NPCRole role)
        {
            var filtered = new List<GameObject>();
            foreach (var denizen in allDenizens)
            {
                if (denizen == null) continue;
                var controller = denizen.GetComponent<DenizenController>();
                if (controller != null && controller.CurrentRole == role)
                {
                    filtered.Add(denizen);
                }
            }
            return filtered;
        }

        // Placeholders for future integration
        public void SaveAllDenizens() { /* integrate with save system if needed */ }
        public void LoadAllDenizens() { /* integrate with save system if needed */ }
        public void UpdateAllDenizens() { /* broadcast events if needed */ }
    }
}
