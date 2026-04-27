using UnityEngine;

namespace Script.Utils
{
    /// <summary>
    /// Utility class for creating and managing game object containers.
    /// Used by both tutorial scenes (simple setup) and normal game (save/load system).
    /// </summary>
    public static class ContainerUtils
    {
        private const string ContainerParentName = "Container";

        /// <summary>
        /// Tạo hoặc tìm parent container GameObject.
        /// </summary>
        /// <param name="isDebug">Có log debug không</param>
        /// <returns>GameObject parent cho các container</returns>
        public static GameObject GetOrCreateContainerParent(bool isDebug = false)
        {
            GameObject containerParent = GameObject.Find(ContainerParentName);
            if (containerParent == null)
            {
                containerParent = new GameObject(ContainerParentName);
                if (isDebug)
                    Debug.Log("[ContainerUtils] Created parent Container GameObject");
            }
            return containerParent;
        }

        /// <summary>
        /// Tạo container con nếu chưa tồn tại.
        /// </summary>
        /// <param name="containerName">Tên container</param>
        /// <param name="parent">Transform parent (thường là Container parent)</param>
        /// <param name="isDebug">Có log debug không</param>
        /// <returns>GameObject container đã tạo hoặc đã tồn tại</returns>
        public static GameObject CreateContainerIfNotExists(string containerName, Transform parent, bool isDebug = false)
        {
            if (parent == null)
            {
                Debug.LogError($"[ContainerUtils] Parent is null when creating {containerName}");
                return null;
            }

            Transform existing = parent.Find(containerName);
            if (existing != null)
            {
                if (isDebug)
                    Debug.Log($"[ContainerUtils] Container already exists: {containerName}");
                return existing.gameObject;
            }

            // Check globally as fallback
            GameObject container = GameObject.Find(containerName);
            if (container != null)
            {
                // Move to correct parent
                container.transform.SetParent(parent, false);
                if (isDebug)
                    Debug.Log($"[ContainerUtils] Found and reparented container: {containerName}");
                return container;
            }

            // Create new
            container = new GameObject(containerName);
            container.transform.SetParent(parent, false);
            if (isDebug)
                Debug.Log($"[ContainerUtils] Created new container: {containerName}");
            
            return container;
        }

        /// <summary>
        /// Tạo toàn bộ container cơ bản cho game.
        /// Dùng cho cả tutorial scene và normal game.
        /// </summary>
        /// <param name="isDebug">Có log debug không</param>
        /// <returns>GameObject parent chứa tất cả container</returns>
        public static GameObject SetupAllContainers(bool isDebug = false)
        {
            GameObject parent = GetOrCreateContainerParent(isDebug);

            CreateContainerIfNotExists("PlayerContainer", parent.transform, isDebug);
            CreateContainerIfNotExists("StructureContainer", parent.transform, isDebug);
            CreateContainerIfNotExists("CoinContainer", parent.transform, isDebug);
            CreateContainerIfNotExists("NatureContainer", parent.transform, isDebug);
            CreateContainerIfNotExists("ChunkContainer", parent.transform, isDebug);
            CreateContainerIfNotExists("EnemyContainer", parent.transform, isDebug);
            CreateContainerIfNotExists("DenizenContainer", parent.transform, isDebug);
            CreateContainerIfNotExists("OtherContainer", parent.transform, isDebug);

            if (isDebug)
                Debug.Log("[ContainerUtils] All containers setup completed");

            return parent;
        }

        /// <summary>
        /// Tạo container cơ bản tối thiểu cho tutorial scene.
        /// Chỉ tạo các container thực sự cần thiết.
        /// </summary>
        /// <param name="isDebug">Có log debug không</param>
        /// <returns>GameObject parent chứa các container</returns>
        public static GameObject SetupSimpleContainers(bool isDebug = false)
        {
            GameObject parent = GetOrCreateContainerParent(isDebug);

            CreateContainerIfNotExists("PlayerContainer", parent.transform, isDebug);
            CreateContainerIfNotExists("StructureContainer", parent.transform, isDebug);
            CreateContainerIfNotExists("CoinContainer", parent.transform, isDebug);
            CreateContainerIfNotExists("NatureContainer", parent.transform, isDebug);
            CreateContainerIfNotExists("ChunkContainer", parent.transform, isDebug);
            CreateContainerIfNotExists("OtherContainer", parent.transform, isDebug);

            if (isDebug)
                Debug.Log("[ContainerUtils] Simple containers setup completed");

            return parent;
        }
    }
}
