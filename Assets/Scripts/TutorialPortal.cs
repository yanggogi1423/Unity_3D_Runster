using System;
using UnityEngine;

public class TutorialPortal : MonoBehaviour
{
  
    public void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            if (other.gameObject.GetComponent<Player>().tm.curState == TutorialManager.State.Move)
            {
                StartCoroutine(other.gameObject.GetComponent<Player>().tm.BuffNextState());
                
                transform.localScale = new Vector3(0, 0, 0);
                GetComponent<BoxCollider>().enabled = false;
                Destroy(gameObject, 10f);
            }
            else
            {
                Debug.LogError("Tutorial Error Occured");
            }
        }
    }
}
