using System;

namespace Script.Data_Model
{
    [Serializable]
    public class TechState
    {
        public bool TechFireArrow;
        public bool TechPenetrating;
        public bool TechForge;
        public bool TechAdhesive;

        public TechState(bool fireArrow = false, bool penetrating = false, bool forge = false, bool adhesive = false)
        {
            TechFireArrow = fireArrow;
            TechPenetrating = penetrating;
            TechForge = forge;
            TechAdhesive = adhesive;
        }

        public void CopyFrom(TechState other)
        {
            if (other == null) return;
            TechFireArrow = other.TechFireArrow;
            TechPenetrating = other.TechPenetrating;
            TechForge = other.TechForge;
            TechAdhesive = other.TechAdhesive;
        }
    }
}
