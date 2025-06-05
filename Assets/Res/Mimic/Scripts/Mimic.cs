using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace MimicSpace
{
    public class Mimic : MonoBehaviour
    {
        [Header("Animation")]
        public GameObject legPrefab;

        [Range(2, 20)]
        public int numberOfLegs = 5;
        [Range(1, 10)]
        public int partsPerLeg = 4;

        int maxLegs;
        public int legCount;
        public int deployedLegs;
        [Range(0, 19)] public int minimumAnchoredLegs = 2;
        public int minimumAnchoredParts;

        public float minLegLifetime = 5;
        public float maxLegLifetime = 15;

        public Vector3 legPlacerOrigin = Vector3.zero;
        public float newLegRadius = 3;

        public float minLegDistance = 4.5f;
        public float maxLegDistance = 6.3f;

        [Range(2, 50)]
        public int legResolution = 40;

        public float minGrowCoef = 4.5f;
        public float maxGrowCoef = 6.5f;

        public float newLegCooldown = 0.3f;
        bool canCreateLeg = true;

        List<GameObject> availableLegPool = new List<GameObject>();

        public Vector3 velocity;

        public DefaultMonster dm;
        public List<LineRenderer> legLineList = new List<LineRenderer>();

        public bool isDying = false;

        //   공격 관련
        public float attackCooldown = 3f;
        public float attackDistance = 5f;
        public int maxAttackLegs = 3;
        private float lastAttackTime;
        public Transform player;

        void Awake()
        {
            // 초기값 보장
            Vector2 randV = Random.insideUnitCircle;
            velocity = new Vector3(randV.x, 0, randV.y);
        }

        void Start()
        {
            ResetMimic(); // legCount, deployedLegs 등은 여기서 초기화
            isDying = false;
            dm = GetComponent<DefaultMonster>();
        }

        private void OnValidate()
        {
            ResetMimic();
        }

        private void OnEnable()
        {
            ResetMimic();
        }

        private void ResetMimic()
        {
            foreach (Leg g in GameObject.FindObjectsOfType<Leg>())
            {
                Destroy(g.gameObject);
            }

            legCount = 0;
            deployedLegs = 0;
            maxLegs = numberOfLegs * partsPerLeg;

            Vector2 randV = Random.insideUnitCircle;
            velocity = new Vector3(randV.x, 0, randV.y);
            minimumAnchoredParts = minimumAnchoredLegs * partsPerLeg;
            maxLegDistance = newLegRadius * 2.1f;
        }

        IEnumerator NewLegCooldown()
        {
            canCreateLeg = false;
            yield return new WaitForSeconds(newLegCooldown);
            canCreateLeg = true;
        }
        
        //  For Test
        // void TestRaycast()
        // {
        //     Vector3 testPos = transform.position + Vector3.up * 20f;
        //     RaycastHit hit;
        //     int groundMask = ~LayerMask.GetMask("MimicBody", "LegPart");
        //
        //     if (Physics.Raycast(testPos, Vector3.down, out hit, 50f, groundMask))
        //     {
        //         Debug.Log("✅ Raycast 성공: " + hit.point);
        //     }
        //     else
        //     {
        //         Debug.LogError("❌ Raycast 실패! 현재 위치: " + transform.position);
        //     }
        // }


        void Update()
        {
            if (isDying || dm.isSpawning) return;

            if (isDying) return;
            
            // TestRaycast();
            
            if (velocity == Vector3.zero)
            {
                Debug.LogWarning("⚠ Instantiate된 Mimic의 velocity가 0입니다!");
            }

            if (dm.curState.ToString() == "Attack" && player != null)
            {
                if (Time.time - lastAttackTime > attackCooldown && Vector3.Distance(transform.position, player.position) < attackDistance)
                {
                    lastAttackTime = Time.time;
                    AttackWithLegsAtTarget(player.position, maxAttackLegs);
                }
                return;
            }

            if (!canCreateLeg) return;

            legPlacerOrigin = transform.position + velocity.normalized * newLegRadius;

            if (legCount <= maxLegs - partsPerLeg)
            {
                Vector2 offset = Random.insideUnitCircle * newLegRadius;
                Vector3 newLegPosition = legPlacerOrigin + new Vector3(offset.x, 0, offset.y);

                if (velocity.magnitude > 1f)
                {
                    float newLegAngle = Vector3.Angle(velocity, newLegPosition - transform.position);
                    if (Mathf.Abs(newLegAngle) > 90)
                        newLegPosition = transform.position - (newLegPosition - transform.position);
                }

                if (Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z),
                        new Vector3(legPlacerOrigin.x, 0, legPlacerOrigin.z)) < minLegDistance)
                {
                    newLegPosition = ((newLegPosition - transform.position).normalized * minLegDistance) + transform.position;
                }

                if (Vector3.Angle(velocity, newLegPosition - transform.position) > 45)
                {
                    newLegPosition = transform.position +
                                     ((newLegPosition - transform.position) + velocity.normalized * (newLegPosition - transform.position).magnitude) / 2f;
                }

                int groundMask = ~LayerMask.GetMask("MimicBody", "LegPart");

                //  Default : 10 -> 20로 변경
                RaycastHit hit;
                Physics.Raycast(newLegPosition + Vector3.up * 20f, -Vector3.up, out hit, Mathf.Infinity, groundMask);

                Vector3 myHit = hit.point;
                if (Physics.Linecast(transform.position, hit.point, out hit, groundMask))
                    myHit = hit.point;

                float lifeTime = Random.Range(minLegLifetime, maxLegLifetime);

                StartCoroutine(NewLegCooldown());
                for (int i = 0; i < partsPerLeg; i++)
                {
                    RequestLeg(myHit, legResolution, maxLegDistance, Random.Range(minGrowCoef, maxGrowCoef), this, lifeTime);
                    if (legCount >= maxLegs)
                        return;
                }
            }
        }

        private void RequestLeg(Vector3 footPosition, int legResolution, float maxLegDistance, float growCoef, Mimic myMimic, float lifeTime)
        {
            GameObject newLeg = GetLegFromPool();
            newLeg.SetActive(true);
            newLeg.GetComponent<Leg>().Initialize(footPosition, legResolution, maxLegDistance, growCoef, this, lifeTime);
            SetLegLayer(newLeg);
        }

        private GameObject GetLegFromPool()
        {
            if (availableLegPool.Count > 0)
            {
                var leg = availableLegPool[availableLegPool.Count - 1];
                availableLegPool.RemoveAt(availableLegPool.Count - 1);
                return leg;
            }
            return Instantiate(legPrefab, transform.position, Quaternion.identity);
        }

        private void SetLegLayer(GameObject leg)
        {
            int legLayer = LayerMask.NameToLayer("LegPart");
            leg.layer = legLayer;
            foreach (Transform child in leg.transform)
                child.gameObject.layer = legLayer;
            leg.transform.SetParent(transform);
        }

        public void RecycleLeg(GameObject leg)
        {
            availableLegPool.Add(leg);
            leg.SetActive(false);
        }

        public void AttackWithLegsAtTarget(Vector3 targetPosition, int attackLegCount)
        {
            for (int i = 0; i < attackLegCount; i++)
            {
                Vector3 attackTargetPos = targetPosition + Random.insideUnitSphere * 0.5f;
                attackTargetPos.y = targetPosition.y;

                GameObject newLeg = GetLegFromPool();
                newLeg.SetActive(true);
                var leg = newLeg.GetComponent<Leg>();
                
                leg.isAttackLeg = true; 
                leg.Initialize(attackTargetPos, legResolution, maxLegDistance, Random.Range(minGrowCoef, maxGrowCoef), this, 1.5f);
                SetLegLayer(newLeg);
            }
        }
        
        public void BoostLegSpeed()
        {
            minGrowCoef *= 1.8f;
            maxGrowCoef *= 1.8f;

            minLegLifetime *= 0.5f;
            maxLegLifetime *= 0.5f;

            newLegCooldown *= 0.5f;

            maxLegDistance *= 1.5f; // ▶ 더 멀리 뻗을 수 있도록
        }
    }
}
