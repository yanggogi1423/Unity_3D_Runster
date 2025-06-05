using System.Collections;
using UnityEngine;

public class Climbing : MonoBehaviour
{
    [Header("References")]
    public Transform orientation;
    private Rigidbody rb;
    private PlayerMovement pm;
    public LayerMask wallLayer;

    [Header("Climbing")]
    public float climbSpeed;
    public float maxClimbTime;
    private float climbTimer;

    [Header("ClimbJumping")] public float climbJumpForce;
    public float climbJumpBackForce;

    public KeyCode jumpKey = KeyCode.Space;
    public int climbJumps;
    public int climbJumpsLeft;
    

    [Header("Detection")] 
    public float detectionLength;
    public float sphereCastRadius;
    public float maxWallLookAngle;
    private float wallLookAngle;

    private RaycastHit frontWallHit;
    private bool wallFront;

    private Transform lastWall;
    private Vector3 lastWallNormal;
    public float minWallNormalAngleChange;

    [Header("Exiting")] 
    public bool exitingWall;

    public float exitWallTime;
    private float exitWallTimer;
    
    //  UI
    private float climbVisualTimer;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        pm = GetComponent<PlayerMovement>();
    }

    private void Update()
    {
        WallCheck();
        CheckInput();
        
        if(pm.climbing && !exitingWall) ClimbingMovement();
        
        if (pm.climbing)
        {
            climbVisualTimer = Mathf.MoveTowards(
                climbVisualTimer,
                climbTimer,
                Time.deltaTime * 10f
            );
        }
        else
        {
            climbVisualTimer = Mathf.MoveTowards(
                climbVisualTimer,
                maxClimbTime,
                Time.deltaTime * 15f
            );
        }
    }

    private void CheckInput()
    {
        if (pm.player.tm != null)
        {
            if (pm.player.tm.curState < TutorialManager.State.Climb && !pm.player.tm.isShowingText)
            {
                return;
            }
        }
        
        if (wallFront && Input.GetKey(KeyCode.W) && wallLookAngle < maxWallLookAngle && !exitingWall)
        {
            if (!pm.climbing && climbTimer > 0)
            {
                StartClimbing();
            }
            
            //  Timer
            if (climbTimer > 0) climbTimer -= Time.deltaTime;
            else StopClimbing();
        }
        
        //  Exiting
        else if (exitingWall)
        {
            if(pm.climbing) StopClimbing();

            if (exitWallTimer > 0) exitWallTimer -= Time.deltaTime;
            else exitingWall = false;
        }

        else
        {
            if(pm.climbing) StopClimbing();
        }
        
        //  Climb Jump
        if (wallFront && Input.GetKeyDown(jumpKey) && climbJumpsLeft > 0) ClimbJump();
    }

    private void WallCheck()
    {
        wallFront = Physics.SphereCast(transform.position, sphereCastRadius, orientation.forward, out frontWallHit,
            detectionLength, wallLayer);

        wallLookAngle = Vector3.Angle(orientation.forward, -frontWallHit.normal);

        bool newWall = frontWallHit.transform != lastWall ||
                       Mathf.Abs(Vector3.Angle(lastWallNormal, frontWallHit.normal)) > minWallNormalAngleChange;
        
        
        //  Reset Climb Timer
        if ((wallFront && newWall) || pm.isGrounded)
        {
            climbTimer = maxClimbTime;
            climbJumpsLeft = climbJumps;
        }
    }

    private Coroutine climbCoroutine = null;

    private void StartClimbing()
    {
        pm.climbing = true;
        
        pm.anim.SetBool("isClimbing", true);
        
        //  계속해서 climbing하는 현상을 제거
        pm.anim.SetBool("isIdle", true);

        lastWall = frontWallHit.transform;
        lastWallNormal = frontWallHit.normal;
        
        
    }

    private IEnumerator ForTutorialCoroutine()
    {
        yield return new WaitForSeconds(5f);
        StartCoroutine(pm.player.tm.BuffNextState());
    }

    private void ClimbingMovement()
    {
        //  Climb는 AddForce를 사용하지 않아도 자연스럽게 구현 가능하다.
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, climbSpeed, rb.linearVelocity.z);
    }

    private void StopClimbing()
    {
        pm.climbing = false;
        pm.anim.SetBool("isClimbing", false);

        if (pm.player.tm != null)
        {
            if (pm.player.isTutorial && pm.player.tm.curState == TutorialManager.State.Climb 
                                     && pm.player.tm.climbChecker  && !pm.player.tm.isShowingText && climbCoroutine == null)
            {
                climbCoroutine = StartCoroutine(ForTutorialCoroutine());
            }
        }
        
    }

    private void ClimbJump()
    {
        AudioManager.Instance.PlaySfx(AudioManager.Sfx.PlayerJump);
        
        exitingWall = true;
        exitWallTimer = exitWallTime;

        pm.anim.SetTrigger("jumping");
        
        Vector3 forceToApply = transform.up * climbJumpForce + frontWallHit.normal * climbJumpBackForce;

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce(forceToApply, ForceMode.Impulse);

        climbJumpsLeft--;
    }

    public float GetClimbTimeRatio()
    {
        return Mathf.Clamp01(climbVisualTimer / maxClimbTime);
    }
}
