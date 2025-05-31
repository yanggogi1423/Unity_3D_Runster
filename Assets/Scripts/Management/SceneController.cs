using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : Singleton<SceneController>
{
    public bool canProceed = false;

    private void Start()
    {
        
    }

    private void Update()
    {
        // Loading 씬에서 Space 입력 가능할 때만 반응
        if (SceneManager.GetActiveScene().name == "Loading" && canProceed)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                LoadInGameScene();
                canProceed = false;
            }
        }
    }

    private IEnumerator WaitBeforeAllowingInput()
    {
        yield return new WaitForSeconds(3f);
        canProceed = true;
        Debug.Log("Press Space to continue...");
    }

    // === 씬 로딩 함수들 ===

    public void LoadMainScene()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 1f;
        SceneManager.LoadScene("Main");
        AudioManager.Instance.PlayBGM(AudioManager.Bgm.Main,true);
    }

    public void LoadLoadingScene()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        AudioManager.Instance.PlayBGM(AudioManager.Bgm.Main,false);
        SceneManager.LoadScene("Loading");
        StartCoroutine(WaitBeforeAllowingInput());
    }

    public void LoadInGameScene()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        SceneManager.LoadScene("InGame");
        AudioManager.Instance.PlayBGM(AudioManager.Bgm.InGame,true);
    }

    public void LoadEndingScene()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene("Ending");
        AudioManager.Instance.PlayBGM(AudioManager.Bgm.Ending,true);
    }
}

