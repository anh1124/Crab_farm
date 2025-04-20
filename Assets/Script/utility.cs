using UnityEngine;

namespace Script
{
    public static class Utility 
    {
        public static bool isDebug = false;
        
        public static void RotateObjectTowardsTownCenter(GameObject obj, Vector3 referencePos, Vector3 townCenterPos)
        {
            Vector3 scale = obj.transform.localScale;
            
            // Calculate direction from reference position to town center
            Vector3 directionFromTownCenter = referencePos - townCenterPos;
            
            // We want objects to face RIGHT by default (x positive)
            // If object is to the left of town center, face right (scale.x positive)
            // If object is to the right of town center, face left (scale.x negative)
            if (directionFromTownCenter.x < 0)
            {
                // Object is to the LEFT of town center - should face RIGHT
                scale.x =-1f * Mathf.Abs(scale.x); // Ensure positive scale (face right)
            }
            else
            {
                // Object is to the RIGHT of town center - should face LEFT
                scale.x =  Mathf.Abs(scale.x); // Ensure negative scale (face left)
            }
            
            if (isDebug)
            {
                Debug.Log($"Utility.RotateObjectTowardsTownCenter: Object {obj.name} at pos {referencePos}, town center at {townCenterPos}, direction {directionFromTownCenter}, scale.x={scale.x}");
            }
            
            obj.transform.localScale = scale;
            
            // Ensure Z rotation is always 0
            obj.transform.rotation = Quaternion.identity;
        }

    }
}
