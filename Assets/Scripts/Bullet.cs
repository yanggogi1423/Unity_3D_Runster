using System;
using System.Collections;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private int attackPower = 10;
    
    
    void Start()
    {
        StartCoroutine(DestroyCoroutine());

        AudioManager.Instance.PlaySfx(AudioManager.Sfx.PlayerFire);
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

        
        GetComponent<Rigidbody>().linearVelocity = Vector3.zero;

        if (other.gameObject.CompareTag("Monster"))
        {
            if (other.gameObject != null)
            {
                other.gameObject.GetComponent<DefaultMonster>().GetDamage(attackPower);
            }
                
            Destroy(gameObject);
        }
        else
        {
            Destroy(gameObject, 1.5f);
        }
    }

    public void OnTriggerEnter(Collider other)
    {
        GetComponent<Rigidbody>().linearVelocity = Vector3.zero;

        if (other.gameObject.CompareTag("Monster"))
        {
            if (other.gameObject != null)
            {
                other.gameObject.GetComponent<DefaultMonster>().GetDamage(attackPower);
            }
                
            Destroy(gameObject);
        }
    }
}
