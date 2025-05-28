using System;
using System.Collections;
using System.Collections.Generic;
using MimicSpace;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public class DefaultMonster : MonoBehaviour
{
    [Header("Attributes")]
    [SerializeField] private int maxHp;
    [SerializeField] private int curHp;
    public int attackPower = 10;

    [Header("For Animations")] public float fadeDuration = 2f;

    [Header("Distance Values")]
    private Transform playerTarget;
    [SerializeField] private float traceDist;
    [SerializeField] private float attackDist;

    [Header("Patrol Settings")]
    [SerializeField] private float patrolRadius = 5f;
    [SerializeField] private float patrolWaitTime = 3f;
    private float patrolTimer;
    private Vector3 patrolTarget;

    [SerializeField] private int maxAttackLegs = 3;
    [SerializeField] private float attackCooldown = 3f;
    private float lastAttackTime = -999f;

    [Header("References")]
    [SerializeField] private List<Renderer> renderers = new List<Renderer>();
    [SerializeField] private Mimic mimic;
    [SerializeField] private DefaultEnemyMovement de;

    [Header("Particles")]
    public GameObject spawnParticle;
    public float particleDuration = 3f;
    public bool isSpawning;

    [Header("Handler")]
    [SerializeField] private bool isDead;

    [Header("Manager")] public InGameManager inGameManager;

    [Header("Effects")] public GameObject ultimateMonster;
    [SerializeField] private Renderer sphereRenderer; // 인스펙터에서 할당
    private Material sphereMaterialInstance;
    private GameObject hitEffectPrefab;

    private bool isUltimate;

    public enum MonsterState
    {
        Trace,
        Attack,
        Patrol,
        Die
    }

    public MonsterState curState;

    private void Awake()
    {
        curHp = maxHp;
        if (mimic == null)
            mimic = GetComponent<Mimic>();

        isDead = false;
        curState = MonsterState.Patrol;
        playerTarget = GameObject.FindGameObjectWithTag("Player").transform;

        isUltimate = false;
    }

    private void Start()
    {
        de = GetComponent<DefaultEnemyMovement>();
        inGameManager = GameObject.Find("InGameManager").GetComponent<InGameManager>();
        
        if (sphereRenderer != null)
        {
            // 공유 머티리얼을 복제해서 이 오브젝트만의 머티리얼로 만든다
            sphereMaterialInstance = new Material(sphereRenderer.sharedMaterial);
            sphereRenderer.material = sphereMaterialInstance;
            sphereMaterialInstance.color = Color.black; // 시작은 검정색
        }
        
        hitEffectPrefab = Resources.Load<GameObject>("vfx_Explosion_01");
        
        StartSpawnEffect();
    }

    private void Update()
    {
        if (isDead || isSpawning) return;

        CheckState();

        switch (curState)
        {
            case MonsterState.Patrol:
                HandlePatrol(); break;
            case MonsterState.Trace:
                de.agent.isStopped = false;
                HandleTrace(); break;
            case MonsterState.Attack:
                de.agent.isStopped = false;
                HandleAttack(); break;
            case MonsterState.Die:
                de.agent.isStopped = true;
                break;
        }
        
    }

    public void CheckState()
    {
        //  Ultimate State
        if (curHp <= maxHp * 0.3f && !isUltimate)
        {
            de.target = playerTarget;
            isUltimate = true;
            //  attackDist *= 2f;
            traceDist *= 15f;

            UltimateMonster();
        }
        
        float dist = Vector3.Distance(transform.position, playerTarget.position);

        if (dist <= attackDist)
        {
            curState = MonsterState.Attack;
            de.target = playerTarget;
        }
        else if (dist <= traceDist)
        {
            curState = MonsterState.Trace;
            de.target = playerTarget;
        }
        else if (!de.agent.hasPath || de.agent.remainingDistance < 0.5f || dist > traceDist)
        {
            curState = MonsterState.Patrol;
            de.target = null;
        }
    }

    private void UltimateMonster()
    {
        ultimateMonster.SetActive(true);
        de.speed *= 3.5f;
        de.velocityLerpCoef *= 2f;
        
        //  mimic.BoostLegSpeed();
        
        Invoke(nameof(TurnOffUltimate),3f);
    }

    private void TurnOffUltimate()
    {
        ultimateMonster.SetActive(false);
    }

    private void HandleTrace()
    {
        if (de.agent.isStopped || de.target == null) return;
        de.agent.SetDestination(de.target.position);
    }

    private void HandleAttack()
    {
        if (de.target == null) return;

        de.agent.SetDestination(transform.position);

        Vector3 dir = (de.target.position - transform.position).normalized;
        dir.y = 0f;
        if (dir != Vector3.zero)
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 10f);

        if (Time.time - lastAttackTime >= attackCooldown)
        {
            lastAttackTime = Time.time;
            mimic.AttackWithLegsAtTarget(de.target.position, maxAttackLegs);
            de.target.GetComponent<Player>()?.GetDamage(attackPower);
        }
    }

    private void HandlePatrol()
    {
        patrolTimer += Time.deltaTime;

        if (!de.agent.hasPath || de.agent.remainingDistance < 0.5f)
        {
            if (patrolTimer >= patrolWaitTime)
            {
                Vector3 randomDir = Random.insideUnitSphere * patrolRadius;
                randomDir.y = 0f;
                Vector3 candidatePos = transform.position + randomDir;

                if (NavMesh.SamplePosition(candidatePos, out NavMeshHit hit, patrolRadius, NavMesh.AllAreas))
                {
                    patrolTarget = hit.position;
                    de.agent.isStopped = false;
                    de.agent.SetDestination(patrolTarget);
                }

                patrolWaitTime = Random.Range(2f, 3f);
                patrolTimer = 0f;
            }
        }
    }

    public void StartSpawnEffect()
    {
        if (spawnParticle == null) return;

        isSpawning = true;
        spawnParticle.SetActive(true);
        spawnParticle.transform.localScale = Vector3.one;

        StartCoroutine(SpawnEffectRoutine());
    }

    private IEnumerator SpawnEffectRoutine()
    {
        float timer = 0f;
        Vector3 originalScale = spawnParticle.transform.localScale;
        de.agent.isStopped = true;

        while (timer < particleDuration)
        {
            timer += Time.deltaTime;
            float t = timer / particleDuration;
            spawnParticle.transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, t);
            yield return null;
        }

        spawnParticle.SetActive(false);
        de.agent.isStopped = false;
        isSpawning = false;
    }

    public void GetDamage(int damage)
    {
        curHp -= damage;

        if (hitEffectPrefab != null)
        {
            GameObject fx = Instantiate(hitEffectPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
            Destroy(fx, 1.5f); // 일정 시간 후 자동 제거
        }
        else
        {
            Debug.LogError("Prefab is null");
        }
        UpdateSphereColor(); // 색상 갱신
        

        if (curHp <= 0 && !isDead)
        {
            curState = MonsterState.Die;
            isDead = true;
            
            de.target = playerTarget;   //  Null Exception 방지
            de.target.gameObject.GetComponent<Player>().KillEnemies();
            FadeAndDie();
        }
    }

    private void FadeAndDie()
    {
        de.agent.isStopped = true;
        
        inGameManager.UpdateMonsterText();
        
        StartCoroutine(FadeOutCoroutine());
        StartCoroutine(ShakeCoroutine());
    }

    private IEnumerator FadeOutCoroutine()
    {
        float timer = 0f;

        foreach (var rend in renderers)
            rend.material = new Material(rend.material);

        foreach (var legLine in mimic.legLineList)
        {
            if (legLine != null)
                legLine.material = new Material(legLine.material);
        }

        while (timer < fadeDuration)
        {
            float alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
            SetAlpha(alpha);
            timer += Time.deltaTime;
            yield return null;
        }

        SetAlpha(0f);
        Destroy(gameObject);
    }

    private void SetAlpha(float alpha)
    {
        foreach (var rend in renderers)
        {
            if (rend != null && rend.material.HasProperty("_Color"))
            {
                Color c = rend.material.color;
                c.a = alpha;
                rend.material.color = c;
            }
        }

        foreach (var legLine in mimic.legLineList)
        {
            if (legLine != null && legLine.material.HasProperty("_Color"))
            {
                Color c = legLine.material.color;
                c.a = alpha;
                legLine.material.color = c;
            }
        }
    }

    private IEnumerator ShakeCoroutine()
    {
        float shakeDuration = fadeDuration;
        float elapsed = 0f;
        Quaternion originalRot = transform.rotation;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / shakeDuration;
            float damper = 1f - progress;

            float yAngle = Mathf.PerlinNoise(Time.time * 10f, 0f) * 10f - 5f;
            float zAngle = Mathf.Sin(elapsed * 25f) * 5f;

            yAngle *= damper;
            zAngle *= damper;

            transform.rotation = originalRot * Quaternion.Euler(0f + yAngle, 0f, zAngle);
            yield return null;
        }

        transform.rotation = originalRot;
    }
    
    private void UpdateSphereColor()
    {
        if (sphereMaterialInstance == null) return;

        float healthRatio = 1f - (float)curHp / maxHp; // 체력이 100% → 0%
        Color newColor = Color.Lerp(Color.black, Color.red, healthRatio);
        sphereMaterialInstance.color = newColor;
    }


    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Player"))
            other.gameObject.GetComponent<Player>().GetDamage(attackPower);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, traceDist);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackDist);
    }
}
