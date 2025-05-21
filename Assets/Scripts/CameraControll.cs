using System.Collections;
using UnityEngine;
using DG.Tweening;
using Unity.Cinemachine;
using Unity.Mathematics.Geometry;
using Unity.VisualScripting;

public class CameraControll : MonoBehaviour
{
    public float sensX;
    public float sensY;

    public Transform orientation;
    public Transform cameraContainer;

    private float xRotation;
    private float yRotation;

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
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);  //  상하 제한

        // cameraContainer.rotation = Quaternion.Euler(xRotation, yRotation, 0);
        orientation.rotation = Quaternion.Euler(xRotation, yRotation, 0);
        
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
        Debug.Log("Change to " + curState);
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

    public bool isModified;
    private int lastMode = 0;
    
    public void DoFov(int mode)
    {
        if (lastMode != mode)
        {
            lastMode = mode;
            StopAllCoroutines();
            StartCoroutine(FovCoroutine(mode));
        }
    }

    //  mode - 0 : default, 1 : sprint, 2 : wallRun
    private IEnumerator FovCoroutine(int mode)
    {
        Vector2 offsetV = Vector2.zero;

        Debug.Log("Fov Mode : " + mode); 

        switch (mode)
        {
            case 0 :
                isModified = false;
                break;
            case 1 :
                offsetV =  new Vector2(0, sprintFovRange);
                isModified = true;
                break;
            case 2 :
                offsetV =  new Vector2(0, wallFovRange);
                isModified = true;
                break;
        }

        Vector2 distVec = new Vector2(defalutFovRange.x + offsetV.x, defalutFovRange.y + offsetV.y);

        while (Mathf.Abs(firstCam.GetComponent<CinemachineFollowZoom>().FovRange.magnitude - distVec.magnitude) > 0.1)
        {
            firstCam.GetComponent<CinemachineFollowZoom>().FovRange = 
                Vector2.Lerp(firstCam.GetComponent<CinemachineFollowZoom>().FovRange, distVec, 0.03f);
            yield return new WaitForSeconds(0.02f);
        }

        firstCam.GetComponent<CinemachineFollowZoom>().FovRange = distVec;
    }
    
    public void DoTilt(float zTilt)
    {
        transform.DOLocalRotate(new Vector3(0, 0, zTilt), 0.25f);
    }
}
