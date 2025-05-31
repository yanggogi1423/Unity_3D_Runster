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
            clearText.SetActive(false);
            failedText.SetActive(true);
        }
        else
        {
            clearText.SetActive(true);
            failedText.SetActive(false);
        }
    }
}
