using System;
using System.Collections;
using System.Collections.Generic;
using MimicSpace;
using UnityEngine;
using UnityEngine.AI;

public class DefaultMonster : MonoBehaviour
{
    
    [Header("Attributes")] 
    [SerializeField] private int maxHp;
    [SerializeField] private int curHp;
    public int attackPower = 10;

    [Header("For Animations")] public float fadeDuration = 2f;

    [Header("Distance Values")] 
    [SerializeField] private float traceDist;
    [SerializeField] private float attackDist;
    
    [Header("Patrol Settings")]
    [SerializeField] private float patrolRadius = 5f;
    [SerializeField] private float patrolWaitTime = 3f;

    private float patrolTimer;
    private Vector3 patrolTarget;


    [Header("References")] [SerializeField]
    private List<Renderer> renderers = new List<Renderer>(); // 본체 렌더러들

    [SerializeField] private Mimic mimic; // Mimic 참조
    [SerializeField] private DefaultEnemyMovement de;
    
    [Header("Particles")]
    public GameObject spawnParticle;
    public float particleDuration = 3f;
    public bool isSpawning;

    [Header("Handler")]
    [SerializeField] private bool isDead;
    
    
    //  State Machine
    public enum MonsterState
    {
        Idle,
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
        
        curState = MonsterState.Idle;
    }

    private void Start()
    {
        de = GetComponent<DefaultEnemyMovement>();
        
        StartSpawnEffect();
    }
    
    private void Update()
    {
        if (isDead || isSpawning) return;

        CheckState();

        switch (curState)
        {
            case MonsterState.Patrol:
                HandlePatrol();
                break;
            case MonsterState.Trace:
                HandleTrace();
                break;
            case MonsterState.Attack:
                HandleAttack();
                break;
        }

    }

    public void CheckState()
    {
        float dist = CheckDistanceToTarget();

        if (dist <= attackDist)
        {
            curState = MonsterState.Attack;
        }
        else if (dist <= traceDist)
        {
            curState = MonsterState.Trace;
        }
        else
        {
            curState = MonsterState.Patrol; // ✅ 추가
        }
    }


    public float CheckDistanceToTarget()
    {
        return Vector3.Distance(transform.position, de.target.transform.position);
    }
    
    private void HandleTrace()
    {
        if (de.agent.isStopped) return;

        // 플레이어 위치를 따라감
        de.agent.SetDestination(de.target.position);
    }
    
    private void HandleAttack()
    {
        // 공격 범위 내 도달한 상태 → 멈추고 공격 애니메이션 또는 타격 처리
        de.agent.SetDestination(transform.position); // 멈춤

        // 여기서 플레이어 바라보게
        Vector3 dir = (de.target.position - transform.position).normalized;
        dir.y = 0f;
        if (dir != Vector3.zero)
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 10f);

        // 공격 조건 타이밍 또는 애니메이션 트리거는 이곳에서 처리 가능
        // 예시: anim.SetTrigger("attack");
    }


    
    private void HandlePatrol()
    {
        patrolTimer += Time.deltaTime;

        // 도착했거나 목적지 없음 → 새 목적지 설정
        if (!de.agent.hasPath || de.agent.remainingDistance < 0.5f)
        {
            if (patrolTimer >= patrolWaitTime)
            {
                Vector3 randomDir = UnityEngine.Random.insideUnitSphere * patrolRadius;
                randomDir += transform.position;

                if (NavMesh.SamplePosition(randomDir, out NavMeshHit hit, patrolRadius, NavMesh.AllAreas))
                {
                    patrolTarget = hit.position;
                    de.agent.SetDestination(patrolTarget);
                }

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

        de.agent.isStopped = true; // 잠시 멈춤

        while (timer < particleDuration)
        {
            timer += Time.deltaTime;
            float t = timer / particleDuration;

            // 선형으로 작아짐
            spawnParticle.transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, t);

            yield return null;
        }

        spawnParticle.SetActive(false);
        de.agent.isStopped = false; // 이동 재개
        isSpawning = false;
    }


    public void GetDamage(int damage)
    {
        curHp -= damage;

        if (curHp <= 0 && !isDead)
        {
            Debug.Log("Monster Die!");

            curState = MonsterState.Die;
            
            isDead = true;
            de.target.gameObject.GetComponent<Player>().KillEnemies();
            
            FadeAndDie();
        }
    }

    private void FadeAndDie()
    {
        de.agent.isStopped = true;
        StartCoroutine(FadeOutCoroutine());
        StartCoroutine(ShakeCoroutine()); // 👈 흔들림 코루틴 시작
    }

    private IEnumerator FadeOutCoroutine()
    {
        float timer = 0f;

        // 본체 렌더러
        foreach (var rend in renderers)
        {
            rend.material = new Material(rend.material);
        }

        // 다리 라인렌더러
        if (mimic != null)
        {
            foreach (var legLine in mimic.legLineList)
            {
                if (legLine != null)
                    legLine.material = new Material(legLine.material);
            }
        }

        if (mimic != null)
        {
            foreach (var legLine in mimic.legLineList)
            {
                if (legLine != null)
                {
                    legLine.material = new Material(legLine.material);
                }
            }
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

        if (mimic != null)
        {
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
            float damper = 1f - progress;  // 시간이 지날수록 약해짐

            // Y축, Z축 기반의 랜덤 진동값
            float yAngle = Mathf.PerlinNoise(Time.time * 10f, 0f) * 10f - 5f;
            float zAngle = Mathf.Sin(elapsed * 25f) * 5f;

            yAngle *= damper;
            zAngle *= damper;

            // 회전 적용
            transform.rotation = originalRot * Quaternion.Euler(0f + yAngle, 0f, zAngle);
            yield return null;
        }

        transform.rotation = originalRot;
    }

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            other.gameObject.GetComponent<Player>().GetDamage(attackPower);
        }
    }
}