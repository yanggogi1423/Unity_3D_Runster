using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

public class CameraControll : MonoBehaviour
{
    public float sensX;
    public float sensY;

    public Transform orientation;
    public Transform camOrientation;
    public Transform cameraContainer;

    [Header("Player")] 
    public PlayerMovement pm;

    private float xRotation;
    private float yRotation;
    
    private float curZTilt = 0f;

    [Header("Cameras")] 
    public GameObject firstCam;
    public GameObject thirdCam;
    
    public enum CameraState
    {
        First,
        Third
    }

    public CameraState curState;
    public KeyCode camChangeKey = KeyCode.F5;

    private void Start()
    {
        //  커서를 가운데에 고정
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        //  Set Default Cam
        curState = CameraState.First;
        firstCam.SetActive(true);
        thirdCam.SetActive(false);
    }

    private void Update()
    {
        //  Mouse Input
        float mouseX = Input.GetAxisRaw("Mouse X") * Time.deltaTime * sensX;
        float mouseY = Input.GetAxisRaw("Mouse Y") * Time.deltaTime * sensY;

        
        yRotation += mouseX;
        
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 45f);  //  상하 제한

        // cameraContainer.rotation = Quaternion.Euler(xRotation, yRotation, 0);
        
        camOrientation.rotation = Quaternion.Euler(xRotation, yRotation, curZTilt);
        orientation.rotation = Quaternion.Euler(xRotation, yRotation, 0);
        
        //  Player Rotation
        pm.PlayerRotation(yRotation);
        
        CheckInput();
    }

    public void CheckInput()
    {
        if (Input.GetKeyDown(camChangeKey))
        {
            if (curState == CameraState.First)
            {
                curState = CameraState.Third;
            }
            else if (curState == CameraState.Third)
            {
                curState = CameraState.First;
            }
            
            ChangeCamera();
        }
    }

    private void ChangeCamera()
    {
        switch (curState)
        {
            case CameraState.First :
                thirdCam.SetActive(false);
                firstCam.SetActive(true);
                break;
            case CameraState.Third :
                firstCam.SetActive(false);
                thirdCam.SetActive(true);
                break;
        }
    }

    
    private Vector2 defalutFovRange = new Vector2(1f, 58f);
    
    [Header("Fov")]
    [Range(0f,40f)]
    public float wallFovRange = 30f;

    [Range(0f, 20f)] 
    public float sprintFovRange = 15f;

    public bool isFovModified;
    private int lastMode = 0;
    
    private Coroutine fovRoutine = null;

    public void DoFov(int mode)
    {
        if (lastMode != mode || fovRoutine == null)
        {
            lastMode = mode;

            if (fovRoutine != null)
                StopCoroutine(fovRoutine);

            fovRoutine = StartCoroutine(FovCoroutine(mode));
        }
    }


    //  mode - 0 : default, 1 : sprint, 2 : wallRun
    private IEnumerator FovCoroutine(int mode)
    {

        Vector2 offsetV = Vector2.zero;

        switch (mode)
        {
            case 0:
                isFovModified = false;
                break;
            case 1:
                offsetV = new Vector2(0, sprintFovRange);
                isFovModified = true;
                break;
            case 2:
                offsetV = new Vector2(0, wallFovRange);
                isFovModified = true;
                break;
        }

        Vector2 destVec = new Vector2(defalutFovRange.x + offsetV.x, defalutFovRange.y + offsetV.y);

        var zoom = firstCam.GetComponent<CinemachineFollowZoom>();
    
        while (Vector2.Distance(zoom.FovRange, destVec) > 0.1f)
        {
            zoom.FovRange = Vector2.Lerp(zoom.FovRange, destVec, 0.03f);
            yield return new WaitForSeconds(0.02f);
        }

        zoom.FovRange = destVec;
        
        fovRoutine = null;
    }
    
    
    private Coroutine tiltCoroutine = null;

    public void DoTilt(float zTilt)
    {
        curZTilt = zTilt;

        // 기존 코루틴이 실행 중이면 멈춤
        if (tiltCoroutine != null)
        {
            StopCoroutine(tiltCoroutine);
        }

        // 새 코루틴 시작
        tiltCoroutine = StartCoroutine(TiltCoroutine(zTilt));
    }

    private IEnumerator TiltCoroutine(float zTilt)
    {
        float t = 0f;
        float duration = 0.25f;
        

        float startZ = camOrientation.rotation.eulerAngles.z;
        float targetZ = zTilt;

        while (t < duration)
        {
            t += Time.deltaTime;
            float progress = t / duration;

            float z = Mathf.LerpAngle(startZ, targetZ, progress);
            camOrientation.rotation = Quaternion.Euler(xRotation, yRotation, z);

            yield return null;
        }

        camOrientation.rotation = Quaternion.Euler(xRotation, yRotation, targetZ);

        // 완료 후 코루틴 핸들러 정리
        tiltCoroutine = null;
    }

    

}
