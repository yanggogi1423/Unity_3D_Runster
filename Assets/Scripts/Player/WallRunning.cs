using System;
using System.Collections;
using UnityEngine;

public class WallRunning : MonoBehaviour
{
    [Header("For Check Wall")] 
    public LayerMask wallLayer;
    public LayerMask groundLayer;
    public float wallRunForce;
    public float wallJumpUpForce;
    public float wallJumpSideForce;
    public float wallClimbSpeed;
    public float maxWallRunTime;
    private float wallRunTimer;

    [Header("Input")] 
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode upwardsRunKey = KeyCode.LeftShift;
    public KeyCode downwardsRunKey = KeyCode.LeftControl;
    private bool upwardsRunning;
    private bool downwardsRunning;
    private float horizontalInput;
    private float verticalInput;

    [Header("Detection")] 
    public float wallCheckDistance;
    public float minJumpHeight;
    private RaycastHit leftWallHit;
    private RaycastHit rightWallHit;
    private bool wallLeft;
    private bool wallRight;

    [Header("Exiting")] 
    private bool exitingWall;
    public float exitWallTime;
    private float exitWallTimer;

    [Header("Gravity")] 
    public bool useGravity;
    public float gravityCounterForce;

    [Header("References")]
    public Transform orientation;

    public CameraControll cam;
    
    private PlayerMovement pm;
    private Rigidbody rb;
    public CapsuleCollider cc;
    
    //  UI
    private float wallRunCooldownVisual;
    
    [Header("UI Refill Speed")]
    public float wallRunRefillSpeed = 2f;  // 1초면 2만큼 찬다고 가정
    
    private float wallLostTime = 0f;
    private float wallLostGraceTime = 0.15f;
    
    // [Header("Camera Offset")]
    // public Transform cameraContainer;             // CameraContainer (PlayerCam의 부모)
    // public Vector3 defaultCamLocalPos;            // 원래 로컬 위치 저장용
    // public float camOffsetDistance = 0.3f;         // 벽 반대쪽으로 이동할 거리
    // public float camLerpSpeed = 5f;                // 이동 속도
    // public Transform cameraPos;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        pm = GetComponent<PlayerMovement>();
        
        // // 카메라의 기본 로컬 위치 저장
        // if (cameraContainer != null)
        // {
        //     defaultCamLocalPos = cameraContainer.localPosition;
        // }

