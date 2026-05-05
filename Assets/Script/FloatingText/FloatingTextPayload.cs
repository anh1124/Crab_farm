using UnityEngine;

namespace Script.FloatingText
{
    public readonly struct FloatingTextPayload
    {
        public readonly Vector3          WorldPosition;
        public readonly float            Value;
        public readonly FloatingTextType Type;

        public FloatingTextPayload(Vector3 worldPosition, float value, FloatingTextType type)
        {
            WorldPosition = worldPosition;
            Value         = value;
            Type          = type;
        }
    }
}
