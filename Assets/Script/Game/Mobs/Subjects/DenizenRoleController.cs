using UnityEngine;
using Script.Game.Other;
using Script.Data_Model.Interfaces;
using Script.Game.Mobs.MobsManager;
using Script.Data_Model.Status;
using UnityEngine.Serialization;
using Script.Game.Mobs.Components;

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

    public class DenizenRoleController : MonoBehaviour, ISaveable
    {
        // Script này quản lý chuyển nghề và thiết lập script cần thiết
        public NPCRole CurrentRole = NPCRole.Vagrant;

        private Animator animator;
        private RayCastHelper rayCastHelper;

        // Centralized Status
        private DenizenStatus denizenStatus;

        // Behaviors
        private VagrantBehavior vagrantBehavior;
        private VillagerBehavior villagerBehavior;
        private ArcherBehavior archerBehavior;
        
        // Current active behavior for Update loop
        private INPCBehavior currentActiveBehavior;

        [HideInInspector] [SerializeField] private NPCRole previousRole = NPCRole.Vagrant;

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Avoid logic loops
            if (CurrentRole == previousRole) return;

            // Fix SendMessage error by delaying execution
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this == null) return; // Safety check if object destroyed

                if (CurrentRole != previousRole)
                {
                    if (Application.isPlaying)
                    {
                        NPCRole newRole = CurrentRole;
                        CurrentRole = previousRole; // Revert to trick ChangeRole
                        ChangeRole(newRole);
                    }
                    previousRole = CurrentRole;
                }
            };
        }
