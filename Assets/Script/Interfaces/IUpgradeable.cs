using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IUpgradeable
{
    void OnUpgradeComplete(); // Thực hiện nâng cấp
    int GetUpgradeCost(); // Lấy chi phí nâng cấp
}
