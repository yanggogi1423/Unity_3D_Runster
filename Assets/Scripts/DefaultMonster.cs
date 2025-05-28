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
    [SerializeField] private float traceDist;
    [SerializeField] private float attackDist;

    [Header("Patrol Settings")]
    [SerializeField] private float patrolRadius = 5f;
    [SerializeField] private float patrolWaitTime = 3f;
    private float patrolTimer;
    private Vector3 patrolTarget;

    [SerializeField] private int maxAttackLegs = 3;
    [SerializeField] private float attackCooldown = 1f;
    private float lastAttackTime = -999f;

    [Header("References")] 
    [SerializeField] private List<Renderer> renderers = new List<Renderer>(); // 부체 렌더러드
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
                de.agent.isStopped = false;
                HandlePatrol();
                break;

            case MonsterState.Trace:
                de.agent.isStopped = false;
                HandleTrace();
                break;

            case MonsterState.Attack:
                de.agent.isStopped = true;
                HandleAttack();
                break;

            case MonsterState.Die:
                de.agent.isStopped = true;
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
            curState = MonsterState.Patrol;
        }
    }

    public float CheckDistanceToTarget()
    {
        return Vector3.Distance(transform.position, de.target.transform.position);
    }

    private void HandleTrace()
    {
        if (de.agent.isStopped) return;
        de.agent.SetDestination(de.target.position);
    }

    private void HandleAttack()
    {
        de.agent.SetDestination(transform.position);

        Vector3 dir = (de.target.position - transform.position).normalized;
        dir.y = 0f;
        if (dir != Vector3.zero)
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 10f);

        if (Time.time - lastAttackTime >= attackCooldown)
        {
            lastAttackTime = Time.time;
            AttackWithLegs();
        }
    }

    private void AttackWithLegs()
    {
        if (mimic.legLineList.Count == 0) return;

        List<LineRenderer> legLines = new List<LineRenderer>(mimic.legLineList);
        int count = Mathf.Min(maxAttackLegs, legLines.Count);

        for (int i = 0; i < count; i++)
        {
            int idx = Random.Range(0, legLines.Count);
            LineRenderer selectedLeg = legLines[idx];
            legLines.RemoveAt(idx);
            StartCoroutine(SimulateLegAttack(selectedLeg));
        }

        de.target.GetComponent<Player>()?.GetDamage(attackPower);
    }

    private IEnumerator SimulateLegAttack(LineRenderer leg)
    {
        if (leg == null) yield break;

        Color originalColor = leg.material.color;
        leg.material.color = Color.red;

        float t = 0f;
        float duration = 0.2f;
        Vector3[] originalPoints = new Vector3[leg.positionCount];
        leg.GetPositions(originalPoints);

        Vector3[] newPoints = new Vector3[leg.positionCount];
        originalPoints.CopyTo(newPoints, 0);

        for (; t < duration; t += Time.deltaTime)
        {
            for (int i = 1; i < newPoints.Length; i++)
            {
                newPoints[i] += Vector3.forward * 0.01f;
            }

            leg.SetPositions(newPoints);
            yield return null;
        }

        leg.SetPositions(originalPoints);
        leg.material.color = originalColor;
    }

    private void HandlePatrol()
    {
        patrolTimer += Time.deltaTime;

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
        StartCoroutine(ShakeCoroutine());
    }

    private IEnumerator FadeOutCoroutine()
    {
        float timer = 0f;

        foreach (var rend in renderers)
        {
            rend.material = new Material(rend.material);
        }

        if (mimic != null)
        {
            foreach (var legLine in mimic.legLineList)
            {
                if (legLine != null)
                    legLine.material = new Material(legLine.material);
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

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            other.gameObject.GetComponent<Player>().GetDamage(attackPower);
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, traceDist);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackDist);
    }

}