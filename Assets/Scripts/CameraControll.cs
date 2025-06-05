using System.Collections;
using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.UI;

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
    public GameObject clearCam;
    public GameObject dieCam;

    public GameObject curCam;

    [Header("UI")] 
    public UIManager uiManager;

    [Header("Cursor")] 
    public GameObject back;
    public KeyCode cursorVisibleKey = KeyCode.Escape;
    [SerializeField] private bool cursorVisibility;

    [Header("Slider")] public Slider sensitivitySlider;
    
    public enum CameraState
    {
        First,
        Third
    }

    public CameraState curState;
    public KeyCode camChangeKey = KeyCode.T;
    
    [Header("Cinemachine")]
    CinemachineBrain brain;

    private void Start()
    {
        // brain = CinemachineBrain.GetActiveBrain(0);
        // if (brain != null)
        // {
        //     brain.UpdateMethod = CinemachineBrain.UpdateMethods.ManualUpdate;
        // }
        
        cursorVisibility = false;
        SetCursorVisible();
        
        //  Set Default Cam
        curState = CameraState.First;
        firstCam.SetActive(true);
        thirdCam.SetActive(false);

        sensX = GameManager.Instance.curX;
        sensY = GameManager.Instance.curY;
    }

    private void Update()
    {
        if (GameManager.Instance.isClear)
        {
            firstCam.SetActive(false);
            thirdCam.SetActive(false);
            clearCam.SetActive(true);
            return;
        }

        if (pm.GetComponent<Player>().isDead)
        {
            firstCam.SetActive(false);
            thirdCam.SetActive(false);
            dieCam.SetActive(true);
        }

        // cameraContainer.rotation = Quaternion.Euler(xRotation, yRotation, 0);
        if (!cursorVisibility)
        {
            //  Mouse Input
            float mouseX = Input.GetAxisRaw("Mouse X") * sensX;
            float mouseY = Input.GetAxisRaw("Mouse Y") * sensY;
        
            yRotation += mouseX;
        
            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -90f, 45f);  //  상하 제한
            
            camOrientation.rotation = Quaternion.Euler(xRotation, yRotation, curZTilt);
            orientation.rotation = Quaternion.Euler(xRotation, yRotation, 0);
            //  Player Rotation
            pm.PlayerRotation(yRotation);
        }
        
        
        CheckInput();
        
        //  Cursor visibility는 여기서 관리
        if (Input.GetKeyDown(cursorVisibleKey))
        {
            cursorVisibility = !cursorVisibility;
            pm.player.isPause = cursorVisibility;
            
            AudioManager.Instance.StopAllLoopingSfx();
            
            SetCursorVisible();
            
        }
    }
    
    
    // private void LateUpdate()
    // {
    //     // timeScale이 0일 때도 blending 작동하게 만듦
    //     if (brain != null)
    //     {
    //         brain.ManualUpdate();
    //     }
    // }

    public void ExitMenu()
    {
        cursorVisibility = !cursorVisibility;
        SetCursorVisible();
    }

    public void SetCursorVisible()
    {
        if (cursorVisibility)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = cursorVisibility;
            back.SetActive(cursorVisibility);
            uiManager.TopMenuToggle();
            
            Time.timeScale = 0f;

            sensitivitySlider.value = (GameManager.Instance.curX / GameManager.Instance.originX);
        }
        else
        {
            Time.timeScale = 1f;
            
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = cursorVisibility;
            back.SetActive(cursorVisibility);
            uiManager.TopMenuToggle();
            
            
        }
    }

    public void ControlBySwitchButton()
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

            if (pm.player.tm != null)
            {
                if (pm.player.isTutorial && pm.player.tm.curState == TutorialManager.State.FirstThird && !pm.player.tm.isShowingText)
                {
                    StartCoroutine(pm.player.tm.BuffNextState());
                }
            }
            
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

    public void OnChangeSensitivity()
    {
        GameManager.Instance.curX = sensitivitySlider.value * GameManager.Instance.originX;
        GameManager.Instance.curY = sensitivitySlider.value * GameManager.Instance.originY;

        sensX = GameManager.Instance.curX;
        sensY = GameManager.Instance.curY;
    }

}
