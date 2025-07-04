using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class Player : MonoBehaviour
{
    [Header("Attributes")] 
    [SerializeField] public float maxHp;
    [SerializeField] public float desireHp;
    [SerializeField] public float curHp;
    
    [SerializeField] public float maxBoost;
    [SerializeField] public float desireBoost;
    [SerializeField] public float curBoost;
    [SerializeField] public bool allowBoostConsume;
    
    [SerializeField] public float maxUltimate;
    [SerializeField] public float desireUltimate;
    [SerializeField] public float curUltimate;
    
    [SerializeField] private float graceTime = 0.5f;  //  무적 시간
    [SerializeField] private bool isGrace;

    [SerializeField] public bool isDead;

    [Header("References")]
    public CapsuleCollider cc;
    public PlayerMovement pm;
    public Rigidbody rb;
    private UltimateController ultimateController;

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
    public Coroutine boostCoroutine;
    private bool isBoostCharge;
    private Coroutine boostLessCoroutine;

    [Header("Tutorial")]
    public bool isTutorial = false;
    public TutorialManager tm;
    
    private void Awake()
    {
        pm = GetComponent<PlayerMovement>();
        
        //  Init
        isDead = false;
        curHp = desireHp = maxHp;
        curBoost = desireBoost = maxBoost;
        
        //  For Debug -> 원래는 0으로 초기화
        curUltimate = desireUltimate = 0;
        
        isGrace = false;
        
        isPause = false;
        isInit = true;
        
        isBoostCharge = false;
        
        killedEnemies = 0;

        ultimateOffset = 11f;
        isKillTime = false;
        
        boostCoroutine = null;
        boostLessCoroutine = null;

        isHyper = false;
        rb = GetComponent<Rigidbody>();

        allowBoostConsume = true;

        //  Tutorial
        if (SceneManager.GetActiveScene().name == "Tutorial")
        {
            isTutorial = true;
        }
        else isTutorial = false;
    }

    private void Start()
    {
        ultimateController = GetComponent<UltimateController>();
        
        OnPlayerHpChanged.Invoke();
        OnPlayerBoostChaged.Invoke();
        OnPlayerUltimateChanged.Invoke();
    }

    private void Update()
    {
        if (tm != null && tm.isShowingText)
        {
            allowBoostConsume = false;
        }
        else if(tm != null && !tm.isShowingText) allowBoostConsume = true;
        
        if (!isHyper && rb.linearVelocity.magnitude > 13.5f && pm.curState != PlayerMovement.MovementState.Air)
        {
            isHyper = true;
            hyperEffects.SetActive(true);
        }
        else if(isHyper && rb.linearVelocity.magnitude <= 13.5f)
        {
            isHyper = false;
            hyperEffects.SetActive(false);
        }
        
        if (tm != null && isTutorial && tm.isShowingText) return;

        if (tm != null)
        {
            if (!isTutorial || (isTutorial && tm.curState > TutorialManager.State.Boost))
            {
                if ((tm != null && !tm.isShowingText) || tm == null)
                {
                    CheckAboutBoost();
                }
                
                else if(boostCoroutine != null)
                {
                    StopCoroutine(boostCoroutine);
                }
            }

            if (!isTutorial || (isTutorial && tm.curState >= TutorialManager.State.PlayerUltimate))
            {
                CheckAboutUltimate();    
            }
        }
        else
        {
            CheckAboutBoost();
            CheckAboutUltimate(); 
        }
        
        
        //  Hyper
        if (!isHyper && rb.linearVelocity.magnitude > 13.5f && pm.curState != PlayerMovement.MovementState.Air)
        {
            isHyper = true;
            hyperEffects.SetActive(true);
        }
        else if(isHyper && rb.linearVelocity.magnitude <= 13.5f)
        {
            isHyper = false;
            hyperEffects.SetActive(false);
        }
    }

    public void CheckAboutBoost()
    {
        if (curBoost <= 0 && boostLessCoroutine == null)
            NotEnoughBoost();

        //  궁극기 중에는 Boost 감소 및 회복 X
        if (ultimateController.IsUltimateActive || !allowBoostConsume)
        {
            if (boostCoroutine != null)
            {
                StopCoroutine(boostCoroutine);
                boostCoroutine = null;
            }
            return;
        }

        bool shouldCharge = pm.GetNonBoostTime() < 0;
        
        if (isInit || (shouldCharge != isBoostCharge) || boostCoroutine == null)
        {
            if (boostCoroutine != null)
                StopCoroutine(boostCoroutine);

            isBoostCharge = shouldCharge;

            float offset = isBoostCharge ? 0.3f : -0.3f;

            if (offset < 0.3 && !allowBoostConsume)
            {
                boostCoroutine = null;
            }
            else
            {
                boostCoroutine = StartCoroutine(BoostChangeCoroutine(offset));    
            }
            
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
            {
                boostCoroutine = null;
                yield break;
            }

            yield return new WaitForSeconds(0.05f);  // 빠른 속도로 반응하도록
        }
    }
    
    public void StopBoostCoroutine()
    {
        if (boostCoroutine != null)
        {
            StopCoroutine(boostCoroutine);
            boostCoroutine = null;
        }
    }



    public void KillEnemies()
    {
        killedEnemies++;
        
        killTimer = maxKillTime;

        desireUltimate = Mathf.Clamp(desireUltimate + ultimateOffset,0f,maxUltimate);
        
        OnPlayerUltimateChanged.Invoke();

        ultimateOffset += 1;

        if (ultimateOffset >= 15f)
        {
            ultimateOffset = 15f;
        }
    }

    public void UltimateForTutorial()
    {
        desireUltimate = Mathf.Clamp(desireUltimate + 100f,0f, maxUltimate);
        OnPlayerUltimateChanged.Invoke();
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
            ultimateOffset = 11f;
            
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

        AudioManager.Instance.PlaySfx(AudioManager.Sfx.PlayerHit);

        if (curHp <= 0f && !isDead)
        {

            Die();
        }
        else
        {

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
        pm.anim.SetTrigger("die");
        isDead = true;

        AudioManager.Instance.PlaySfx(AudioManager.Sfx.PlayerDie);

        StartCoroutine(DieCoroutine());
    }

    private IEnumerator DieCoroutine()
    {
        yield return new WaitForSeconds(5f);
        GameManager.Instance.playerDie = true;
        AudioManager.Instance.StopAllLoopingSfx();
        AudioManager.Instance.StopAllSfx();
        SceneController.Instance.LoadEndingScene();
    }

    public bool GetIsGrace()
    {
        return isGrace;
    }

    public void GetItemHealPack(int offset)
    {
        desireHp = Mathf.Clamp(desireHp + offset, 0, maxHp);
        curHp = desireHp;
        OnPlayerHpChanged.Invoke();
    }

    public void GetItemCell(int offset)
    {
        if (boostCoroutine != null)
        {
            StopCoroutine(boostCoroutine);
        }
        
        desireBoost = Mathf.Clamp(desireBoost + offset, 0, maxBoost);
        curBoost = desireBoost;
        OnPlayerBoostChaged.Invoke();
        boostCoroutine = null;
        boostLessCoroutine = null;
        isInit = true;
    }

}
