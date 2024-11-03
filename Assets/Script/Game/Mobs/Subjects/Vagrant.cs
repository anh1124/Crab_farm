using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Vagrant : MonoBehaviour
{
    //kẻ lang thang sẽ bị tống ra khỏi cánh cổng cùng với quái vật mỗi đêm,số lượng sẽ là 1 đến 2 
    //khi va chạm 1 xu và xu đó có playerpickupable = false
    //(biến này false khi player vừa thả xu ra) sẽ nhạt và biến thành vllage
    public enum VagrantState
    {
        Fear,//khi có quái vật đứng cạnh sẽ ngồi xuống ôm đầu đến khi trời sáng 
        Idle,//khi không có quái vật sẽ idle,walk-
    }
    
}
