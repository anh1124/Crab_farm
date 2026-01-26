using UnityEngine;
using Script.Game.Other;
using Script.Data_Model.Interfaces;
using Script.Game.Mobs.MobsManager;
using Script.Data_Model.Status;

namespace Script.Game.Mobs.Subjects
{
    // NPCRole is intentionally kept here per your decision.
    public enum NPCRole
    {
        Vagrant,
        Villager,
        Archer,
        Worker,
        Farmer
    }

    public class DenizenController : MonoBehaviour, ISaveable
    {
        public NPCRole CurrentRole = NPCRole.Villager;//khởi đầu là dân làng 

        private Animator animator;
        private RayCastHelper rayCastHelper;

        private MonoBehaviour currentBehavior;
        private MonoBehaviour currentStatus;

        [Header("Animator Controllers")]
        [SerializeField] private RuntimeAnimatorController vagrantController;
        [SerializeField] private RuntimeAnimatorController villagerController;
        [SerializeField] private RuntimeAnimatorController archerController;
        [SerializeField] private RuntimeAnimatorController workerController;
        [SerializeField] private RuntimeAnimatorController farmerController;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            rayCastHelper = GetComponent<RayCastHelper>();
          
        }

        private void Start()
        {
            // Validate Animator Controllers
            if (vagrantController == null) Debug.LogError($"[DenizenController] Vagrant Animator Controller is missing on {gameObject.name}!");
            if (villagerController == null) Debug.LogError($"[DenizenController] Villager Animator Controller is missing on {gameObject.name}!");
            if (archerController == null) Debug.LogError($"[DenizenController] Archer Animator Controller is missing on {gameObject.name}!");
            // Worker and Farmer checks are optional for now as they are not implemented, 
            // but good to check if you plan to use them soon.
            // if (workerController == null) Debug.LogError($"[DenizenController] Worker Animator Controller is missing on {gameObject.name}!");
            // if (farmerController == null) Debug.LogError($"[DenizenController] Farmer Animator Controller is missing on {gameObject.name}!");

            ApplyRole(CurrentRole, null);
            RegisterWithManager();
        }

        private void Update()
        {
            if (currentBehavior is INPCBehavior behavior)
            {
                behavior.UpdateBehavior();
            }
        }

        public void ChangeRole(NPCRole newRole)
        {
            if (newRole == CurrentRole) return;

            UnregisterFromManager();

            var previousStatus = currentStatus; // keep reference for transfer

            if (currentBehavior != null) Destroy(currentBehavior);
            if (currentStatus != null) Destroy(currentStatus);

            ApplyRole(newRole, previousStatus);
            RegisterWithManager();
            CurrentRole = newRole;
        }

        private void ApplyRole(NPCRole role, MonoBehaviour previousStatus)
        {
            RuntimeAnimatorController targetController = null;
            switch (role)
            {
                case NPCRole.Vagrant:
                    gameObject.tag = "Vagrant";
                    currentStatus = gameObject.GetComponent<VagrantStatus>() ?? gameObject.AddComponent<VagrantStatus>();
                    currentBehavior = gameObject.GetComponent<VagrantBehavior>() ?? gameObject.AddComponent<VagrantBehavior>();
                    targetController = vagrantController;
                    break;
                case NPCRole.Villager:
                    gameObject.tag = "Villager";
                    currentStatus = gameObject.GetComponent<VillagerStatus>() ?? gameObject.AddComponent<VillagerStatus>();
                    currentBehavior = gameObject.GetComponent<VillagerBehavior>() ?? gameObject.AddComponent<VillagerBehavior>();
                    targetController = villagerController;
                    break;
                case NPCRole.Archer:
                    gameObject.tag = "Archer";
                    currentStatus = gameObject.GetComponent<ArcherStatus>() ?? gameObject.AddComponent<ArcherStatus>();
                    currentBehavior = gameObject.GetComponent<ArcherBehavior>() ?? gameObject.AddComponent<ArcherBehavior>();
                    targetController = archerController;
                    break;

                case NPCRole.Worker:
                    // TODO: Implement WorkerStatus and WorkerBehavior classes
                    Debug.LogWarning($"[DenizenController] Worker role not implemented yet! Staying as {CurrentRole}");
                    return; // Don't change role
                    /*
                    gameObject.tag = "Worker";
                    currentStatus = gameObject.GetComponent<WorkerStatus>() ?? gameObject.AddComponent<WorkerStatus>();
                    currentBehavior = gameObject.GetComponent<WorkerBehavior>() ?? gameObject.AddComponent<WorkerBehavior>();
                    targetController = workerController;
                    break;
                    */

                case NPCRole.Farmer:
                    // TODO: Implement FarmerStatus and FarmerBehavior classes
                    Debug.LogWarning($"[DenizenController] Farmer role not implemented yet! Staying as {CurrentRole}");
                    return; // Don't change role
                    /*
                    gameObject.tag = "Farmer";
                    currentStatus = gameObject.GetComponent<FarmerStatus>() ?? gameObject.AddComponent<FarmerStatus>();
                    currentBehavior = gameObject.GetComponent<FarmerBehavior>() ?? gameObject.AddComponent<FarmerBehavior>();
                    targetController = farmerController;
                    break;
                    */
            }

            if (animator != null && targetController != null)
            {
                animator.runtimeAnimatorController = targetController;
            }

            // Ensure the object is placed under the correct container based on its tag
            Script.Manager.EntitySaveManager.Instance?.MoveObjectToContainer(gameObject);

            TransferData(previousStatus, currentStatus);
        }

