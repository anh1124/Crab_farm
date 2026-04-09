using UnityEngine;

namespace Script.Game.Combat
{
    public interface IArrowPool
    {
        void Initialize(GameObject prefab);
        GameObject GetArrow();
        void ReturnArrow(GameObject arrow);
    }
}
