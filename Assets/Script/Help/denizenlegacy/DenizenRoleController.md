```csharp
using UnityEngine;
using Script.Game.Other;
using Script.Data_Model.Interfaces;
using Script.Game.Mobs.MobsManager;
using Script.Data_Model.Status;
using UnityEngine.Serialization;

using Script.Game.Mobs.Subjects;
using Script.Manager.GamePlayManager;
using Script.Data_Model;

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

    [RequireComponent(typeof(DenizenTimeHandler))]
    public class DenizenRoleController : MonoBehaviour, ISaveable, IItemHandle, INPCBehavior
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
        private void OnValidate()
        {
            // Avoid logic loops - if no change detected, exit early
            if (CurrentRole == previousRole) return;

            NPCRole oldRole = previousRole;
            previousRole = CurrentRole;

            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this == null) return;
                if (Application.isPlaying)
                {
                    NPCRole newRole = CurrentRole;
                    CurrentRole = oldRole; // Revert to old role temporarily
                    ChangeRole(newRole); 
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

            denizenStatus = GetComponent<DenizenStatus>();
            if (denizenStatus == null)
            {
                denizenStatus = gameObject.AddComponent<DenizenStatus>();
            }

            vagrantBehavior = GetComponent<VagrantBehavior>() ?? gameObject.AddComponent<VagrantBehavior>();
            villagerBehavior = GetComponent<VillagerBehavior>() ?? gameObject.AddComponent<VillagerBehavior>();
            archerBehavior = GetComponent<ArcherBehavior>() ?? gameObject.AddComponent<ArcherBehavior>();

            previousRole = CurrentRole;
            DisableAllBehaviors();
        }

        private void OnEnable()
        {
            if (denizenStatus != null)
            {
                denizenStatus.OnCoinChanged += OnCoinChangedHandler;
            }

            if (NPCUpdateManager.Instance != null)
                NPCUpdateManager.Instance.Register(this);
        }

        private void OnDisable()
        {
            if (denizenStatus != null)
            {
                denizenStatus.OnCoinChanged -= OnCoinChangedHandler;
            }

            if (NPCUpdateManager.Instance != null)
                NPCUpdateManager.Instance.Unregister(this);
        }


        private void OnCoinChangedHandler(int newCoins)
        {
            if (!this.isActiveAndEnabled) return;

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

            if (NPCUpdateManager.Instance != null)
                NPCUpdateManager.Instance.Register(this);
        }

        #region Time Event Routing
        public void OnDayStart()
        {
            if (CurrentRole == NPCRole.Villager && villagerBehavior != null)
            {
                villagerBehavior.OnDayTime();
            }
        }

        public void OnArcherHuntingTime()
        {
            if (CurrentRole == NPCRole.Archer && archerBehavior != null)
            {
                archerBehavior.OnDayBreaks();
            }
        }

        public void OnNightStart()
        {
            if (CurrentRole == NPCRole.Villager && villagerBehavior != null)
            {
                villagerBehavior.OnNightTime();
            }
            else if (CurrentRole == NPCRole.Archer && archerBehavior != null)
            {
                archerBehavior.OnNightFalls();
            }
        }
        #endregion

        public void UpdateBehavior()
        {
            currentActiveBehavior?.UpdateBehavior();
        }

        public void OnObjectsDetected(System.Collections.Generic.List<GameObject> detectedObjects)
        {
            currentActiveBehavior?.OnObjectsDetected(detectedObjects);
        }

        public void ChangeRole(NPCRole newRole)
        {
            if (newRole == CurrentRole) return;

            NPCRole oldRole = CurrentRole; 

            try
            {
                UnregisterFromManager(oldRole); 
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[DenizenRoleController] Error unregistering role {oldRole}: {e.Message}");
            }

            CurrentRole = newRole;
            if (denizenStatus != null)
            {
                denizenStatus.Role = newRole;
            }

            ApplyRole(newRole);
            RegisterWithManager();
            previousRole = newRole;
        }


        private void ApplyRole(NPCRole role)
        {
            if (DenizenManager.Instance == null) return;

            RuntimeAnimatorController targetController = null;
            DisableAllBehaviors();

            switch (role)
            {
                case NPCRole.Vagrant:
                    gameObject.tag = Script.Utils.GameTags.Vagrant;
                    vagrantBehavior.enabled = true;
                    currentActiveBehavior = vagrantBehavior;
                    targetController = DenizenManager.Instance.vagrantAnimatorController;
                    break;

                case NPCRole.Villager:
                    gameObject.tag = Script.Utils.GameTags.Villager;
                    villagerBehavior.enabled = true;
                    currentActiveBehavior = villagerBehavior;
                    targetController = DenizenManager.Instance.villagerAnimatorController;
                    break;

                case NPCRole.Archer:
                    gameObject.tag = Script.Utils.GameTags.Archer;
                    archerBehavior.enabled = true;
                    currentActiveBehavior = archerBehavior;
                    targetController = DenizenManager.Instance.archerAnimatorController;
                    GuardManager.Instance?.RegisterArcher(gameObject);
                    break;
            }

            if (animator != null && targetController != null)
            {
                animator.runtimeAnimatorController = targetController;
            }
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

        private void UnregisterFromManager(NPCRole oldRole)
        {
            DenizenManager.Instance?.UnregisterDenizen(gameObject);
            if (oldRole == NPCRole.Archer)
            {
                GuardManager.Instance?.UnregisterArcher(gameObject);
            }
        }

        public string GetPrefabID() => "denizen";

        public object CaptureData()
        {
            if (denizenStatus != null) return denizenStatus.CaptureData(transform);
            return null;
        }

        public void RestoreData(object data)
        {
            var d = data as DenizenStatus.DenizenSaveData;
            if (d == null || denizenStatus == null) return;
            denizenStatus.RestoreData(d, transform);
            NPCRole savedRole = denizenStatus.Role;
            if (savedRole != CurrentRole) ChangeRole(savedRole);
            else ApplyRole(savedRole);
        }

        public bool HandleItem(GameObject item)
        {
            if (currentActiveBehavior is IItemHandle itemHandler)
            {
                return itemHandler.HandleItem(item);
            }
            return false;
        }

        public void ReceiveDamage(float damage, GameObject instigator)
        {
            if (damage <= 0) return;
            if (CurrentRole == NPCRole.Vagrant) return;

            if (CurrentRole == NPCRole.Archer)
            {
                if (denizenStatus.CurrentCoins > 1)
                {
                    denizenStatus.SubtractCoin(1);
                    Spawner.Instance?.DropByDenizen(transform.position, 1, Vector2.up);
                }
                else if (denizenStatus.CurrentCoins == 1)
                {
                    if (Spawner.Instance != null && PrefabData.Instance != null)
                    {
                        Spawner.Instance.DropItem(PrefabData.Instance.bow, transform.position,
                            playerPickupAble: false, denizensPickupAble: true);
                    }
                    denizenStatus.isFearPending = true;
                    denizenStatus.fearTimer = 3f; 
                    denizenStatus.isInDanger = true;
                    ChangeRole(NPCRole.Villager);
                }
            }
            else if (CurrentRole == NPCRole.Villager)
            {
                if (denizenStatus.CurrentCoins > 0)
                {
                    denizenStatus.SubtractCoin(1);
                    Spawner.Instance?.DropByDenizen(transform.position, 1, Vector2.up);
                    if (villagerBehavior != null && villagerBehavior.isActiveAndEnabled)
                    {
                        villagerBehavior.TriggerFear();
                    }
                }
                if (denizenStatus.CurrentCoins <= 0)
                {
                    ChangeRole(NPCRole.Vagrant);
                }
            }
        }
    }
}
```
