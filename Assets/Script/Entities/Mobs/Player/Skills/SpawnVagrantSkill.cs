using UnityEngine;
using Script.Game.Mobs.MobsManager;
using Script.Controller.PlayerController;
using Script.Data_Model;
using Script.Game.Mobs.Denizen.Core;
using Script.Manager;
using Script.Game.Mobs.Subjects;

namespace Script.Game.PlayerCharacter.Skills
{
    /// <summary>
    /// Skill tạm thời: Triệu hồi 1 Vagrant trước mặt Player.
    /// Cooldown 1 giây. Không tốn Stamina/Mana.
    /// Thay thế debug button Vagrant spawn cũ.
    /// </summary>
    [CreateAssetMenu(fileName = "SpawnVagrantSkill", menuName = "Skills/Player/SpawnVagrant")]
    public class SpawnVagrantSkill : SkillBase
    {
        public override void OnStart(GameObject owner, SkillManager manager)
        {
            var vm = FindObjectOfType<VagrantManager>();
            if (vm == null)
            {
                Debug.LogError("[SpawnVagrantSkill] VagrantManager not found in scene!");
                return;
            }

            // Tính vị trí spawn trước mặt Player (y = 0 để chạm đất)
            float dir = owner.transform.localScale.x;
            Vector3 spawnPos = owner.transform.position + new Vector3(dir * 2f, 0f, 0f);
            

            try
            {
                // Sử dụng Spawner trung tâm để sinh Denizen
                GameObject vagrantObj = Spawner.Instance.SpawnMob(spawnPos, PrefabData.Instance.denizen);
                
                if (vagrantObj != null)
                {
                    // Thiết lập vai trò Vagrant ngay lập tức
                    var roleCtrl = vagrantObj.GetComponent<DenizenRoleController>();
                    if (roleCtrl != null)
                    {
                        // Tag sẽ được tự động cập nhật trong ApplyRole
                        roleCtrl.ChangeRole(NPCRole.Vagrant);
                    }
                    
                    Debug.Log($"[SpawnVagrantSkill] Successfully spawned and initialized Vagrant at {spawnPos}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SpawnVagrantSkill] Error during spawning: {e.Message}");
            }
        }

        public override void OnHit(GameObject owner, SkillManager manager)
        {
            // Không có hitbox — spawn xảy ra ngay trong OnStart
        }

        public override void OnEnd(GameObject owner, SkillManager manager)
        {
            // Không cần cleanup
        }
    }
}
