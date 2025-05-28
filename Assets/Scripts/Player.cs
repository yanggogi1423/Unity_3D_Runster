using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class Player : MonoBehaviour
{
    [Header("Attributes")] 
    [SerializeField] public float maxHp;
    [SerializeField] public float desireHp;
    [SerializeField] public float curHp;
    
    [SerializeField] public float maxBoost;
    [SerializeField] public float desireBoost;
    [SerializeField] public float curBoost;
    
    [SerializeField] public float maxUltimate;
    [SerializeField] public float desireUltimate;
    [SerializeField] public float curUltimate;
    
    [SerializeField] private float graceTime = 3f;  //  무적 시간
    [SerializeField] private bool isGrace;

    [Header("References")]
    public CapsuleCollider cc;
    private PlayerMovement pm;
    private Rigidbody rb;

    [Header("Events")] //  Sync가 제대로 안맞는 문제가 존재.
    public UnityEvent OnPlayerHpChanged;
    public UnityEvent OnPlayerBoostChaged;
    public UnityEvent OnPlayerUltimateChanged;

    [Header("Effects")] 
    public GameObject hyperEffects;

    private bool isHyper;

    [Header("Handler")] 
    public bool isPause;
    private bool isInit;
    
    [Header("Enemies")]
    [SerializeField] private int killedEnemies;

    [SerializeField] private float maxKillTime;
    [SerializeField] private float killTimer;
    [SerializeField] private float ultimateOffset;
    [SerializeField] private bool isKillTime;
    
    
    //  Coroutine
    private Coroutine boostCoroutine;
    private bool isBoostCharge;
    private Coroutine boostLessCoroutine;
    
    private void Awake()
    {
        pm = GetComponent<PlayerMovement>();
        
        //  Init
        curHp = desireHp = maxHp;
        curBoost = desireBoost = maxBoost;
        curUltimate = desireUltimate = 0;
        
        isGrace = false;
        
        isPause = false;
        isInit = true;
        
        isBoostCharge = false;
        
        killedEnemies = 0;

        ultimateOffset = 1f;
        isKillTime = false;
        
        boostCoroutine = null;
        boostLessCoroutine = null;

        isHyper = false;
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        OnPlayerHpChanged.Invoke();
        OnPlayerBoostChaged.Invoke();
        OnPlayerUltimateChanged.Invoke();
    }

    private void Update()
    {
        CheckAboutBoost();
        CheckAboutUltimate();
        
        //  Hyper
        if (!isHyper && rb.linearVelocity.magnitude > 12f && pm.curState != PlayerMovement.MovementState.Air)
        {
            isHyper = true;
            hyperEffects.SetActive(true);
        }
        else if(isHyper && rb.linearVelocity.magnitude <= 12f)
        {
            isHyper = false;
            hyperEffects.SetActive(false);
        }
    }

    public void CheckAboutBoost()
    {
        if (curBoost <= 0 && boostLessCoroutine == null)
            NotEnoughBoost();

        bool shouldCharge = pm.GetNonBoostTime() < 0;
        
        if (isInit || (shouldCharge != isBoostCharge))
        {
            if (boostCoroutine != null)
                StopCoroutine(boostCoroutine);

            isBoostCharge = shouldCharge;

            float offset = isBoostCharge ? 0.3f : -0.3f;
            boostCoroutine = StartCoroutine(BoostChangeCoroutine(offset));
        }

        isInit = false;
    }



    private void NotEnoughBoost()
    {
        boostLessCoroutine = StartCoroutine(DamageByBoostLessCoroutine());
    }

    private IEnumerator DamageByBoostLessCoroutine()
    {
        while (true)
        {
            if (curBoost > 0)
            {
                boostLessCoroutine = null;
                yield break;
            }

            desireHp -= 1f;
            OnPlayerHpChanged.Invoke();
            
            yield return new WaitForSeconds(0.6f);
        }
    }

    private IEnumerator BoostChangeCoroutine(float offset)
    {
        while (true)
        {
            // 범위 제한
            desireBoost = Mathf.Clamp(desireBoost + offset, 0f, maxBoost);
            curBoost = desireBoost;

            // UI 갱신 즉시 호출
            OnPlayerBoostChaged.Invoke();

            if ((isBoostCharge && curBoost >= maxBoost) || (!isBoostCharge && curBoost <= 0f))
                yield break;

            yield return new WaitForSeconds(0.05f);  // 빠른 속도로 반응하도록
        }
    }


    public void KillEnemies()
    {
        killedEnemies++;
        
        killTimer = maxKillTime;
        
        desireUltimate += ultimateOffset;
        
        OnPlayerUltimateChanged.Invoke();
        
        ultimateOffset++;
    }

    private void CheckAboutUltimate()
    {
        if (killTimer > 0f && !isKillTime)
        {
            isKillTime = true;
            
            StartCoroutine(KillTimeCoroutine());
        }
        else if (killTimer <= 0f && isKillTime)
        {
            isKillTime = false;
            ultimateOffset = 1f;
            
            killTimer = 0f;
        }
    }

    private IEnumerator KillTimeCoroutine()
    {
        while (killTimer > 0)
        {
            killTimer -= 0.1f;
            
            yield return new WaitForSeconds(0.1f);
        }
    }

    public void GetDamage(int damage)
    {
        if (isGrace) return;
            
        desireHp -= damage;
        
        OnPlayerHpChanged.Invoke();

        if (curHp <= 0f)
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

    public bool GetIsGrace()
    {
        return isGrace;
    }
}
