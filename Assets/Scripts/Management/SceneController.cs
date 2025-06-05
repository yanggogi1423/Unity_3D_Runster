using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : Singleton<SceneController>
{

    public bool isTutorial;

    private void Start()
    {
        isTutorial = false;
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

    public void LoadLoadingScene(bool isTutorial)
    {
        this.isTutorial = isTutorial;

        Time.timeScale = 1f;
        
        AudioManager.Instance.StopAllLoopingSfx();
        AudioManager.Instance.StopAllSfx();
        
        StopAllCoroutines();
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        AudioManager.Instance.PlayBGM(AudioManager.Bgm.Tutorial,false);
        AudioManager.Instance.PlayBGM(AudioManager.Bgm.Main,false);
        SceneManager.LoadScene("Loading");
        
    }
    
    public void LoadTutorial()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        SceneManager.LoadScene("Tutorial");
        AudioManager.Instance.PlayBGM(AudioManager.Bgm.Tutorial,true);
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
        AudioManager.Instance.StopAllSfx();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene("Ending");
        // AudioManager.Instance.PlayBGM(AudioManager.Bgm.Ending,true);
    }
}

