using System;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [Header("Cross Hair")] public GameObject chPrefab;
    public LayerMask monsterLayer;

    [Header("Weapon")]
    public float maxDistance = 100f;
    
    private void Start()
    {
        chPrefab = Instantiate(chPrefab);
    }

    private void LateUpdate()
    {
        PositionCrossHair();
    }

    private void PositionCrossHair()
    {
        RaycastHit hit;
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f));

        if (Physics.Raycast(ray, out hit, maxDistance, monsterLayer))
        {
            Vector3 hitPos = hit.point;
            chPrefab.transform.position = hitPos;
            chPrefab.transform.LookAt(Camera.main.transform);
        }
    }
    
}
