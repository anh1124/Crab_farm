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
        Archer
    }

    public class DenizenController : MonoBehaviour, ISaveable
    {
        public NPCRole CurrentRole = NPCRole.Vagrant;

        private Animator animator;
        private RayCastHelper rayCastHelper;

        private MonoBehaviour currentBehavior;
        private MonoBehaviour currentStatus;

        [Header("Animator Controllers")]
        [SerializeField] private RuntimeAnimatorController vagrantController;
        [SerializeField] private RuntimeAnimatorController villagerController;
        [SerializeField] private RuntimeAnimatorController archerController;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            rayCastHelper = GetComponent<RayCastHelper>();
            if (gameObject.tag != "Vagrant" && gameObject.tag != "Villager" && gameObject.tag != "Archer")
            {
                gameObject.tag = "Denizen"; // base tag; ApplyRole will overwrite with specific role tag
            }
        }

        private void Start()
        {
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

            if (animator != null)
            {
                // Optional: play a transition animation if exists
                // animator.Play("ChangeRole");
            }

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
                    // currentStatus = gameObject.AddComponent<ArcherStatus>();
                    // currentBehavior = gameObject.AddComponent<ArcherBehavior>();
                    targetController = archerController;
                    break;
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
        [System.Serializable]
        public class DenizenData
        {
            public float positionX;
            public float positionY;
            public float scaleX;
            public int role;
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
                role = (int)CurrentRole,
                coins = coins
            };
        }

        public void RestoreData(object data)
        {
            var d = data as DenizenData;
            if (d == null) return;

            transform.position = new Vector3(d.positionX, d.positionY, 0);
            transform.localScale = new Vector3(d.scaleX, 1, 1);

            var savedRole = (NPCRole)d.role;
            if (savedRole != CurrentRole)
            {
                ChangeRole(savedRole);
            }
            // Ensure coins applied after role is loaded
            if (currentStatus is INPCStatus s)
            {
                s.CurrentCoins = d.coins;
            }

            // Ensure container is updated after restore
            Script.Manager.EntitySaveManager.Instance?.MoveObjectToContainer(gameObject);
        }
    }
}
