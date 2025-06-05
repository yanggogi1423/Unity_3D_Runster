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
    [SerializeField] private float speed = 15000f;

    [Header("MuzzleFlash")] public GameObject muzzle;

    [Header("References")] 
    public PlayerMovement pm;
    public UltimateController uc;  //   추가
    
    [Header("Fire Control")]
    [SerializeField] private float fireCooldown = 0.1f;
    private float fireTimer;

    private void Start()
    {
        pm = GetComponent<PlayerMovement>();
        uc = GetComponent<UltimateController>(); // ← GetComponent로 연결
    }
    
    private void Update()
    {
        if (pm.player.isPause) return;

        fireTimer += Time.deltaTime;

        Detection();
        
        if(!pm.player.tm.isShowingText)
            CheckInput();
    }

    private void CheckInput()
    {
        if (Input.GetMouseButton(0) && fireTimer >= fireCooldown && pm.CheckShootable())
        {
            if (pm.player.desireBoost < 1f) return;
            
            fireTimer = 0f; // 쿨타임 리셋

            if (uc.IsUltimateActive)
            {
                Transform target = uc.GetNearestEnemyInView();
                if (target != null)
                {
                    ShootAtTarget(target.position);
                }
                else
                {
                    ShootForward(); // fallback
                }
            }
            else
            {
                ShootForward();

                if (pm.player.curBoost > 0f)
                {
                    pm.player.desireBoost -= 1f;
                    pm.player.desireBoost = Mathf.Max(0f, pm.player.desireBoost);
                    pm.player.OnPlayerBoostChaged.Invoke();
                }
            }
        }
    }



    private void Shoot()
    {
        Ray ray = GetCenterScreenRay();
        
        muzzle.SetActive(true);

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
        
        Invoke(nameof(TurnOffMuzzle),0.1f);
    }

    private void TurnOffMuzzle()
    {
        muzzle.SetActive(false);
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
    
    private void ShootAtTarget(Vector3 targetPos)
    {
        Vector3 dir = (targetPos - firePos.position).normalized;
        Quaternion rot = Quaternion.LookRotation(dir) * Quaternion.Euler(90f, 0f, 0f);

        GameObject bullet = Instantiate(bulletEffectPrefab, firePos.position, rot);
        bullet.GetComponent<Rigidbody>().AddForce(dir * speed, ForceMode.Impulse);

        muzzle.SetActive(true);
        Invoke(nameof(TurnOffMuzzle), 0.1f);
    }

    private void ShootForward()
    {
        Ray ray = GetCenterScreenRay();

        muzzle.SetActive(true);

        Vector3 shootPoint;
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance))
        {
            shootPoint = hit.point;
        }
        else
        {
            shootPoint = ray.origin + ray.direction * maxDistance;
        }

        Vector3 dir = (shootPoint - firePos.position).normalized;
        Quaternion rot = Quaternion.LookRotation(dir) * Quaternion.Euler(90f, 0f, 0f);
        GameObject bullet = Instantiate(bulletEffectPrefab, firePos.position, rot);
        bullet.GetComponent<Rigidbody>().AddForce(dir * speed, ForceMode.Impulse);

        Invoke(nameof(TurnOffMuzzle), 0.1f);
    }

}
