using UnityEngine;
using Script.Game.Mobs.MobsManager;
using Script.Controller.PlayerController;

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

            // Tính vị trí spawn trước mặt Player
            float dir = owner.transform.localScale.x;
            Vector3 spawnPos = owner.transform.position + new Vector3(dir * 2f, 2f, 0f);

            vm.SpawnVagrants(1, null, spawnPos);
            Debug.Log($"[SpawnVagrantSkill] Spawned 1 Vagrant at {spawnPos}");
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
