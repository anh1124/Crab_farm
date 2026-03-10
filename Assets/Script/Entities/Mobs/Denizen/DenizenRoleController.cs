using UnityEngine;
using Script.Game.Other;
using Script.Data_Model.Interfaces;
using Script.Game.Mobs.MobsManager;
using Script.Data_Model.Status;
using UnityEngine.Serialization;

using Script.Game.Mobs.Subjects;
using Script.Manager.GamePlayManager;

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

    public class DenizenRoleController : MonoBehaviour, ISaveable, IItemHandle
    {
        // Script này quản lý chuyển nghề và thiết lập script cần thiết
        public NPCRole CurrentRole = NPCRole.Vagrant;

        private Animator animator;

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
        /// <summary>
        /// OnValidate is called when Inspector values change in the Unity Editor.
        /// This method handles role switching when CurrentRole is changed via Inspector.
        /// 
        /// CRITICAL BUG FIX EXPLANATION:
        /// When you change CurrentRole in the Inspector (e.g., Archer -> Vagrant), Unity updates
        /// the CurrentRole field BEFORE calling OnValidate. This creates a problem:
        /// 
        /// 1. User changes Inspector: Archer -> Vagrant
        /// 2. Unity sets CurrentRole = Vagrant (BEFORE OnValidate runs)
        /// 3. OnValidate runs and tries to call ChangeRole(Vagrant)
        /// 4. ChangeRole has this check: if (newRole == CurrentRole) return;
        /// 5. Since CurrentRole is already Vagrant, it sees Vagrant == Vagrant and exits early
        /// 6. Result: No role change logic executes! Archer stays registered, causing bugs.
        /// 
        /// THE FIX:
        /// We temporarily revert CurrentRole back to the OLD role before calling ChangeRole.
        /// This ensures ChangeRole sees oldRole != newRole and executes the full transition logic:
        /// - Unregister from old manager (e.g., GuardManager for Archer)
        /// - Apply new role behavior
        /// - Register with new manager
        /// </summary>
        private void OnValidate()
        {
           // Debug.Log($"[OnValidate] ENTRY: CurrentRole={CurrentRole}, previousRole={previousRole}");
            
            // Avoid logic loops - if no change detected, exit early
            if (CurrentRole == previousRole)
            {
               // Debug.Log($"[OnValidate] EARLY EXIT: CurrentRole == previousRole ({CurrentRole})");
                return;
            }

          //  Debug.Log($"[OnValidate] Change detected: {previousRole} -> {CurrentRole}");
            
            // CRITICAL: Update previousRole IMMEDIATELY to prevent double-trigger on next OnValidate
            NPCRole oldRole = previousRole;
            previousRole = CurrentRole;

            // Use delayCall to avoid Unity's "SendMessage cannot be called during Awake/OnValidate" error
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this == null)
                {
                   // Debug.Log("[OnValidate delayCall] Object destroyed, skipping");
                    return; // Safety check if object destroyed
                }

              //  Debug.Log($"[OnValidate delayCall] EXECUTING: CurrentRole={CurrentRole}, oldRole={oldRole}");
                
                if (Application.isPlaying)
                {
                    // THE FIX: Temporarily revert CurrentRole to oldRole before calling ChangeRole
                    // This bypasses the early exit check in ChangeRole (if newRole == CurrentRole)
                    NPCRole newRole = CurrentRole;
                    CurrentRole = oldRole; // Revert to old role temporarily
                  //  Debug.Log($"[OnValidate delayCall] Calling ChangeRole: {oldRole} -> {newRole}");
                    ChangeRole(newRole); // ChangeRole will now see oldRole != newRole and execute properly
                }
                else
                {
                   // Debug.Log($"[OnValidate delayCall] NOT in play mode, skipping ChangeRole");
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

            // Get or Add Unified Status

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


            // Setup Storage handled by DenizenStatus now

            previousRole = CurrentRole;

            // Initially disable all behaviors to prevent auto-updates if they have Update()
            // We will control them manually or via Enable/Disable
            DisableAllBehaviors();
        }

        private void OnEnable()
        {
            if (denizenStatus != null)
            {
                denizenStatus.OnCoinChanged += OnCoinChangedHandler;
            }
        }

        private void OnDisable()
        {
            if (denizenStatus != null)
            {
                denizenStatus.OnCoinChanged -= OnCoinChangedHandler;
            }
        }


        private void OnCoinChangedHandler(int newCoins)
        {
            if (!this.isActiveAndEnabled) return;

            // Damage system normally drops the tool and changes role at coin == 1 via IToolShield.
            // This is a safety fallback if coins directly drop to 0 via other means.
            if (CurrentRole == NPCRole.Archer && newCoins <= 0)
            {
                // Drop Bow and become Vagrant directly
                if (Script.Data_Model.PrefabData.Instance != null && Script.Data_Model.PrefabData.Instance.bow != null)
                {
                    Transform itemContainer = transform.parent; // Fallback
                    if (Script.Manager.EntitySaveManager.Instance != null && Script.Manager.EntitySaveManager.Instance.otherContainer != null)
                    {
                        itemContainer = Script.Manager.EntitySaveManager.Instance.otherContainer.transform;
                    }
                    Vector3 dropPos = transform.position + new Vector3(UnityEngine.Random.Range(-1f, 1f), 1.5f, 0);
                    Instantiate(Script.Data_Model.PrefabData.Instance.bow, dropPos, Quaternion.identity, itemContainer);
                }
                ChangeRole(NPCRole.Vagrant);
            }
            else if (CurrentRole != NPCRole.Vagrant && newCoins <= 0)
            {
                // Become Vagrant
                ChangeRole(NPCRole.Vagrant);
            }
        }

        private void OnDestroy()
        {
            UnregisterFromManager(CurrentRole);
        }

        private void Start()
        {
            ApplyRole(CurrentRole);
            RegisterWithManager();

            // Set Detection Ranges based on Status
            // Logic moved to specific Behaviors
        }

        // Update loop has been removed, replaced by NPCUpdateManager

        public void ChangeRole(NPCRole newRole)
        {
            if (newRole == CurrentRole) return;

            // Xử lý unregister role cũ
            // The original instruction's "Code Edit" snippet was syntactically incorrect.
            // Reverting to the original correct code for this section.
            // Debug.Log($"[DenizenRoleController] ChangeRole called: {CurrentRole} -> {newRole}");
            NPCRole oldRole = CurrentRole; // Save old role BEFORE changing

            try
            {
                UnregisterFromManager(oldRole); // Pass old role explicitly
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[DenizenRoleController] Error unregistering role {oldRole}: {e.Message}");
            }

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
            
            // Ensure previousRole is kept in sync for Inspector changes
            previousRole = newRole;
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
                    vagrantBehavior.enabled = true;
                    currentActiveBehavior = vagrantBehavior;
                    NPCUpdateManager.Instance?.Register(currentActiveBehavior);
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
                    NPCUpdateManager.Instance?.Register(currentActiveBehavior);
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
                    NPCUpdateManager.Instance?.Register(currentActiveBehavior);
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
            if (currentActiveBehavior != null)
            {
                NPCUpdateManager.Instance?.Unregister(currentActiveBehavior);
            }
            
            if (vagrantBehavior != null) vagrantBehavior.enabled = false;
            if (villagerBehavior != null) villagerBehavior.enabled = false;
            if (archerBehavior != null) archerBehavior.enabled = false;
            currentActiveBehavior = null;
        }

        private void RegisterWithManager()
        {
            DenizenManager.Instance?.RegisterDenizen(gameObject);
        }

        private void UnregisterFromManager(NPCRole oldRole)
        {
          //  Debug.Log($"[DenizenRoleController] UnregisterFromManager called for {gameObject.name}, oldRole={oldRole}");
            DenizenManager.Instance?.UnregisterDenizen(gameObject);

            // Handle specific manager unregistration based on OLD role
            if (oldRole == NPCRole.Archer)
            {
                if (GuardManager.Instance != null)
                {
                    //Debug.Log($"[DenizenRoleController] Calling GuardManager.UnregisterArcher for {gameObject.name}");
                    GuardManager.Instance.UnregisterArcher(gameObject);
                }
                else if (Application.isPlaying)
                {
                    Debug.LogWarning("[DenizenRoleController] GuardManager Instance is NULL during unregister!");
                }
            }
            else
            {
               // Debug.Log($"[DenizenRoleController] oldRole is {oldRole}, NOT calling UnregisterArcher");
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

        #region Context Debug
        
        [ContextMenu("Debug Village State")]
        public void DebugVillageState()
        {
            if (denizenStatus == null) denizenStatus = GetComponent<DenizenStatus>();
            if (denizenStatus == null)
            {
                Debug.LogWarning("[DenizenRoleController] DenizenStatus missing!");
                return;
            }

            var timeManager = Script.Manager.WorldManager.GameTimeManager.Instance;
            string timeStr = timeManager != null 
                ? $"{timeManager.GameHour:D2}:{timeManager.GameMinute:D2} (Daytime: {timeManager.IsDaytime})" 
                : "TimeManager NULL";

            string stateStr = "Unknown";
            string archerInfo = "";

            switch (CurrentRole)
            {
                case NPCRole.Vagrant:
                    stateStr = denizenStatus.vagrantState.ToString();
                    break;
                case NPCRole.Villager:
                    stateStr = denizenStatus.villagerState.ToString();
                    break;
                case NPCRole.Archer:
                    stateStr = denizenStatus.archerState.ToString();
                    
                    if (GuardManager.Instance != null)
                    {
                        bool isLeft = denizenStatus.isLeftFlank;
                        var flankList = isLeft ? GuardManager.Instance.leftFlankArchers : GuardManager.Instance.rightFlankArchers;
                        int index = flankList.IndexOf(gameObject);
                        
                        archerInfo = $"\nArcher Flank: {(isLeft ? "LEFT" : "RIGHT")}\nPosition in Line: {index}";
                    }
                    else
                    {
                        archerInfo = "\nArcher Info: GuardManager instance is NULL.";
                    }
                    break;
            }

            Debug.Log($"[{gameObject.name}] DEBUG STATE\n" +
                      $"Job: {CurrentRole}\n" +
                      $"Coin: {denizenStatus.CurrentCoins}\n" +
                      $"Job State: {stateStr}\n" +
                      $"Time: {timeStr}{archerInfo}");
        }
        
        #endregion

        #region IItemHandle Implementation
        
        public bool HandleItem(GameObject item)
        {
            // Router: Pass item directly to the active behavior via Interface casting
            if (currentActiveBehavior is IItemHandle itemHandler)
            {
                return itemHandler.HandleItem(item);
            }
            return false;
        }
        
        #endregion
    }
}
