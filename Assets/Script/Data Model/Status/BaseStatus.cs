using System;
using Script.Data_Model;
using Script.Data_Model.Interfaces;
using Script.Game.Enemy.Interfaces;
using UnityEngine;
using UnityEngine.Serialization;

public class BaseStatus : MonoBehaviour, IDamageable,IHealable,ICoinable,IDropable
{
    //đây là status 
    /*public int MaxHp { get; set; }        
    public int CurrentHp { get; set; }    
    public int Coin { get; set; } */     
    public float moveSpeed;                 
    public Vector2 position;               
    public bool isGround;
    
    //cái đống này chỉ để hiện ra thôi,ko dc sài
    [SerializeField] private int maxHp;  // Thêm field private
    [SerializeField] private int currentHp;
    [SerializeField] private int coin;
    
    // Implement interface qua properties
    public int MaxHp { get => maxHp; set => maxHp = value; }
    public int CurrentHp { get => currentHp; set => currentHp = value;}
    public int Coin { get => coin; set => coin = value; }
    
    
    
    public void UpdatePosition()
    {
        position = gameObject.transform.position;
    }
    
    
    public virtual void TakeDamage(int damage, GameObject whoDoDame)
    {
        // Nếu không có coin để rớt, giảm HP
        if (Coin <= 0)
        {
            CurrentHp -= damage;
            if (CurrentHp < 0)
            {
                CurrentHp = 0;
            }
        }
        else
        {
            // Nếu còn coin, làm rơi coin và không giảm HP
            DropCoin(whoDoDame);
        }

        // Nếu HP bằng 0, đối tượng sẽ chết
        if (CurrentHp == 0)
        {
            Die(whoDoDame);
        }
    }
    public virtual void Die(GameObject whoDoDame)
    {
        Debug.Log($"{gameObject.name} đã bị tiêu diệt bởi {whoDoDame.name}.");
    }
    
    public virtual void Heal(int healAmount)
    {
        CurrentHp += healAmount;
        if (CurrentHp > MaxHp)
        {
            CurrentHp = MaxHp;
        }
    }

    
    public void AddCoin(GameObject coinGameObject)
    {
        Coin++;
        Destroy(coinGameObject);
    }
    public void DropCoin(GameObject whoDrop)
    {
        Coin--; // Giảm số coin trong tay
        Debug.Log($"{gameObject.name} rớt một coin khi bị {whoDrop.name} tấn công.");
        // Có thể spawn một coin vào vị trí hiện tại nếu cần
        Drop(whoDrop,PrefabData.Instance.coinPrefab, 1, gameObject.transform.position);
    }

    public void Drop(GameObject whoDrop,GameObject prefab,int amount, Vector2 pos)
    {
        // Kiểm tra nếu prefab coin không phải null
        if (prefab != null)
        {
            // Tạo coin tại vị trí pos, với góc quay mặc định (no rotation)
            Instantiate(prefab, pos, Quaternion.identity);
        }
        else
        {
            Debug.LogError(" prefab không hợp lệ!");
        }
    }
    
    // Kiểm tra script cần thiết
    private void Start()
    {
        CheckRequireScript();
    }
    private void CheckRequireScript()
    {
        if (gameObject.GetComponent<GroundCheck>() == null)
        {
            Debug.LogWarning($"{gameObject.name} không có script GroundCheck.");
        }
        if (gameObject.GetComponent<TriggerCheckSetup>() == null)
        {
            Debug.LogWarning($"{gameObject.name} không có script TriggerCheckSetup.");
        }
    }
}
