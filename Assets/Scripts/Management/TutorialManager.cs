using System;
using System.Collections.Generic;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    [Header("Monster")] public GameObject monsterPrefab;

    [Header("Attributes")] public State curState;

    public enum State
    {
        Init,
        Move,
        Jump,
        Run,
        Slide,
        FirstThird,
        WallRun,
        Climb,
        Boost,
        Enemy,
        Attack,
        EnemyUltimate,
        PlayerUltimate,
        End
    }

    //  Texts
    private List<String[]> stringsList;
    private String[] curList;

    private void SetText()
    {
        stringsList.Add(new String[]
        {
            "반갑습니다. 저는 기본 전투 훈련 시스템입니다.",
            "당신은 임무를 완수하기 위해 배치 받은 Runster 요원입니다.",
            "귀하의 성공적인 임무를 위해 지금부터 전투에 대한 사항을 알려드리겠습니다.",
            "혹시 제 얘기가 기억이 나지 않으신다면 esc키를 눌러 조작법을 재확인할 수 있습니다."
        } );
        
        stringsList.Add(new String[]
        {
            "먼저 이동기부터 배워보겠습니다.",
            "기본적인 이동은 WASD로 진행합니다.",
            "빛나는 지점으로 이동하세요."
        } );
        
        stringsList.Add(new String[]
        {
            "점프는 Space를 통해 진행합니다.",
            "점프를 진행해보십시오."
        } );
        
        stringsList.Add(new String[]
        {
            "Shift 키와 함께 이동을 하면 달릴 수 있습니다",
            "달려볼까요?"
        } );
        
        stringsList.Add(new String[]
        {
            "좋습니다. 이번에는 순간적인 가속을 해줄 슬라이딩을 해보겠습니다.",
            "이동과 함께 Ctrl를 입력해보세요."
        } );
        
        stringsList.Add(new String[]
        {
            "아주 잘했습니다. 기본적인 이동은 해냈습니다.",
            "요원에 따라 바라보는 시각에 따라 명중률이 달라진다고 하더군요.",
            "T 키를 누르면 시점을 전환할 수 있습니다."
        } );
        
        stringsList.Add(new String[]
        {
            "이번에는 특수 이동에 대해 알아보겠습니다.",
            "우리 Runster는 특수 제작 갑옷을 이용해 여러 가지 이동을 할 수 있습니다.",
            "벽달리기는 벽을 향해 점프 후 이동을 하면 진행할 수 있습니다",
            "Shift키를 누르면 상향 벽타기, Ctrl를 누르면 하향 벽타기를 진행합니다."
        } );
        
        stringsList.Add(new String[]
        {
            "좋습니다. 벽달리기는 기본적인 달리기보다 훨씬 빠르며 통계상 요원의 생존확률을 올려줍니다.",
            "또 벽을 타고 올라갈 수도 있습니다.",
            "벽을 향해 점프하여 전진키(w)을 누르면 수행할 수 있습니다."
        } );
        
        stringsList.Add(new String[]
        {
            "아주 잘하셨습니다. 요원님께서는 아무래도 오래 살 운명인 것 같군요.",
            "이번에는 우리의 최첨단 시스템인 운동학동력발전기에 대해 알려드립니다.",
            "음.. 어렵다구요? 그래서 보통 부스트라고 부릅니다. (하하)",
            "하단 바의 좌측 게이지는 체력, 우측 게이지가 부스트입니다.",
            "우리는 일정 속도 이상으로 이동하지 않으면 부스트가 바닥나게 됩니다.",
            "부스트가 바닥나면 무기를 사용할 수 없을 뿐만 아니라, 생명 유지 장치가 정지됩니다.",
            "요원님, 반드시 일정 속도 이상 달려야 함을 잊지 마세요."
        } );
        
        stringsList.Add(new String[]
        {
            "마침 저희가 만든 모의 적 전투를 수행할 수 있습니다.",
            "앞의 적은 군벌이 길들인 그 적의 복사본입니다.",
            "이 적을 처치하기 위해서는 머리 부분인 검은 구슬을 때려야 합니다.",
            "검은 구슬을 때리다 보면 점점 빨간색으로 변하게 됩니다"
        } );
        
        stringsList.Add(new String[]
        {
            "적을 공격하기 위해서는 마우스 좌클릭을 하면 됩니다.",
            "적에게 피해를 주도록 하세요."
        } );
        
        stringsList.Add(new String[]
        {
            "이런, 제가 말씀 안드렸나요.",
            "적은 일정 체력 이하로 내려가면 궁극 상태에 돌입하게 됩니다.",
            "이때 공격을 받으면 시야가 마비된다고 알려져 있습니다.",
            "또한 이동 속도가 훨씬 빨라집니다."
        } );
        
        stringsList.Add(new String[]
        {
            "물론 우리 요원들도 궁극기를 사용할 수 있습니다.",
            "적을 처치하면, 하단 바 가운데에 궁극기 게이지가 차오릅니다.",
            "100%가 되면 우리가 개발한 자동 표적 조준기를 가동할 수 있습니다.",
            "아 물론 공격은 수동이기에, 좌클릭을 꼭 하셔야 합니다.",
            "서비스로 부스트또한 무한 상태로 돌입하게 되죠.",
            "사용 권한을 드릴테니, 사용해볼까요?"
        } );
        
        stringsList.Add(new String[]
        {
            "수고하셨습니다. 모든 모의 전투가 종료됩니다.",
            "우리의 숙명을 잊지마시길 바랍니다.",
            "달려라. 싸워라.",
            "------ 연결 종료 ------"
        } );
    }

    private void Awake()
    {
        curState = State.Init;

        stringsList = new List<string[]>();
        SetText();
    }

    private void Start()
    {
        curList = stringsList;
    }

    private void Update()
    {
        switch (curState)
        {
            case State.Init :
                
                break;
            case State.Move :
                
                break;
            case State.Jump :
                
                break;
            case State.Run :
                
                break;
            case State.Slide :
                
                break;
            case State.FirstThird :
                
                break;
            case State.WallRun :
                
                break;
            case State.Climb :
                
                break;
            case State.Boost :
                
                break;
            case State.Enemy :
                
                break;
            case State.Attack :
                
                break;
            case State.EnemyUltimate :
                
                break;
            case State.PlayerUltimate :
                
                break;
            case State.End :
                
                break;
        }
    }

    private void ShowText()
    {
        
    }
    
    
    
}
