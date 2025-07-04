using System;
using UnityEngine;

public class Item : MonoBehaviour
{
    public enum ItemStatus
    {
        HealPack,
        Cell
    }

    [Header("Item attributes")] 
    public ItemStatus itemType;
    public int heal = 20;
    public int cell = 20;

    [Header("Effects")] public GameObject effects;


    public void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            if (itemType == ItemStatus.HealPack)
            {
                other.gameObject.GetComponent<Player>().GetItemHealPack(heal);
            }
            else
            {
                other.gameObject.GetComponent<Player>().GetItemCell(cell);
            }

            AudioManager.Instance.PlaySfx(AudioManager.Sfx.ItemSound);
            Destroy(gameObject);
        }
    }

    // public void OnTriggerEnter(Collider other)
    // {
    //     if (other.gameObject.CompareTag("Player"))
    //     {
    //         if (itemType == ItemStatus.Cell)
    //         {
    //             other.gameObject.GetComponent<Player>().GetItemHealPack(heal);
    //         }
    //         else
    //         {
    //             other.gameObject.GetComponent<Player>().GetItemHealPack(cell);
    //         }
    //         Destroy(gameObject);
    //     }
    // }
}