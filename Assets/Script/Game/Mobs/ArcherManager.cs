using System.Collections.Generic;
using UnityEngine;

namespace Script.Game.Mobs
{
    public class ArcherManager : MonoBehaviour
    {
        public static ArcherManager Instance { get; private set; }

        private List<GameObject> archers = new List<GameObject>();

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

        public void RegisterArcher(GameObject archer)
        {
            if (archer != null && !archers.Contains(archer))
            {
                archers.Add(archer);
                Debug.Log($"Archer {archer.name} registered.");
            }
        }

        public void UnregisterArcher(GameObject archer)
        {
            if (archer != null && archers.Contains(archer))
            {
                archers.Remove(archer);
                Debug.Log($"Archer {archer.name} unregistered.");
            }
        }

        // Bạn có thể thêm các thuộc tính và phương thức sau
        
        //thêm hàm tạo báo động,chia acher sang 2 bên
    }
} 