        wallRunCooldownVisual = maxWallRunTime;
    }

    private void Update()
    {
        CheckInput();
        CheckForWall();
        
        if (pm.wallRunning)
        {
            cam.DoFov(2);

            wallRunCooldownVisual = Mathf.MoveTowards(
                wallRunCooldownVisual,
                wallRunTimer,
                Time.deltaTime * 10f
            );
        }
        else
        {
            wallRunCooldownVisual = Mathf.MoveTowards(
                wallRunCooldownVisual,
                maxWallRunTime,
                Time.deltaTime * 15f
            );
        }
    }

    private void FixedUpdate()
    {
        if (pm.wallRunning)
        {
            WallRunningMovement();
        }
    }

    private void CheckForWall()
    {
        wallRight = Physics.SphereCast(transform.position, 0.3f, orientation.right, out rightWallHit, wallCheckDistance, wallLayer);
        wallLeft  = Physics.SphereCast(transform.position, 0.3f, -orientation.right, out leftWallHit, wallCheckDistance, wallLayer);
    }

    private bool AboveGround()
    {
        return !Physics.Raycast(transform.position, Vector3.down, minJumpHeight, groundLayer);
    }

    private void CheckInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        upwardsRunning = Input.GetKey(upwardsRunKey);
        downwardsRunning = Input.GetKey(downwardsRunKey);

        bool isWallDetected = wallLeft || wallRight;
        bool isTryingToRun = verticalInput > 0 && AboveGround() && !exitingWall;

        if (isWallDetected && isTryingToRun)
        {
            wallLostTime = 0f; // 벽 있음: 리셋

            if (!pm.wallRunning)
            {
                StartWallRunning();
            }

            wallRunTimer -= Time.deltaTime;
            if (wallRunTimer <= 0f && pm.wallRunning)
            {
                exitingWall = true;
                exitWallTimer = exitWallTime;
            }

            if (Input.GetKey(jumpKey))
            {
                WallJump();
            }
        }
        else if (!exitingWall)
        {
            // 벽이 없어진 상태: 유예 시간 증가
            wallLostTime += Time.deltaTime;
            if (wallLostTime > wallLostGraceTime && pm.wallRunning)
            {
                StopWallRun();
            }
        }
        else if (exitingWall)
        {
            if (pm.wallRunning) StopWallRun();

            exitWallTimer -= Time.deltaTime;
            if (exitWallTimer <= 0f) exitingWall = false;
        }
    }

    private void StartWallRunning()
    {
        pm.wallRunning = true;
        
        if(wallRight)
            pm.anim.SetBool("isRightWall", true);
        else
            pm.anim.SetBool("isLeftWall", true);
        
        //  For Animation
        cc.radius = 0.59f;

        wallRunTimer = maxWallRunTime;
        
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        
        //  Camera Effects
        cam.DoFov(2);
        
        if(wallLeft) cam.DoTilt(-7f);
        if(wallRight) cam.DoTilt(7f);
    }

    private void WallRunningMovement()
    {
        rb.useGravity = useGravity;

        Vector3 wallNormal = wallRight ? rightWallHit.normal : leftWallHit.normal;
        Vector3 wallForward = Vector3.Cross(wallNormal, transform.up);

        if ((orientation.forward - wallForward).magnitude > (orientation.forward - (-wallForward)).magnitude)
        {
            wallForward = -wallForward;
        }

        rb.AddForce(wallForward * wallRunForce, ForceMode.Force);

        if (upwardsRunning)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, wallClimbSpeed, rb.linearVelocity.z);
        }
        else if (downwardsRunning)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, -wallClimbSpeed, rb.linearVelocity.z);
        }
        else
        {
            //  Y속도 점진 감속
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f, rb.linearVelocity.z);
        }

        if (!(wallLeft && horizontalInput > 0) && !(wallRight && horizontalInput < 0))
        {
            rb.AddForce(-wallNormal * 100f, ForceMode.Force);
        }

        if (useGravity)
        {
            rb.AddForce(transform.up * gravityCounterForce, ForceMode.Force);
        }
    }


    private void StopWallRun()
    {
        pm.wallRunning = false;
        rb.useGravity = useGravity;

        cc.radius = 0.5f;

        pm.anim.SetBool("isRightWall", false);
        pm.anim.SetBool("isLeftWall", false);

        // Reset Camera Effects
        cam.DoFov(0);
        cam.DoTilt(0f);

        StartCoroutine(ForTutorialCoroutine());
    }

    private IEnumerator ForTutorialCoroutine()
    {
        yield return new WaitForSeconds(7f);
        if (pm.player.isTutorial && pm.player.tm.curState == TutorialManager.State.WallRun)
        {
            StartCoroutine(pm.player.tm.BuffNextState());
        }
    }


    private void WallJump()
    {
        AudioManager.Instance.PlaySfx(AudioManager.Sfx.PlayerJump);
        
        //  Jump Animation
        pm.anim.SetTrigger("jumping");

        exitingWall = true;
        exitWallTimer = exitWallTime;
        
        Vector3 wallNormal = wallRight ? rightWallHit.normal : leftWallHit.normal;

        Vector3 jumpForceVec = Vector3.up * Mathf.Sqrt(wallJumpUpForce * -Physics.gravity.y) + wallNormal * wallJumpSideForce;
        
        //  Before Add force, Reset Y velocity
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        
        //  Add Force
        rb.AddForce(jumpForceVec, ForceMode.Impulse);
    }
    
    //  For UI
    public float GetWallRunTimeRatio()
    {
        return Mathf.Clamp01(wallRunCooldownVisual / maxWallRunTime);
    }
    
    // private void UpdateCameraOffset()
    // {
    //     if (cameraContainer == null || cameraPos == null)
    //     {
    //         return;
    //     }
    //
    //     Vector3 offset = Vector3.zero;
    //
    //     if (pm.wallRunning && wallLeft)
    //     {
    //         offset += orientation.right * camOffsetDistance;
    //     }
    //     else if (pm.wallRunning && wallRight)
    //     {
    //         offset += -orientation.right * camOffsetDistance;
    //     }
    //
    //     Vector3 targetPos = cameraPos.position + offset;
    //
    //     cameraContainer.position = Vector3.Lerp(
    //         cameraContainer.position,
    //         targetPos,
    //         Time.deltaTime * camLerpSpeed
    //     );
    // }



}
