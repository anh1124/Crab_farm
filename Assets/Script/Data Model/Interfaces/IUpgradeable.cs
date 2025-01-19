using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IUpgradeable
{
    int Level { get; set; }
    void Upgrade(); // Thực hiện nâng cấp
    int GetUpgradeCost(); // Lấy chi phí nâng cấp
}
