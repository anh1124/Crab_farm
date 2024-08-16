using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TownCentrerList : MonoBehaviour
{
    public static TownCentrerList Instance { get; private set; }

    [System.Serializable] 
    public class TownCentrerData 
    {
        public int tier;
        public  string name;
        public int buildCost;
        public bool blessing;

        public GameObject nextUpgrade;
        public List<GameObject> atribute;
    }
    [SerializeField] private List<TownCentrerData> townCentrerList;
}
