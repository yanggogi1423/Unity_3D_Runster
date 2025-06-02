using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EndingDirector : MonoBehaviour
{
    [Header("UIs")] public Button homeButton;
    public GameObject clearText;
    public GameObject failedText;

    private void Start()
    {
        homeButton.onClick.AddListener(SceneController.Instance.LoadMainScene);

        if (GameManager.Instance.playerDie)
        {
            AudioManager.Instance.PlayBGM(AudioManager.Bgm.GameOver,true);
            clearText.SetActive(false);
            failedText.SetActive(true);
        }
        else
        {
            AudioManager.Instance.PlayBGM(AudioManager.Bgm.Ending,true);
            clearText.SetActive(true);
            failedText.SetActive(false);
        }
    }
}
