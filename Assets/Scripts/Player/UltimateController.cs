using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;

public class UltimateController : MonoBehaviour
{
    [Header("Ultimate Settings")]
    public GameObject ultimateCrossHairPrefab;    // 이 프리팹 루트에 Image 컴포넌트가 붙어 있다고 가정
    public RectTransform uiCanvasTransform;
    public LayerMask enemyLayer;
    public float fieldOfViewAngle = 80f;
    public float viewDistance = 50f;
    public float ultimateDuration = 15f;

    [Header("References")]
    public Player player;
    public GameObject normalCrossHair;

    private PlayerAttack playerAttack;
    private Camera mainCam;

    // 화면 위에 띄운 각 적별 크로스헤어(GameObject) 저장
    private Dictionary<Transform, GameObject> activeCrossHairs = new();

    // 크로스헤어의 기본(원래) 색상을 저장
    private Color defaultCrossHairColor;

    public bool IsUltimateActive => isUltimateActive;
    private bool isUltimateActive = false;
    private Coroutine ultimateRoutine;

    [Header("UIs")]
    public GameObject ultimateText;

    private bool ultimateUsable;

    private void Start()
    {
        var brain = CinemachineBrain.GetActiveBrain(0);
        mainCam = brain != null ? brain.OutputCamera : Camera.main;

        playerAttack = GetComponent<PlayerAttack>();

        // --- 프리팹 바로 위에 붙어 있는 Image 컴포넌트에서 기본 색상을 읽어옵니다. ---
        var prefabImage = ultimateCrossHairPrefab.GetComponent<Image>();
        defaultCrossHairColor = prefabImage.color;

        ultimateUsable = player.curUltimate >= player.maxUltimate;
    }

    private void Update()
    {
        if (player.curUltimate >= player.maxUltimate && !ultimateUsable)
        {
            ultimateUsable = true;
            ultimateText.GetComponent<Animator>().SetBool("ultimateReady", true);
            AudioManager.Instance.PlaySfx(AudioManager.Sfx.UltimateUsable);
        }

        if (Input.GetKeyDown(KeyCode.R) && !isUltimateActive && ultimateUsable)
        {
            ultimateText.GetComponent<Animator>().SetBool("ultimateReady", false);
            ultimateRoutine = StartCoroutine(UltimateRoutine());
        }

        if (isUltimateActive)
        {
            UpdateCrossHairsPosition();
            UpdateCrossHairColors(); // 색상 업데이트
        }
    }

    private IEnumerator UltimateRoutine()
    {
        AudioManager.Instance.PlaySfx(AudioManager.Sfx.PlayerUltimate);

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
        ultimateUsable = false;
    }

    private void TrackAllEnemiesInView()
    {
        Collider[] enemiesInRange = Physics.OverlapSphere(transform.position, viewDistance, enemyLayer);
        foreach (var enemy in enemiesInRange)
        {
            Transform enemyTransform = enemy.transform;

            // 이미 존재하면 새로 만들지 않음
            if (activeCrossHairs.ContainsKey(enemyTransform))
                continue;

            Vector3 dir = enemyTransform.position - mainCam.transform.position;
            float angle = Vector3.Angle(mainCam.transform.forward, dir);

            if (angle < fieldOfViewAngle / 2f)
            {
                Vector3 screenPos = mainCam.WorldToScreenPoint(enemyTransform.position);
                if (screenPos.z > 0)
                {
                    GameObject crossHair = Instantiate(ultimateCrossHairPrefab, uiCanvasTransform);
                    crossHair.GetComponent<RectTransform>().position = screenPos;
                    activeCrossHairs[enemyTransform] = crossHair;
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

    private void UpdateCrossHairColors()
    {
        Transform nearest = GetNearestEnemyInView();

        foreach (var kvp in activeCrossHairs)
        {
            var img = kvp.Value.GetComponent<Image>();
            if (img != null)
            {
                img.color = defaultCrossHairColor;
            }
        }

        if (nearest != null && activeCrossHairs.TryGetValue(nearest, out var greenCrossHair))
        {
            var img = greenCrossHair.GetComponent<Image>();
            if (img != null)
            {
                img.color = Color.green;
            }
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

            // 시야각 안에 있고 카메라 전방에 존재할 경우만 고려
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