        private void TransferData(MonoBehaviour fromStatus, MonoBehaviour toStatus)
        {
            if (fromStatus is INPCStatus from && toStatus is INPCStatus to)
            {
                to.CurrentCoins = from.CurrentCoins;
            }
        }

        private void RegisterWithManager()
        {
            DenizenManager.Instance?.RegisterDenizen(gameObject);
        }

        private void UnregisterFromManager()
        {
            DenizenManager.Instance?.UnregisterDenizen(gameObject);
        }

        // ISaveable implementation
        // ISaveable implementation
        [System.Serializable]
        public class DenizenData
        {
            public float positionX;
            public float positionY;
            public float scaleX;
            public string role;
            public int coins;
        }

        public string GetPrefabID()
        {
            return "denizen";
        }

        public object CaptureData()
        {
            int coins = 0;
            if (currentStatus is INPCStatus s)
            {
                coins = s.CurrentCoins;
            }

            return new DenizenData
            {
                positionX = transform.position.x,
                positionY = transform.position.y,
                scaleX = transform.localScale.x,
                role = CurrentRole.ToString(),
                coins = coins
            };
        }

        public void RestoreData(object data)
        {
            var d = data as DenizenData;
            if (d == null) return;

            // Debug log to trace restoration
            //Debug.Log($"[DenizenController] Restoring ID {GetPrefabID()}. Role: {d.role}, Coins: {d.coins}");

            transform.position = new Vector3(d.positionX, d.positionY, 0);
            transform.localScale = new Vector3(d.scaleX, 1, 1);

            if (System.Enum.TryParse<NPCRole>(d.role, out var savedRole))
            {
                // Fix: Ensure we Initialize/ApplyRole even if the role matches the default CurrentRole.
                // This is crucial because RestoreData runs before Start(), so components might not be added yet.
                if (savedRole != CurrentRole)
                {
                  //  Debug.Log($"[DenizenController] Role mismatch ({CurrentRole} -> {savedRole}). Changing role.");
                    ChangeRole(savedRole);
                }
                else if (currentStatus == null)
                {
                    // Same role, but components not initialized yet (Start hasn't run)
                   // Debug.Log($"[DenizenController] Role matches default ({savedRole}) but components not ready. Forcing ApplyRole.");
                    ApplyRole(savedRole, null);
                }
            }
            else
            {
                Debug.LogWarning($"[DenizenController] Could not parse role: {d.role}");
            }

            // Ensure coins applied after role is loaded and components are ready
            if (currentStatus is INPCStatus s)
            {
                s.CurrentCoins = d.coins;
              //  Debug.Log($"[DenizenController] Coins restored set to: {s.CurrentCoins}");
            }
            else
            {
               Debug.LogError($"[DenizenController] Failed to restore coins! currentStatus is null or invalid.");
            }

            // Ensure container is updated after restore
            Script.Manager.EntitySaveManager.Instance?.MoveObjectToContainer(gameObject);
        }
    }
}
