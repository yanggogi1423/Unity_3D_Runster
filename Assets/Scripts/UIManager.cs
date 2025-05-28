using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Button = UnityEngine.UIElements.Button;

public class UIManager : MonoBehaviour
{
    public GameObject playerContainer;
    
    [Header("Player")]
    public Image playerHp;
    public Image playerBoost;
    public TMP_Text playerUltimate;
    
    
    [Header("Skills")]
    public Image wallRun;
    public Image climbing;
    public Image sliding;
    
    //  미리 할당하지 않으면 문제가 발생함 반드시 Player는 미리 할당할 것
    private Player player;
    private PlayerMovement pm;
    private WallRunning wr;
    private Climbing climb;
    private PlayerSliding slide;

    //  Coroutine
    private Coroutine hpCoroutine;
    private Coroutine boostCoroutine;
    private Coroutine ultimateCoroutine;
    
    //  Handler
    private bool isModifiedGrace;

    [Header("Top Menu")] 
    public GameObject topContainer;

    public Animator topAnim;
    public Button exitMenuButton;
    public bool topMenuVisible;

    private void Awake()
    {
        player = playerContainer.GetComponent<Player>();
        pm = playerContainer.GetComponent<PlayerMovement>();
        wr = playerContainer.GetComponent<WallRunning>();
        climb = playerContainer.GetComponent<Climbing>();
        slide = playerContainer.GetComponent<PlayerSliding>();
        
        isModifiedGrace = false;

        hpCoroutine = null;
        boostCoroutine = null;
        ultimateCoroutine = null;

        topMenuVisible = true;
    }

    private void Update()
    {
        UISkillUpdate();
    }


    //  Unity Event 사용 - 또 나름의 밸런스 조절이 들어간다. (실제 연산 양과 다름)
    public void UIPlayerHpUpdate()
    {
        if (hpCoroutine != null)
        {
            StopCoroutine(hpCoroutine);
            player.desireHp = player.curHp;
        }
        hpCoroutine = StartCoroutine(UIWithPlayerLerpCoroutine(0));
    }
    
    public void UIPlayerBoostUpdate()
    {
        if (boostCoroutine != null)
        {
            StopCoroutine(boostCoroutine);
            player.desireBoost = player.curBoost;
        }
        boostCoroutine = StartCoroutine(UIWithPlayerLerpCoroutine(1));
    }

    public void UIPlayerUltimateUpdate()
    {
        if (ultimateCoroutine != null)
        {
            StopCoroutine(ultimateCoroutine);
            player.desireUltimate = player.curUltimate;
        }
        ultimateCoroutine = StartCoroutine(UIWithPlayerLerpCoroutine(2));
    }

    //  [Mode] 0 : Hp, 1 : Boost, 2 : Ultimate
    private IEnumerator UIWithPlayerLerpCoroutine(int mode)
    {
        while (true)
        {
            switch (mode)
            {
                case 0:
                    player.curHp = Mathf.Lerp(player.curHp, player.desireHp, 0.1f);
                    playerHp.fillAmount = player.curHp / player.maxHp;
                    if (Mathf.Abs(player.curHp - player.desireHp) < 0.01f)
                    {
                        player.curHp = player.desireHp;
                        hpCoroutine = null;
                        yield break;
                    }
                    break;
                case 1:
                    player.curBoost = Mathf.Lerp(player.curBoost, player.desireBoost, 0.1f);
                    playerBoost.fillAmount = player.curBoost / player.maxBoost;
                    if (Mathf.Abs(player.curBoost - player.desireBoost) < 0.01f)
                    {
                        player.curBoost = player.desireBoost;
                        boostCoroutine = null;
                        yield break;
                    }
                    break;
                case 2:
                    player.curUltimate = Mathf.Lerp(player.curUltimate, player.desireUltimate, 0.1f);
                    playerUltimate.SetText(Mathf.CeilToInt((player.curUltimate / player.maxUltimate) * 100f) + "%");
                    if (Mathf.Abs(player.curUltimate - player.desireUltimate) < 0.01f)
                    {
                        Debug.Log(((player.curUltimate / player.maxUltimate) * 100) + "%");
                        player.curUltimate = player.desireUltimate;
                        ultimateCoroutine = null;
                        yield break;
                    }
                    break;
            }

            yield return null;  
        }
    }
    
    //  구현의 간단함을 위해 Busy-Update 사용
    public void UISkillUpdate()
    {
        wallRun.fillAmount = wr.GetWallRunTimeRatio();
        climbing.fillAmount = climb.GetClimbTimeRatio();
        sliding.fillAmount = slide.GetSlideTimeRatio();

        Debug.Log("Wall Running : " + wr.GetWallRunTimeRatio() +
                  "\nClimbing : " + climb.GetClimbTimeRatio() +
                  "\nSliding : " + slide.GetSlideTimeRatio());
        
        if (player.GetIsGrace() && !isModifiedGrace)
        {
           playerHp.color = new Color(playerHp.color.r, playerHp.color.g, playerHp.color.b, 120);
           isModifiedGrace = true;
        }
        else if(!player.GetIsGrace() && isModifiedGrace)
        {
            playerHp.color = new Color(playerHp.color.r, playerHp.color.g, playerHp.color.b, 255);
            isModifiedGrace = true;
        }
    }
    
    //  Top Menu Visibility
    public void TopMenuToggle()
    {
        topMenuVisible = !topMenuVisible;
        
        topAnim.SetBool("visible", topMenuVisible);
    }
}
