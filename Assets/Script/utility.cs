using UnityEngine;

namespace Script
{
    public static class Utility 
    {
        public static void RotateObjectTowardsTownCenter(GameObject obj, Vector3 referencePos, Vector3 townCenterPos)
        {
            Vector3 scale = obj.transform.localScale;
            if (referencePos.x < townCenterPos.x)
            {
                // Nếu vị trí đối tượng bên trái TownCenter thì lật sang trái (scale.x = -1)
                scale.x = -1f;
            }
            else
            {
                scale.x = 1f;
            }
            Debug.LogError("scale.x=" + scale.x);
            obj.transform.localScale = scale;
        }

    }
}
