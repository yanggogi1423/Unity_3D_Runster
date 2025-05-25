using System;
using System.Collections;
using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("Attributes")] 
    [SerializeField] private int maxHp;
    [SerializeField] private int curHp;
    
    [SerializeField] private float graceTime = 3f;  //  무적 시간
    [SerializeField] private bool isGrace;

    [Header("References")]
    public CapsuleCollider cc;
    
    private void Awake()
    {
        curHp = maxHp;

        isGrace = false;
        
        
    }

    public void GetDamage(int damage)
    {
        if (isGrace) return;
            
        curHp -= damage;

        if (curHp <= 0)
        {
            Debug.Log("Game Over - Player Die");
            Die();
        }
        else
        {
            Debug.Log("Grace Time ! Current Hp : " + curHp);
            StartCoroutine(GraceTimeCoroutine());
        }
    }

    private IEnumerator GraceTimeCoroutine()
    {
        isGrace = true;
        yield return new WaitForSeconds(graceTime);
        isGrace = false;
    }

    private void Die()
    {
        Destroy(gameObject, 3f);
    }
}
