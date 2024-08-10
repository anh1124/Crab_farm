using System;
using System.Collections;
using System.Collections.Generic;
using Script.Game.Item.Coin;
using Script.Game.Structures;
using UnityEngine;

public class WallLogic : MonoBehaviour
{
 
    /*
     * chú ý, stump ở layer defautl
     *         wall ở layer wall
     */
    public int tier;//phải nhập cái này 

    private CoinUser _coinUser;
    
   
    public new string name;
        
    
    public int rebuilding;
    public int buildCost;

    public int baseHp;
    public bool blessing;
    public int blessedHp;
    public int currentHp;

    public GameObject stump;
    public GameObject nextUpgrade;
    
    public bool readyToUpgrade;
  

    private void Start()
    {
        _coinUser = gameObject.GetComponent<CoinUser>();
        CheckScriptRequire();
        GetHp();
        GetDataFromList();
        WallTier();
    }
    
    //sau này nếu thêm statue blessing wall thì cho nó chạy lại hàm get hp
    public void GetHp()
    {
        if (blessing)
        {
            currentHp = blessedHp;
        }
        else
        {
            currentHp = baseHp;
        }
    }
    public void CheckScriptRequire()
    {
     
        _coinUser = gameObject.GetComponent<CoinUser>();
        if (_coinUser == null)
        {
            Debug.Log("lỗi : gameobject " + gameObject.name  +" thiếu coinUser" );
        }
       
    }
    public void WallTier()
    {
        switch (tier)
        {
            case 0:
            // Thực hiện hành động cho tier 0
            //Debug.Log("Tier 0");
            break;   
            case 1: break;   
        }
    }
    private void GetDataFromList()
    {
        WallAndStumpList wallAndStumpList = WallAndStumpList.Instance;
        if (wallAndStumpList != null)
        {
            WallAndStumpList.WallData wallData = wallAndStumpList.GetWallDataByTier(tier);
            if (wallData != null)
            {
                name = wallData.name;
                buildCost = wallData.buildCost;
                rebuilding = wallData.rebuilding;
                baseHp = wallData.baseHp;
                blessedHp = wallData.blessedHp;
                stump = wallData.stump;
                nextUpgrade = wallData.nextUpgrade;

                UpdateBuildCostInCoinUser();
            }
            else
            {
                Debug.LogWarning("Không tìm thấy WallData cho tier: " + tier);
            }
        }
        else
        {
            Debug.LogError("WallAndStumpList instance is null");
        }
    }

    public void UpdateBuildCostInCoinUser()
    {
        _coinUser.coinQuantity = buildCost;
    }
    public void TakeDame(int dame)
    {
        currentHp -= dame;
        if (currentHp <= 0)
        {
            TurnIntoStump();
        }
    }
    //hoạt động khi được sửa xong 
    public void FixingWall()
    {
        GetHp();
    }
    //hoạt động khi bị phá hủy 
    public void TurnIntoStump()
    {
        Debug.Log("bức tường " + name + " vừa bị phá hủy và chuyển thành stump");
        ConvertGamobject(stump);
    }
    //hoạt động nâng cấp
    public void DoUpgrade()
    {
        readyToUpgrade = true;
        ConvertGamobject(nextUpgrade);
        Destroy(gameObject);
    }
    //chức năng, tạo 1 object mới ở vị trí object hiện tại
    public void ConvertGamobject(GameObject gameObject)
    {
        // Lấy vị trí hiện tại của this.gameObject
        Vector3 currentPosition = this.gameObject.transform.position;
    
        // Khởi tạo stump tại vị trí hiện tại
        Instantiate(gameObject, currentPosition, Quaternion.identity);
    }
}
