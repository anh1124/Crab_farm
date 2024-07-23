using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CoinSlot : MonoBehaviour
{

    private SpriteRenderer _spriteRenderer;
    [SerializeField] public Sprite image;

    public Sprite coin;
    public Sprite empty;
    public Sprite locked;

    private void Start()
    {
       // SetCoin();
    }

    public void SetCoin()
    {
        image = _spriteRenderer.GetComponent<Sprite>();
        image = coin;
    }

    public void SetEmpty()
    {
        image = empty;
    }

    public void SetLock()
    {
        image = locked;
    }
}