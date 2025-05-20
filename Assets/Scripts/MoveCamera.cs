using UnityEngine;

public class MoveCamera : MonoBehaviour
{
    public Transform cameraPosition;

    public PlayerMovement pm;

    public float followSpeed = 5f;

    private void Update()
    {
        if (!pm.wallRunning)
        {
            // 자연스럽게 CameraPos 위치로 이동
            transform.position = Vector3.Lerp(
                transform.position,
                cameraPosition.position,
                Time.deltaTime * followSpeed
            );
        }
    }
}
