using System;
using System.Collections;
using UnityEngine;

public class LoadingManager : MonoBehaviour
{
    [Header("UIs")] 
    public GameObject pressContainer;

    public bool canProceed;
    
    private void Awake()
    {
        Time.timeScale = 1f;
        pressContainer.SetActive(false);

        canProceed = false;

        StartCoroutine(ProceedCoroutine());
    }

    private void Update()
    {
        if (canProceed)
        {
            pressContainer.SetActive(true);
        }
        
        // Loading 씬에서 Space 입력 가능할 때만 반응
        if (canProceed)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (SceneController.Instance.isTutorial)
                {
                    SceneController.Instance.LoadTutorial();
                    canProceed = false;
                }
                else
                {
                    SceneController.Instance.LoadInGameScene();
                    canProceed = false;
                }

                //  항상 false로 초기화
                SceneController.Instance.isTutorial = false;
            }
        }
    }

    private IEnumerator ProceedCoroutine()
    {
        yield return new WaitForSeconds(3f);
        canProceed = true;
    }
}
