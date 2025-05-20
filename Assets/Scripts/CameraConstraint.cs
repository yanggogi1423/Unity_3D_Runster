using UnityEngine;

public class CameraConstraint : MonoBehaviour
{
    [Header("Essential References")]
    public Transform playerBody;
    public Transform cameraPivot;
    public Transform cameraObj;

    [Header("Collision")]
    public float cameraDistance = 0.3f;
    public float cameraRadius = 0.15f;
    public LayerMask wallLayerMask;

    [Header("Smooth Movement")]
    public float smoothSpeed = 20f;

    private Vector3 currentVelocity;

    void LateUpdate()
    {
        // 카메라 기준점과 목표 지점 계산
        Vector3 start = cameraPivot.position;
        Vector3 dir = (cameraObj.position - start).normalized;
        Vector3 targetPos = start + dir * cameraDistance;

        // SphereCast 감지
        RaycastHit hit;
        bool isHit = Physics.SphereCast(
            start,
            cameraRadius,
            dir,
            out hit,
            cameraDistance,
            wallLayerMask
        );

        Vector3 finalPos = targetPos;

        // 충돌 시 거리 보정 (최소값 보장)
        if (isHit)
        {
            float clampedDist = Mathf.Max(hit.distance - 0.01f, 0.05f);
            finalPos = start + dir * clampedDist;
        }

        // 부드럽게 위치 적용
        cameraObj.position = Vector3.SmoothDamp(cameraObj.position, finalPos, ref currentVelocity, 1f / smoothSpeed);
    }
}