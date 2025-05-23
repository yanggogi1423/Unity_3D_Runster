using System.Collections;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(DestroyCoroutine());
    }
    

    private IEnumerator DestroyCoroutine()
    {
        yield return new WaitForSeconds(5f);
        Destroy(gameObject);
    }
    
    public void OnCollisionEnter(Collision other)
    {
        ContactPoint cp = other.GetContact(0);
        Quaternion rot = Quaternion.LookRotation(-cp.normal);   //  법선
        
        Debug.Log("Oh! Collision!");
    }
}
