using System;
using Unity.Cinemachine;
using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [Header("Cross Hair")] 
    public CrossHair cr;

    public float maxDistance = 100f;
    public LayerMask monsterLayer1;
    public LayerMask monsterLayer2;
    
    [SerializeField] private Transform firePos;               // 총구 위치
    [SerializeField] private GameObject bulletEffectPrefab;   // 총알 이펙트 프리팹
    [SerializeField] private float speed = 10000f;

    [Header("References")] 
    public PlayerMovement pm;

    private void Start()
    {
        pm = GetComponent<PlayerMovement>();
    }

    private void Update()
    {
        Detection();
        CheckInput();
    }

    private void CheckInput()
    {
        if (Input.GetMouseButtonDown(0) && pm.CheckShootable())
        {
            Shoot();
        }
    }

    private void Shoot()
    {
        Ray ray = GetCenterScreenRay();

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance))
        {
            Vector3 dir = (hit.point - firePos.position).normalized;
            Quaternion rot = Quaternion.LookRotation(dir) * Quaternion.Euler(90f, 0f, 0f); // Y축 전방 보정
            GameObject bullet = Instantiate(bulletEffectPrefab, firePos.position, rot);
            bullet.GetComponent<Rigidbody>().AddForce(dir * speed, ForceMode.Impulse);
        }
        else
        {
            Vector3 missPoint = ray.origin + ray.direction * maxDistance;
            Vector3 dir = (missPoint - firePos.position).normalized;
            Quaternion rot = Quaternion.LookRotation(dir) * Quaternion.Euler(90f, 0f, 0f); // 동일 보정
            GameObject bullet = Instantiate(bulletEffectPrefab, firePos.position, rot);
            bullet.GetComponent<Rigidbody>().AddForce(dir * speed, ForceMode.Impulse);
        }
    }



    private void Detection()
    {
        Ray ray = GetCenterScreenRay();  // 화면 중앙 기준 Ray

        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, maxDistance, monsterLayer1))
        {
            cr.CrossHairIn(true);
        }
        else if (Physics.Raycast(ray, out hit, maxDistance, monsterLayer2))
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
