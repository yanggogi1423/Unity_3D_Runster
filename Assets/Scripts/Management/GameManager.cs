using System;
using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    [Header("Is Clear")] public bool isClear = false;

    public bool playerDie = false;

}
