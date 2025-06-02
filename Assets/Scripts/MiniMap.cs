using UnityEngine;

public class MiniMap : MonoBehaviour
{
    public RectTransform minimapArrow;       // 미니맵 위에 있는 화살표 이미지
    public Transform playerTransform;        // 플레이어 월드 위치
    public Transform minimapCamera;          // 미니맵용 카메라 (보통 top-down)
    public LayerMask enemyLayer;
    public float detectionRadius = 100f;     // 탐색 범위
    
    void Update()
    {
        UpdateMinimapArrow();
    }

    
    private Transform GetNearestEnemy()
    {
        Collider[] hits = Physics.OverlapSphere(playerTransform.position, detectionRadius, enemyLayer);

        Transform nearest = null;
        float minDist = Mathf.Infinity;

        foreach (var hit in hits)
        {
            float dist = Vector3.Distance(playerTransform.position, hit.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = hit.transform;
            }
        }

        return nearest;
    }

    private void UpdateMinimapArrow()
    {
        Transform nearestEnemy = GetNearestEnemy();
        if (nearestEnemy == null)
        {
            minimapArrow.gameObject.SetActive(false);  // 적이 없으면 비활성화
            return;
        }

        minimapArrow.gameObject.SetActive(true);

        // 방향 계산 (플레이어 → 적)
        Vector3 dir = nearestEnemy.position - playerTransform.position;

        // y축은 무시하고 평면상의 방향으로 계산
        dir.y = 0;
        dir.Normalize();

        // 미니맵은 카메라의 로컬 기준으로 표시 → 회전 변환 필요
        Vector3 localDir = minimapCamera.InverseTransformDirection(dir);

        // 화살표의 로컬 회전 설정
        float angle = Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg;
        minimapArrow.localRotation = Quaternion.Euler(0, 0, -angle);  // z축 회전
    }


}
