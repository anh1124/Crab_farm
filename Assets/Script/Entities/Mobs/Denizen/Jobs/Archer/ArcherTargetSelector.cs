using System.Collections.Generic;
using UnityEngine;
using Script.Game.Mobs.Denizen.Core;
using Script.Utils;

namespace Script.Game.Mobs.Denizen.Jobs.Archer
{
    public class ArcherTargetSelector : DenizenModule
    {
        public ArcherTargetSelector(DenizenContext context) : base(context) { }

        public GameObject SelectCombatTarget(List<GameObject> objects)
        {
            // Ưu tiên 1: Enemy (chặn đầu)
            GameObject target = FindClosestTarget(objects, new[] { GameTags.Enemy });
            if (target != null) return target;

            // Ưu tiên 2: EnemyPortal và Animal (cùng cấp, chọn cái gần nhất)
            return FindClosestTarget(objects, new[] { GameTags.EnemyPortal, GameTags.Animal });
        }

        private GameObject FindClosestTarget(List<GameObject> objects, string[] validTags)
        {
            GameObject best = null;
            float minDistance = float.MaxValue;
            foreach (var go in objects)
            {
                if (go == null) continue;
                
                bool hasTag = false;
                for (int i = 0; i < validTags.Length; i++)
                {
                    if (go.CompareTag(validTags[i]))
                    {
                        hasTag = true;
                        break;
                    }
                }

                if (hasTag)
                {
                    float dist = Vector2.Distance(Context.transform.position, go.transform.position);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        best = go;
                    }
                }
            }
            return best;
        }
    }
}
