using System;
using UnityEngine;

public class LoadingManager : MonoBehaviour
{
    [Header("UIs")] 
    public GameObject pressContainer;
    
    private void Awake()
    {
        pressContainer.SetActive(false);
    }

    private void Update()
    {
        if (SceneController.Instance.canProceed)
        {
            pressContainer.SetActive(true);
        }
    }
}
