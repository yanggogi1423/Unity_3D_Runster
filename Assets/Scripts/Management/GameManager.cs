using System;
using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    [Header("Is Clear")] public bool isClear = false;

    public bool playerDie = false;
    
    [Header("Camera")]
    public float originX;
    public float originY;
    public float curX;
    public float curY;

    private void Awake()
    {
        originX = 4f;
        originY = 4f;
        curX = originX * 0.5f;
        curY = originY * 0.5f;
    }

}
