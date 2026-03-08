using UnityEngine;

namespace Script.Data_Model.Interfaces
{
    public interface INPCBehavior
    {
        void UpdateBehavior();
        void OnObjectsDetected(System.Collections.Generic.List<UnityEngine.GameObject> detectedObjects);
    }
}