#endif

        private void Awake()
        {
            // Get helper components
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                animator = gameObject.AddComponent<Animator>();
                Debug.LogWarning($"[DenizenRoleController] Animator missing on {gameObject.name}. Auto-added.");
            }

            rayCastHelper = GetComponent<RayCastHelper>();
            if (rayCastHelper == null)
            {
                rayCastHelper = gameObject.AddComponent<RayCastHelper>();
            }

            // Get or Add Unified Status
            denizenStatus = GetComponent<DenizenStatus>();
            if (denizenStatus == null)
            {
                denizenStatus = gameObject.AddComponent<DenizenStatus>();
            }

            // Pre-cache or Add Behaviors
            // We adding them all, but will only enable the active one.
            vagrantBehavior = GetComponent<VagrantBehavior>() ?? gameObject.AddComponent<VagrantBehavior>();
            villagerBehavior = GetComponent<VillagerBehavior>() ?? gameObject.AddComponent<VillagerBehavior>();
            archerBehavior = GetComponent<ArcherBehavior>() ?? gameObject.AddComponent<ArcherBehavior>();

            // Setup CoinCollector
            if (GetComponent<CoinCollector>() == null)
            {
                gameObject.AddComponent<CoinCollector>();
            }

            previousRole = CurrentRole;

            // Initially disable all behaviors to prevent auto-updates if they have Update()
            // We will control them manually or via Enable/Disable
            DisableAllBehaviors();
        }

        private void OnDestroy()
        {
            UnregisterFromManager();
        }

        private void Start()
        {
            ApplyRole(CurrentRole);
            RegisterWithManager();

            // Set RayCast distance based on Status
            if (rayCastHelper != null && denizenStatus != null)
            {
                rayCastHelper.SetRayLength(denizenStatus.detectRange);
            }
        }

        private void Update()
        {
            // Centralized Update Loop
            if (currentActiveBehavior != null)
            {
                currentActiveBehavior.UpdateBehavior();
            }
        }

        public void ChangeRole(NPCRole newRole)
        {
            if (newRole == CurrentRole) return;

            UnregisterFromManager();

            // Update Role FIRST so that ApplyRole and RegisterWithManager see the new role
            CurrentRole = newRole;
            if (denizenStatus != null)
            {
                denizenStatus.Role = newRole;
            }

            // Then Apply Behavior (Enable script, Animator, etc.)
            ApplyRole(newRole);

            // Finally Register (DenizenManager will see new role via CurrentRole)
            RegisterWithManager();
        }

        private void ApplyRole(NPCRole role)
        {
            if (DenizenManager.Instance == null)
            {
                Debug.LogError("[DenizenRoleController] DenizenManager Instance is null! Cannot ApplyRole.");
                return;
            }

            RuntimeAnimatorController targetController = null;
            DisableAllBehaviors();

            switch (role)
            {
                case NPCRole.Vagrant:
                    if (DenizenManager.Instance.vagrantAnimatorController == null)
                    {
                        Debug.LogError("[DenizenRoleController] Missing VagrantAnimatorController in DenizenManager!");
                        return;
                    }
                    gameObject.tag = "Vagrant";
                    vagrantBehavior.enabled = true; // Use simple enabled toggle if behaviors use OnEnable/OnDisable
                    currentActiveBehavior = vagrantBehavior;
                    targetController = DenizenManager.Instance.vagrantAnimatorController;
                    break;

                case NPCRole.Villager:
                    if (DenizenManager.Instance.villagerAnimatorController == null)
                    {
                        Debug.LogError("[DenizenRoleController] Missing VillagerAnimatorController in DenizenManager!");
                        return;
                    }
                    gameObject.tag = "Villager";
                    villagerBehavior.enabled = true;
                    currentActiveBehavior = villagerBehavior;
                    targetController = DenizenManager.Instance.villagerAnimatorController;
                    break;

                case NPCRole.Archer:
                    if (DenizenManager.Instance.archerAnimatorController == null)
                    {
                        Debug.LogError("[DenizenRoleController] Missing ArcherAnimatorController in DenizenManager!");
                        return;
                    }
                    gameObject.tag = "Archer";
                    archerBehavior.enabled = true;
                    currentActiveBehavior = archerBehavior;
                    targetController = DenizenManager.Instance.archerAnimatorController;
                    
                    // Register to GuardManager centrally
                    GuardManager.Instance?.RegisterArcher(gameObject);
                    break;

                case NPCRole.Worker:
                    // TODO: Implement Worker
                    Debug.LogWarning($"[DenizenRoleController] Worker role not implemented yet! Staying as {CurrentRole}");
                    return; 

                case NPCRole.Farmer:
                    // TODO: Implement Farmer
                    Debug.LogWarning($"[DenizenRoleController] Farmer role not implemented yet! Staying as {CurrentRole}");
                    return; 
            }

            if (animator != null && targetController != null)
            {
                animator.runtimeAnimatorController = targetController;
            }

            // Ensure the object is placed under the correct container based on its tag
            Script.Manager.EntitySaveManager.Instance?.MoveObjectToContainer(gameObject);
        }

        private void DisableAllBehaviors()
        {
            if (vagrantBehavior != null) vagrantBehavior.enabled = false;
            if (villagerBehavior != null) villagerBehavior.enabled = false;
            if (archerBehavior != null) archerBehavior.enabled = false;
            currentActiveBehavior = null;
        }

        private void RegisterWithManager()
        {
            DenizenManager.Instance?.RegisterDenizen(gameObject);
        }

        private void UnregisterFromManager()
        {
            DenizenManager.Instance?.UnregisterDenizen(gameObject);

            // Handle specific manager unregistration based on role
            if (CurrentRole == NPCRole.Archer)
            {
                GuardManager.Instance?.UnregisterArcher(gameObject);
            }
        }

        // ISaveable implementation
        public string GetPrefabID()
        {
            return "denizen";
        }

        public object CaptureData()
        {
            if (denizenStatus != null)
            {
                return denizenStatus.CaptureData(transform);
            }
            return null;
        }

        public void RestoreData(object data)
        {
            var d = data as DenizenStatus.DenizenSaveData;
            if (d == null || denizenStatus == null) return;

            denizenStatus.RestoreData(d, transform);

            NPCRole savedRole = denizenStatus.Role;
            if (savedRole != CurrentRole)
            {
                ChangeRole(savedRole);
            }
            else
            {
                // Refresh components even if role is same (e.g. initial load)
                ApplyRole(savedRole);
            }
        }
    }
}
