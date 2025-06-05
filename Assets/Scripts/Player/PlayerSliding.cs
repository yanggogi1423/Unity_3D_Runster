using UnityEngine;

public class PlayerSliding : MonoBehaviour
{
    [Header("References")] 
    public Transform orientation;
    public Transform player;
    private Rigidbody rb;
    private PlayerMovement pm;

    [Header("Sliding")] public float maxSlideTime;
    public float slideForce;
    private float slideTimer;

    public float slideYScale;
    private float startYScale;

    [Header("Input")] public KeyCode slideKey = KeyCode.LeftControl;
    private float horizontalInput;
    private float verticalInput;
    
    //  UI
    private float slideVisualTimer;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        pm = GetComponent<PlayerMovement>();

        startYScale = player.localScale.y;
    }

    private void Update()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        if (Input.GetKeyDown(slideKey) && (horizontalInput != 0 || verticalInput != 0) && !pm.sliding)
        {
            StartSliding();

            if (pm.player.tm != null)
            {
                if (pm.player.isTutorial && pm.player.tm.curState == TutorialManager.State.Slide && !pm.player.tm.isShowingText)
                {
                    StartCoroutine(pm.player.tm.BuffNextState());
                }
            }
        }

        if (Input.GetKeyUp(slideKey) && pm.sliding)
        {
            StopSlide();
        }
        
        // UI용 보간 처리
        if (pm.sliding)
        {
            slideVisualTimer = Mathf.MoveTowards(
                slideVisualTimer,
                slideTimer,
                Time.deltaTime * 10f
            );
        }
        else
        {
            slideVisualTimer = Mathf.MoveTowards(
                slideVisualTimer,
                maxSlideTime,
                Time.deltaTime * 15f
            );
        }

    }

    private void FixedUpdate()
    {
        if (pm.sliding)
        {
            SlidingMovement();
        }
    }

    private void StartSliding()
    {
        pm.anim.SetBool("isSliding", true);
        
        pm.sliding = true;

        // player.localScale = new Vector3(player.localScale.x, slideYScale, player.localScale.z);
        rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);

        slideTimer = maxSlideTime;
    }
    
    private void SlidingMovement()
    {
        // (1) 입력에 따라 얻은 orientation.forward/ right → 수평으로만 뽑기
        Vector3 flatForward = new Vector3(orientation.forward.x, 0f, orientation.forward.z).normalized;
        Vector3 flatRight   = new Vector3(orientation.right.x,   0f, orientation.right.z).normalized;
    
        // (2) 키 입력 방향을 수평 벡터들로 결합
        Vector3 inputDirection = flatForward * verticalInput + flatRight * horizontalInput;

        // (3) 경사면 위가 아닐 때(또는 위로 솟구치지 않을 때)는 평지 슬라이딩
        if (!pm.OnSlope() || rb.linearVelocity.y > -0.1f)
        {
            rb.AddForce(inputDirection.normalized * slideForce, ForceMode.Force);
            slideTimer -= Time.deltaTime;
        }
        // (4) 경사면 아래로 미끄러질 때는 slope 속도 방향으로
        else
        {
            Vector3 slopeDir = pm.GetSlopeMoveDirection(inputDirection);
            rb.AddForce(slopeDir * slideForce, ForceMode.Force);
            // 이 때는 slideTimer를 줄이지 않으므로 따로 처리하지 않음
        }

        // (5) 시간이 다 되면 슬라이딩 종료
        if (slideTimer <= 0f)
        {
            StopSlide();
        }
    }


    // private void SlidingMovement()
    // {
    //     //  입력 들어온 방향을 계산
    //     Vector3 inputDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;
    //     
    //     //  Sliding Normal
    //     if (!pm.OnSlope() || rb.linearVelocity.y > -0.1f)
    //     {
    //         rb.AddForce(inputDirection.normalized * slideForce, ForceMode.Force);
    //
    //         slideTimer -= Time.deltaTime;
    //     }
    //
    //     //  Sliding down a slope - Timer 줄어들지 않음 
    //     else
    //     {
    //         rb.AddForce(pm.GetSlopeMoveDirection(inputDirection) * slideForce, ForceMode.Force);
    //     }
    //
    //     if (slideTimer <= 0)
    //     {
    //         StopSlide();
    //     }
    // }

    private void StopSlide()
    {
        pm.sliding = false;
        
        pm.anim.SetBool("isSliding", false);
        
        // player.localScale = new Vector3(player.localScale.x, startYScale, player.localScale.z);
    }

    public float GetSlideTimeRatio()
    {
        return Mathf.Clamp01(slideVisualTimer / maxSlideTime);
    }

}
