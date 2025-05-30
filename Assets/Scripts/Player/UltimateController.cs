using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

public class UltimateController : MonoBehaviour
{
    [Header("Ultimate Settings")]
    public GameObject ultimateCrossHairPrefab;
    public RectTransform uiCanvasTransform;
    public LayerMask enemyLayer;
    public float fieldOfViewAngle = 80f;
    public float viewDistance = 50f;
    public float ultimateDuration = 15f;

    [Header("References")]
    public Player player;
    public GameObject normalCrossHair;

    private PlayerAttack playerAttack; // 추가
    private Camera mainCam;
    private Dictionary<Transform, GameObject> activeCrossHairs = new();

    public bool IsUltimateActive => isUltimateActive;
    private bool isUltimateActive = false;
    private Coroutine ultimateRoutine;

    private void Start()
    {
        var brain = CinemachineBrain.GetActiveBrain(0);
        mainCam = brain != null ? brain.OutputCamera : Camera.main;

        playerAttack = GetComponent<PlayerAttack>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R) && !isUltimateActive && player.curUltimate >= player.maxUltimate)
        {
            ultimateRoutine = StartCoroutine(UltimateRoutine());
        }

        if (isUltimateActive)
        {
            UpdateCrossHairsPosition();
        }
    }

    private IEnumerator UltimateRoutine()
    {
        isUltimateActive = true;

        // Boost 최대치 고정
        player.curBoost = player.maxBoost;
        player.desireBoost = player.maxBoost;
        player.OnPlayerBoostChaged?.Invoke();

        player.allowBoostConsume = false;

        // 기존 CrossHair 숨김
        if (normalCrossHair != null) normalCrossHair.SetActive(false);
        if (playerAttack?.cr != null) playerAttack.cr.gameObject.SetActive(false);

        float duration = ultimateDuration;
        while (duration > 0f)
        {
            duration -= Time.deltaTime;

            // 잔여 궁극기 퍼센트 감소
            player.curUltimate = Mathf.Lerp(0f, player.maxUltimate, duration / ultimateDuration);
            player.OnPlayerUltimateChanged.Invoke();

            TrackAllEnemiesInView();
            yield return null;
        }

        // 종료 처리
        ClearCrossHairs();
        player.allowBoostConsume = true;

        if (normalCrossHair != null) normalCrossHair.SetActive(true);
        if (playerAttack?.cr != null) playerAttack.cr.gameObject.SetActive(true);

        player.curUltimate = 0f;
        player.OnPlayerUltimateChanged.Invoke();
        isUltimateActive = false;
    }

    private void TrackAllEnemiesInView()
    {
        ClearCrossHairs();

        Collider[] enemiesInRange = Physics.OverlapSphere(transform.position, viewDistance, enemyLayer);
        foreach (var enemy in enemiesInRange)
        {
            Vector3 dir = enemy.transform.position - mainCam.transform.position;
            float angle = Vector3.Angle(mainCam.transform.forward, dir);

            if (angle < fieldOfViewAngle / 2f)
            {
                Vector3 screenPos = mainCam.WorldToScreenPoint(enemy.transform.position);
                if (screenPos.z > 0)
                {
                    GameObject crossHair = Instantiate(ultimateCrossHairPrefab, uiCanvasTransform);
                    crossHair.GetComponent<RectTransform>().position = screenPos;
                    activeCrossHairs[enemy.transform] = crossHair;
                }
            }
        }
    }

    private void UpdateCrossHairsPosition()
    {
        List<Transform> toRemove = new();

        foreach (var pair in activeCrossHairs)
        {
            if (pair.Key == null)
            {
                Destroy(pair.Value);
                toRemove.Add(pair.Key);
                continue;
            }

            Vector3 screenPos = mainCam.WorldToScreenPoint(pair.Key.position);
            if (screenPos.z > 0)
            {
                pair.Value.GetComponent<RectTransform>().position = screenPos;
            }
            else
            {
                Destroy(pair.Value);
                toRemove.Add(pair.Key);
            }
        }

        foreach (var key in toRemove)
        {
            activeCrossHairs.Remove(key);
        }
    }

    public void OnMonsterDead(Transform monsterTransform)
    {
        if (activeCrossHairs.TryGetValue(monsterTransform, out var ch))
        {
            Destroy(ch);
            activeCrossHairs.Remove(monsterTransform);
        }
    }

    private void ClearCrossHairs()
    {
        foreach (var ch in activeCrossHairs.Values)
        {
            if (ch != null)
                Destroy(ch);
        }
        activeCrossHairs.Clear();
    }
    
    public Transform GetNearestEnemyInView()
    {
        Collider[] enemiesInRange = Physics.OverlapSphere(transform.position, viewDistance, enemyLayer);

        Transform nearest = null;
        float minDistance = Mathf.Infinity;

        foreach (var enemy in enemiesInRange)
        {
            Vector3 dirToEnemy = enemy.transform.position - mainCam.transform.position;
            float angle = Vector3.Angle(mainCam.transform.forward, dirToEnemy);

            // 시야각 안에 있고, 카메라 전방에 존재할 경우만 고려
            if (angle < fieldOfViewAngle / 2f)
            {
                Vector3 screenPos = mainCam.WorldToScreenPoint(enemy.transform.position);
                if (screenPos.z > 0)
                {
                    float dist = dirToEnemy.magnitude;
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        nearest = enemy.transform;
                    }
                }
            }
        }

        return nearest;
    }

}
