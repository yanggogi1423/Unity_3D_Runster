using System;
using System.Collections;
using System.Collections.Generic;
using MimicSpace;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Experimental.GlobalIllumination;
using Random = UnityEngine.Random;
using RenderSettings = UnityEngine.RenderSettings;

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
    private UltimateController uc;

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
    // 필요시 override 할 수 있게 public
    public Light directionalLight;

    private bool isUltimate;
    private Coroutine ultimateCoroutine;
    
    //  SkyBox
    private Material skyboxInstance;
    private static Material sharedSkyboxInstance;   //  모두가 공유하는 객체
    private Color originalSkyboxColor;
    private Color darkSkyboxColor = Color.black;
    private bool hasSkyboxColorProperty = false;

    [Header("Items")] 
    public GameObject healPackItem;

    public GameObject cellItem;
    
    [Header("Audio")]
    private Coroutine sfxCoroutine;
    
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
        
        //  Directional Light Access
        if (directionalLight == null)
        {
            directionalLight = RenderSettings.sun;
        }
        
        if (sharedSkyboxInstance == null && RenderSettings.skybox != null)
        {
            sharedSkyboxInstance = new Material(RenderSettings.skybox);
            RenderSettings.skybox = sharedSkyboxInstance;
        }

        skyboxInstance = sharedSkyboxInstance;

        isUltimate = false;
        ultimateCoroutine = null;
    }

    private void Start()
    {
        de = GetComponent<DefaultEnemyMovement>();
        uc = playerTarget.GetComponent<UltimateController>();
        
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
        
        sfxCoroutine = StartCoroutine(PlaySfxLoop());
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
        AudioManager.Instance.PlaySfx(AudioManager.Sfx.MonsterUltimate);
        
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

            if (isUltimate)
            {
                EnterUltimateDarknessMode(); // 반드시 실행
                if (ultimateCoroutine != null) StopCoroutine(ultimateCoroutine);
                ultimateCoroutine = StartCoroutine(UltimateDarknessModeCoroutine());
            }

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
            AudioManager.Instance.PlaySfx(AudioManager.Sfx.MonsterHit);
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
        
        uc?.OnMonsterDead(transform); // CrossHair 제거
        
        //  Collider 제거
        GetComponent<SphereCollider>().enabled = false;
        
        AudioManager.Instance.PlaySfx(AudioManager.Sfx.MonsterDie);
        
        StartCoroutine(FadeOutCoroutine());
        StartCoroutine(ShakeCoroutine());
    }

    private IEnumerator FadeOutCoroutine()
    {
        float timer = 0f;
        float startDensity = RenderSettings.fogDensity;
        

        while (timer < fadeDuration)
        {
            float t = timer / fadeDuration;

            float alpha = Mathf.Lerp(1f, 0f, t);
            SetAlpha(alpha);

            RenderSettings.fogDensity = Mathf.Lerp(startDensity, 0f, t);

            if (skyboxInstance != null && skyboxInstance.HasProperty("_Exposure"))
            {
                skyboxInstance.SetFloat("_Exposure", Mathf.Lerp(0.2f, 1f, t)); // 1f = 원래 밝기
            }


            timer += Time.deltaTime;
            yield return null;
        }

        RenderSettings.fog = false;

        if (directionalLight != null)
            directionalLight.gameObject.SetActive(true);

        SetAlpha(0f);
        
        GetRandomItem();
        
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
    
    //  Ultimate State
    public void EnterUltimateDarknessMode()
    {
        AudioManager.Instance.PlaySfx(AudioManager.Sfx.Darkness);
        
        RenderSettings.fog = true;
        RenderSettings.fogColor = Color.black;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.25f;

        if (directionalLight != null)
            directionalLight.gameObject.SetActive(false);

        if (skyboxInstance != null && skyboxInstance.HasProperty("_Exposure"))
        {
            skyboxInstance.SetFloat("_Exposure", 0.2f); // 어둡게
        }

    }


    private IEnumerator UltimateDarknessModeCoroutine()
    {
        EnterUltimateDarknessMode();

        // 3초간 어두운 상태 유지
        yield return new WaitForSeconds(3f);

        // 2초간 fogDensity + Skybox 색상 점점 줄이기
        float duration = 2f;
        float elapsed = 0f;
        float startDensity = RenderSettings.fogDensity;

        Color startSkyColor = darkSkyboxColor;
        Color endSkyColor = originalSkyboxColor;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Fog 밀도 감소
            RenderSettings.fogDensity = Mathf.Lerp(startDensity, 0f, t);

            // Skybox 색상 복원 (해당 속성 존재 시)
            if (skyboxInstance != null && skyboxInstance.HasProperty("_Exposure"))
            {
                skyboxInstance.SetFloat("_Exposure", Mathf.Lerp(0.2f, 1f, t)); // 1f = 원래 밝기
            }


            yield return null;
        }

        ExitUltimateDarknessMode();
        ultimateCoroutine = null;
    }



    public void ExitUltimateDarknessMode()
    {
        RenderSettings.fog = false;

        if (directionalLight != null)
            directionalLight.gameObject.SetActive(true);

        if (skyboxInstance != null && skyboxInstance.HasProperty("_Exposure"))
        {
            skyboxInstance.SetFloat("_Exposure", 1f);
        }

    }


    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            other.gameObject.GetComponent<Player>().GetDamage(attackPower);

            if (isUltimate)
            {
                EnterUltimateDarknessMode(); // 반드시 실행
                if (ultimateCoroutine != null) StopCoroutine(ultimateCoroutine);
                ultimateCoroutine = StartCoroutine(UltimateDarknessModeCoroutine());
            }

        }
    }
    

    private IEnumerator PlaySfxLoop()
    {
        while (true)
        {
            float waitTime = Random.Range(5f, 10f); // 5~10초 사이 랜덤
            yield return new WaitForSeconds(waitTime);

            float dist = Vector3.Distance(transform.position, playerTarget.transform.position);
            
            AudioManager.Instance.PlaySfx(AudioManager.Sfx.MonsterDefault, dist,50);
        }
    }

    private void GetRandomItem()
    {
        float rand = Random.Range(0f, 1f);

        if (rand <= 0.1f)
            Instantiate(healPackItem, transform.position + Vector3.up * 0.5f, Quaternion.identity);
        else if (rand <= 0.2f) 
            Instantiate(cellItem, transform.position + Vector3.up * 0.5f, Quaternion.identity);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, traceDist);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackDist);
    }
}
