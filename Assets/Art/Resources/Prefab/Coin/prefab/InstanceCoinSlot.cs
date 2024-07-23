using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InstanceCoinSlot : MonoBehaviour
{
    public static InstanceCoinSlot Instance { get;  set; }
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public GameObject coinSlotPrefab;
    
    public Sprite coinEmpty;
    public Sprite coinLocked;
    
    public float x = 1.6f;
    public float y = 1.0f;
    public float curvature = 0.5f; // Giá trị điều chỉnh độ cong (có thể điều chỉnh bên ngoài hàm)

    public void CreateCoinSlot(int coinQuantity, GameObject createLocation)
    {
        
        Vector3[] positions = CalculatePositions(coinQuantity);

        foreach (Vector3 position in positions)
        {
            GameObject coinSlot = Instantiate(coinSlotPrefab, createLocation.transform);
            coinSlot.transform.localPosition = position;
        }
    }



private Vector3[] CalculatePositions(int coinQuantity)
{
    List<Vector3> positions = new List<Vector3>();
    float startX = 0.0f;
    float startY = 0.0f;
    int maxSlotsPerRow = 10;
    int rows = 0;

    if (coinQuantity <= 7)
    {
        // Xếp tất cả các slot trên một hàng
        rows = 1;
    }
    else if (coinQuantity >= 8 && coinQuantity <= 20)
    {
        // Chia thành 2 hàng với số lượng slot phân phối đều
        rows = 2;
    }
    else // coinQuantity > 20
    {
        // Tính số hàng với tối đa 10 slot mỗi hàng
        rows = Mathf.CeilToInt(coinQuantity / (float)maxSlotsPerRow);
        startY = (rows - 1) * y; // Khoảng cách y giữa các hàng
    }

    int slotIndex = 0;
    for (int row = 0; row < rows; row++)
    {
        int slotsInCurrentRow;

        if (coinQuantity <= 7)
        {
            // Xếp tất cả các slot trên một hàng
            slotsInCurrentRow = coinQuantity;
        }
        else if (coinQuantity >= 8 && coinQuantity <= 20)
        {
            // Chia thành 2 hàng với số lượng phân phối đều
            slotsInCurrentRow = (row == 0) ? Mathf.CeilToInt(coinQuantity / 2.0f) : Mathf.FloorToInt(coinQuantity / 2.0f);
        }
        else
        {
            // Tính số lượng slot trong hàng hiện tại
            slotsInCurrentRow = (row < rows - 1) ? maxSlotsPerRow : (coinQuantity - slotIndex);
        }

        startX = -(slotsInCurrentRow - 1) * (x / 2);

        for (int i = 0; i < slotsInCurrentRow; i++)
        {
            // Tính toán độ cong
            float curveOffset = 0.0f;
            if (slotsInCurrentRow > 1)
            {
                // Tính toán độ cong theo hàm hình parabol
                float midIndex = (slotsInCurrentRow - 1) / 2.0f;
                float distanceFromMid = Mathf.Abs(i - midIndex);
                curveOffset = curvature * Mathf.Pow(distanceFromMid / midIndex, 2);
            }

            positions.Add(new Vector3(startX + i * x, startY - row * y - curveOffset, 0));
        }

        slotIndex += slotsInCurrentRow;
    }

    return positions.ToArray();
}



}
