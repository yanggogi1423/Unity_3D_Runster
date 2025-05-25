using System;
using System.Collections;
using System.Collections.Generic;
using MimicSpace;
using UnityEngine;

public class DefaultMonster : MonoBehaviour
{
    
    [Header("Attributes")] 
    [SerializeField] private int maxHp;
    [SerializeField] private int curHp;
    public int attackPower = 10;

    [Header("For Animations")] public float fadeDuration = 2f;

    [Header("References")] [SerializeField]
    private List<Renderer> renderers = new List<Renderer>(); // 본체 렌더러들

    [SerializeField] private Mimic mimic; // Mimic 참조
    [SerializeField] private DefaultEnemyMovement de;
    
    [Header("Particles")]
    public GameObject spawnParticle;
    public float particleDuration = 3f;
    public bool isSpawning;

    private void Awake()
    {
        curHp = maxHp;

        if (mimic == null)
            mimic = GetComponent<Mimic>();
        
    }

    private void Start()
    {
        de = GetComponent<DefaultEnemyMovement>();
        
        StartSpawnEffect();
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

        if (curHp <= 0)
        {
            Debug.Log("Monster Die!");
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