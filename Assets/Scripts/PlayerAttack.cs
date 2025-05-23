using System;
using Unity.Cinemachine;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [Header("Cross Hair")] 
    public CrossHair cr;

    public float maxDistance = 100f;
    public LayerMask monsterLayer;
    
    [SerializeField] private Transform firePos;               // 총구 위치
    [SerializeField] private GameObject bulletEffectPrefab;   // 총알 이펙트 프리팹
    [SerializeField] private float speed = 20000f;
     
    private void Update()
    {
        Detection();
        CheckInput();
    }

    private void CheckInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Shoot();
        }
    }

    private void Shoot()
    {
        Ray ray = GetCenterScreenRay();

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance))
        {
            // 1. 실제 피격 처리
            Debug.Log("Hit: " + hit.collider.name);

            // 2. 총구에서 hit 지점 방향으로 이펙트 생성
            Vector3 dir = (hit.point - firePos.position).normalized;
            GameObject bullet = Instantiate(bulletEffectPrefab, firePos.position, Quaternion.LookRotation(dir));

            bullet.GetComponent<Rigidbody>().AddForce(dir * speed, ForceMode.Impulse);
        }
        else
        {
            // 3. 맞은 게 없다면 Ray 방향 끝으로 이펙트
            Vector3 missPoint = ray.origin + ray.direction * maxDistance;
            Vector3 dir = (missPoint - firePos.position).normalized;
            GameObject bullet = Instantiate(bulletEffectPrefab, firePos.position, Quaternion.LookRotation(dir));
            
            bullet.GetComponent<Rigidbody>().AddForce(dir * speed,ForceMode.Impulse);
        }
    }


    private void Detection()
    {
        Ray ray = GetCenterScreenRay();  // 화면 중앙 기준 Ray

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, monsterLayer))
        {
            cr.CrossHairIn(true);
        }
        else
        {
            cr.CrossHairIn(false);
        }
    }

    private Ray GetCenterScreenRay()
    {
        var brain = CinemachineBrain.GetActiveBrain(0);

        if (brain != null && brain.OutputCamera != null)
        {
            Camera activeCam = brain.OutputCamera;
            Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f);
            return activeCam.ScreenPointToRay(screenCenter);
        }

        return Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f));
    }

    
}
