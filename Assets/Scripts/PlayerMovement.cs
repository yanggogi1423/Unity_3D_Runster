using System;
using System.Numerics;
using NUnit.Framework.Internal.Filters;
using TMPro;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using Vector3 = UnityEngine.Vector3;

public class PlayerMovement : MonoBehaviour
{
    [Header("For Debug")]
    public TMP_Text speedText;
    
    [Header("Movement")] 
    private float moveSpeed;

    public float walkSpeed;
    public float sprintSpeed;

    public float groundDrag;
    
    [Header("Jumping")]
    public float jumpForce;
    public float jumpCooldown;
    public float airMultiplier;
    private bool readyToJump;

    [Header("Crouching")] public float crouchSpeed;
    public float crouchYScale;
    private float startYScale;

    [Header("Keys")] 
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode sprintKey = KeyCode.LeftShift;
    //  사용할 지는 미정
    public KeyCode crouchKey = KeyCode.LeftControl;

    [Header("Ground Checking")] 
    public float playerHeight;
    public LayerMask groundLayer;
    private bool isGrounded;

    [Header("Slope Checking")] //   Slope는 Ground Check했던 것보다 더 깊이 하단을 확인해야 한다. 0.3f
    public float maxSlopeAngle;
    private RaycastHit slopeHit;
    private bool exitingSlope;  //  Slope에서 점프를 지원할 수 있게!
    private bool isOnSlope;

    //  Orientation은 바라보는 방향을 저장한다.
    public Transform orientation;

    //  Member로 존재해야 한다.
    private float horizontalInput;
    private float verticalInput;

    private Vector3 moveDirection;

    private Rigidbody rb;
    
    //  For State Machine
    public enum MovementState
    {
        Walk,
        Sprinting,
        Crouching,
        Air
    }
    
    public MovementState curState;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        readyToJump = true;

        startYScale = transform.localScale.y;
    }

    private void Update()
    {
        //  Player Height의 절반 + 0.2(offset)
        isGrounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f, groundLayer);
        
        CheckInput();
        SpeedControl();
        StateHandler();

        isOnSlope = OnSlope();
        
        //  Handle Drag
        if (isGrounded) rb.linearDamping = groundDrag;
        else rb.linearDamping = 0;

    }

    private void FixedUpdate()
    {
        MovePlayer();
        
        //  For Debug
        speedText.SetText(rb.linearVelocity.magnitude + "");
    }

    private void CheckInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");
        
        //  Checking Jump
        if (Input.GetKey(jumpKey) && readyToJump && (isGrounded || isOnSlope))
        {
            readyToJump = false;

            Jump();
            
            //  nameof를 통해 함수 이름을 string으로 반환 가능 - 일정 시간 후 Jump 쿨타임 해제
            Invoke(nameof(ResetJump), jumpCooldown);
        }
        
        //  Start Crouch
        if (Input.GetKeyDown(crouchKey))
        {
            transform.localScale = new Vector3(transform.localScale.x, crouchYScale, transform.localScale.z);
            //  pivot을 중심으로 scale 축소되기에 floating되는 부분을 해결해야 한다.
            rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);
        }
        
        //  Stop Crouch
        if (Input.GetKeyUp(crouchKey))
        {
            transform.localScale = new Vector3(transform.localScale.x, startYScale, transform.localScale.z);
        }
    }

    private void StateHandler()
    {
        //  Crouching
        if (Input.GetKey(crouchKey))
        {
            curState = MovementState.Crouching;
            moveSpeed = crouchSpeed;
        }
        
        //  Sprinting
        else if (isGrounded && Input.GetKey(sprintKey))
        {
            curState = MovementState.Sprinting;
            moveSpeed = sprintSpeed;
        }
        else if (isGrounded)    //  Walk
        {
            curState = MovementState.Walk;
            moveSpeed = walkSpeed;
        }
        else    //  Air
        {
            curState = MovementState.Air;
        }        
    }
    

    //  NOTE : Run in Fixed Update
    private void MovePlayer()
    {
        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;
        
        //  On Slope
        if (isOnSlope && !exitingSlope)
        {
            Debug.Log("Slope!");
            rb.AddForce(GetSlopeMoveDirection() * moveSpeed * 20f, ForceMode.Force);

            //  너무 위로 가면 안되니깐 Slope에서 멈출 수 있도록 한다. (아래로 가는 힘을 가함)
            if (rb.linearVelocity.y > 0)
            {
                rb.AddForce(Vector3.down * 80f, ForceMode.Force);
            }
        }
        
        else if(isGrounded)  //  땅에 닿았을때 평면 이동
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);
        else if(!isGrounded)    //  공기 저항
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f * airMultiplier, ForceMode.Force);
        
        //  Slope 위에 있을 때 미끄러지지 않도록
        rb.useGravity = !OnSlope();
    }

    //  Max Speed를 제한
    private void SpeedControl()
    {
        //  평면에서 가는 거리와 동일하게 갈 수 있도록 제한한다.
        if (isOnSlope && !exitingSlope)
        {
            if (rb.linearVelocity.magnitude > moveSpeed)
            {
                rb.linearVelocity = rb.linearVelocity.normalized * moveSpeed;
            }
        }
        else
        {
            //  평면에서의 속도
            Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

            if (flatVel.magnitude > moveSpeed)
            {
                Vector3 limitedVel = flatVel.normalized * moveSpeed;
                rb.linearVelocity = new Vector3(limitedVel.x, rb.linearVelocity.y, limitedVel.z);
            }
        }
    }

    private void Jump()
    {
        exitingSlope = true;
        
        //  Before Jump Need to Reset y velocity
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        
        rb.AddForce(transform.up * jumpForce,ForceMode.Impulse);
    }

    private void ResetJump()
    {
        readyToJump = true;
        
        exitingSlope = false;
    }

    private bool OnSlope()
    {
        //  Check와 동시에 out을 통해 slopeHit에 값 반환
        if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, playerHeight * 0.5f + 0.3f))
        {
            //  SteepAngle은 Slope의 지면과의 각도를 측정한다.
            float steepAngle = Vector3.Angle(Vector3.up, slopeHit.normal);
            //  또한 steepAngle이 0이 아닌지도 확인해야 한다. -> LayerMask로 판별하는게 아니라 각도로 판별함
            return steepAngle < maxSlopeAngle && steepAngle != 0;
        }

        return false;
    }

    //  Slope에 대하여 이동하는 방향을 검사해야 한다.
    //  Slope.normal에 대해 project한다. (슬로프 위에서 방향이 정해짐)
    private Vector3 GetSlopeMoveDirection()
    {
        return (Vector3.ProjectOnPlane(moveDirection, slopeHit.normal).normalized);
    }
}
