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
        Debug.Log("Slide Start");
        
        pm.anim.SetBool("isSliding", true);
        
        pm.sliding = true;

        // player.localScale = new Vector3(player.localScale.x, slideYScale, player.localScale.z);
        rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);

        slideTimer = maxSlideTime;
    }

    private void SlidingMovement()
    {
        //  입력 들어온 방향을 계산
        Vector3 inputDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;
        
        //  Sliding Normal
        if (!pm.OnSlope() || rb.linearVelocity.y > -0.1f)
        {
            rb.AddForce(inputDirection.normalized * slideForce, ForceMode.Force);

            slideTimer -= Time.deltaTime;
        }

        //  Sliding down a slope - Timer 줄어들지 않음 
        else
        {
            rb.AddForce(pm.GetSlopeMoveDirection(inputDirection) * slideForce, ForceMode.Force);
        }

        if (slideTimer <= 0)
        {
            StopSlide();
        }
    }

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
