using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IUpgradeable
{
    int Level { get; set; }
    int UpgradeCost { get; set; }
    GameObject CurrentPrefab{ get; set; }
    GameObject NextUpgradePrefab{ get; set; }
    bool ReadyToUpgrade{ get; set; }
    bool MaxLevel { get; set; }
    void Upgrade(); // Thực hiện nâng cấp
    int GetUpgradeCost(); // Lấy chi phí nâng cấp
}
