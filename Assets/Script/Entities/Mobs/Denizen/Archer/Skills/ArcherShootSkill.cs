using UnityEngine;
using Script.Game.PlayerCharacter.Skills;
using Script.Game.Mobs.Denizen.Core;

namespace Script.Game.Mobs.Denizen.Archer.Skills
{
    [CreateAssetMenu(fileName = "ArcherShootSkill", menuName = "Denizen/Skill/ArcherShoot")]
    public class ArcherShootSkill : SkillBase
    {
        [SerializeField] private DenizenContext context;

        public override void OnStart(GameObject owner, SkillManager manager)
        {
            // Logic đã được chuyển sang ArcherAttackState để tránh việc GetComponent.
            // Biến context ở đây dành cho việc "kéo tay" trong inspector nếu cần mở rộng sau này.
        }

        public override void OnHit(GameObject owner, SkillManager manager) { }

        public override void OnEnd(GameObject owner, SkillManager manager) { }
    }
}
