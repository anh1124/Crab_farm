using UnityEngine;
using Script.Utils;
using System.Collections.Generic;

namespace Script.Manager
{
    /// <summary>
    /// ContainerEntityManager: Chuyên trách việc quản lý container và phân loại đối tượng vào đúng nhóm.
    /// Giúp tách biệt logic "tổ chức scene" ra khỏi logic "save/load" của EntitySaveManager.
    /// </summary>
    public class ContainerEntityManager : MonoBehaviour
    {
        public static ContainerEntityManager Instance { get; private set; }

        [Header("Containers")]
        public GameObject playerContainer;
        public GameObject structureContainer;
        public GameObject coinContainer;
        public GameObject natureContainer;
        public GameObject animalsContainer;
        public GameObject enemyContainer;
        public GameObject denizenContainer;
        public GameObject otherContainer;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                SetupContainers();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void SetupContainers()
        {
            // Tự động tìm container nếu chưa được gán trong Inspector
            if (playerContainer == null) playerContainer = GameObject.Find("PlayerContainer");
            if (structureContainer == null) structureContainer = GameObject.Find("StructureContainer");
            if (coinContainer == null) coinContainer = GameObject.Find("CoinContainer");
            if (natureContainer == null) natureContainer = GameObject.Find("NatureContainer");
            if (animalsContainer == null) animalsContainer = GameObject.Find("AnimalsContainer");
            if (enemyContainer == null) enemyContainer = GameObject.Find("EnemyContainer");
            if (denizenContainer == null) denizenContainer = GameObject.Find("DenizenContainer");
            if (otherContainer == null) otherContainer = GameObject.Find("OtherContainer");
            
            // Tạo các container nếu vẫn chưa tồn tại
            if (playerContainer == null) playerContainer = CreateContainer("PlayerContainer");
            if (structureContainer == null) structureContainer = CreateContainer("StructureContainer");
            if (coinContainer == null) coinContainer = CreateContainer("CoinContainer");
            if (natureContainer == null) natureContainer = CreateContainer("NatureContainer");
            if (animalsContainer == null) animalsContainer = CreateContainer("AnimalsContainer");
            if (enemyContainer == null) enemyContainer = CreateContainer("EnemyContainer");
            if (denizenContainer == null) denizenContainer = CreateContainer("DenizenContainer");
            if (otherContainer == null) otherContainer = CreateContainer("OtherContainer");
        }

        private GameObject CreateContainer(string name)
        {
            GameObject container = new GameObject(name);
            container.transform.parent = this.transform;
            return container;
        }

        /// <summary>
        /// Di chuyển đối tượng vào đúng container dựa trên tag của nó.
        /// </summary>
        public void MoveToContainer(GameObject obj)
        {
            if (obj == null) return;

            GameObject targetContainer = GetContainerByTag(obj.tag);
            if (targetContainer != null)
            {
                obj.transform.SetParent(targetContainer.transform);
            }
            else
            {
                obj.transform.SetParent(otherContainer.transform);
            }
        }

        /// <summary>
        /// Di chuyển tất cả con của một đối tượng vào các container tương ứng.
        /// Thường dùng sau khi spawn một Chunk để phân loại cây cối/đá.
        /// </summary>
        public void MoveChildrenToContainers(GameObject parent)
        {
            if (parent == null) return;

            List<Transform> children = new List<Transform>();
            foreach (Transform child in parent.transform)
            {
                children.Add(child);
            }

            foreach (Transform child in children)
            {
                MoveToContainer(child.gameObject);
            }
        }

        private GameObject GetContainerByTag(string tag)
        {
            switch (tag)
            {
                case GameTags.Player:
                    return playerContainer;
                case GameTags.Wall:
                case GameTags.Stump:
                case GameTags.TownCenter:
                case GameTags.Tower:
                case GameTags.Building:
                case GameTags.Statues:
                case "Statue":
                case "Portal":
                    return structureContainer;
                case GameTags.Coin:
                    return coinContainer;
                case GameTags.Tree:
                case GameTags.Rock:
                case GameTags.Bush:
                    return natureContainer;
                case "Animal":
                case "Deer":
                    return animalsContainer;
                case GameTags.Enemy:
                    return enemyContainer;
                case GameTags.Vagrant:
                case GameTags.Villager:
                case GameTags.Archer:
                case GameTags.Worker:
                case GameTags.Farmer:
                    return denizenContainer;
                default:
                    return otherContainer;
            }
        }
    }
}
