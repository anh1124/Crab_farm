using UnityEngine;

namespace Script.Game.Item
{
    public class ToolStats : MonoBehaviour
    {
        public bool isPickedByEnemy { get; private set; }
        public GameObject currentHolder { get; private set; }

        public delegate void ToolPickupHandler(GameObject holder);
        public event ToolPickupHandler OnToolPickup;

        public void SetPickedUp(GameObject holder, bool byEnemy = false)
        {
            isPickedByEnemy = byEnemy;
            currentHolder = holder;
            OnToolPickup?.Invoke(holder);
        }

        public void ResetState()
        {
            isPickedByEnemy = false;
            currentHolder = null;
        }
    }
}
