using System;
using MimicSpace;
using UnityEngine;
using UnityEngine.AI;

public class DefaultEnemyMovement : MonoBehaviour
{
    [Header("Properties")] 
    public float speed;

    [Tooltip("NavMeshAgent의 Base Offset(땅에서 띄울 높이)과 동일하게 설정해주세요")]
    [Range(0.5f, 5f)]
    public float height = 0.8f;

    // 다리 애니메이션용 속도 보간 변수
    private Vector3 mimicVelocity = Vector3.zero;
    public float velocityLerpCoef = 4f;
    
    [Header("References")] 
    public NavMeshAgent agent;
    private Mimic myMimic;
    private DefaultMonster dm;   // 몬스터 상태 제어용

    [Header("StateMachine")] 
    public Transform target;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        myMimic = GetComponent<Mimic>();
        dm = GetComponent<DefaultMonster>();

        // NavMeshAgent의 Base Offset을 height로 설정
        //  agent.baseOffset = height;

        // 플레이어 태그를 통해 타겟을 가져옴
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            target = playerObj.transform;
    }

    private void Update()
    {
        // 1) NavMeshAgent가 매 프레임 계산한 velocity를 기반으로
        //    다리 애니메이션용 mimicVelocity를 보간
        Vector3 flatVel = new Vector3(agent.velocity.x, 0f, agent.velocity.y);
        Vector3 targetVel = flatVel.normalized * speed;
        mimicVelocity = Vector3.Lerp(
            mimicVelocity,
            targetVel,
            velocityLerpCoef * Time.deltaTime
        );

        // 2) Mimic 컴포넌트에 전달하여 애니메이션에 반영
        myMimic.velocity = mimicVelocity;

        // 이동 경로 탐색과 실제 X,Z 이동은 NavMeshAgent가 전담하므로,
        // Update() 안에서 transform.position을 직접 건드리지 않습니다.
    }

    private void FixedUpdate()
    {
        // 3) 타겟이 존재하고 에이전트가 정지되지 않은 상태면 경로를 설정
        if (target == null || agent.isStopped)
            return;

        agent.SetDestination(target.position);
    }
}
