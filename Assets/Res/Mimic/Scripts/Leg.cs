using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MimicSpace
{
    public class Leg : MonoBehaviour
    {
        Mimic myMimic;
        public bool isDeployed = false;
        public Vector3 footPosition;
        public float maxLegDistance;
        public int legResolution;
        public LineRenderer legLine;
        public int handlesCount = 8;

        public float legMinHeight;
        public float legMaxHeight;
        float legHeight;
        public Vector3[] handles;
        public float handleOffsetMinRadius;
        public float handleOffsetMaxRadius;
        public Vector3[] handleOffsets;
        public float finalFootDistance;

        public float growCoef;
        public float growTarget = 1;
        [Range(0, 1f)] public float progression;

        bool isRemoved = false;
        bool canDie = false;
        public float minDuration;

        [Header("Rotation")]
        public float rotationSpeed;
        public float minRotSpeed;
        public float maxRotSpeed;
        float rotationSign = 1;
        public float oscillationSpeed;
        public float minOscillationSpeed;
        public float maxOscillationSpeed;
        float oscillationProgress;

        public Color myColor;
        
        public bool isAttackLeg = false;

        public void Initialize(Vector3 footPosition, int legResolution, float maxLegDistance, float growCoef, Mimic myMimic, float lifeTime)
        {
            myColor = new Color(Random.Range(0, 1f), Random.Range(0, 1f), Random.Range(0, 1f));
            this.footPosition = footPosition;
            this.legResolution = legResolution;
            this.maxLegDistance = maxLegDistance;
            this.growCoef = growCoef;
            this.myMimic = myMimic;

            myMimic.legLineList.Add(GetComponent<LineRenderer>());
            this.legLine = GetComponent<LineRenderer>();
            handles = new Vector3[handlesCount];

            handleOffsets = new Vector3[6];
            for (int i = 0; i < 6; i++)
                handleOffsets[i] = Random.onUnitSphere * Random.Range(handleOffsetMinRadius, handleOffsetMaxRadius);

            Vector2 footOffset = Random.insideUnitCircle.normalized * finalFootDistance;
            handles[7] = footPosition + new Vector3(footOffset.x, 0.3f, footOffset.y); // 🟢 땅이 아니라 플레이어 방향으로

            legHeight = Random.Range(legMinHeight, legMaxHeight);
            rotationSpeed = Random.Range(minRotSpeed, maxRotSpeed);
            rotationSign = 1;
            oscillationSpeed = Random.Range(minOscillationSpeed, maxOscillationSpeed);
            oscillationProgress = 0;

            myMimic.legCount++;
            growTarget = 1;

            isRemoved = false;
            canDie = false;
            isDeployed = false;
            StartCoroutine(WaitToDie());
            StartCoroutine(WaitAndDie(lifeTime));
            Sethandles();
        }

        IEnumerator WaitToDie()
        {
            yield return new WaitForSeconds(minDuration);
            canDie = true;
        }

        IEnumerator WaitAndDie(float lifeTime)
        {
            yield return new WaitForSeconds(lifeTime);
            while (myMimic.deployedLegs < myMimic.minimumAnchoredParts)
                yield return null;
            growTarget = 0;
        }

        void Update()
        {
            // if (growTarget == 1 &&
            //     Vector3.Distance(new Vector3(myMimic.legPlacerOrigin.x, 0, myMimic.legPlacerOrigin.z),
            //         new Vector3(footPosition.x, 0, footPosition.z)) > maxLegDistance &&
            //     canDie && myMimic.deployedLegs > myMimic.minimumAnchoredParts)
            // {
            //     growTarget = 0;
            // }
            if (growTarget == 1 &&
                Vector3.Distance(new Vector3(myMimic.legPlacerOrigin.x, 0, myMimic.legPlacerOrigin.z),
                    new Vector3(footPosition.x, 0, footPosition.z)) > maxLegDistance &&
                canDie &&
                myMimic.deployedLegs > myMimic.minimumAnchoredParts + 2) // ✅ 여유를 줌
            {
                Debug.LogWarning($"[삭제 조건 발동] dist={Vector3.Distance(new Vector3(myMimic.legPlacerOrigin.x, 0, myMimic.legPlacerOrigin.z),new Vector3(footPosition.x, 0, footPosition.z))}, deployed={myMimic.deployedLegs}");
                growTarget = 0;
            }    
            
            progression = Mathf.Lerp(progression, growTarget, growCoef * Time.deltaTime);
            
            if (isAttackLeg && legLine != null && legLine.material.HasProperty("_Color"))
            {
                legLine.material.color = Color.red;
                StartCoroutine(RevertColorAfterDelay(1.5f)); // 공격 지속 시간 후 복귀
            }
            else if (legLine != null && legLine.material.HasProperty("_Color"))
            {
                legLine.material.color = Color.black;
            }

            if (!isDeployed && progression > 0.9f)
            {
                myMimic.deployedLegs++;
                isDeployed = true;
            }
            else if (isDeployed && progression < 0.9f)
            {
                myMimic.deployedLegs--;
                isDeployed = false;
            }

            if (progression < 0.5f && growTarget == 0)
            {
                if (!isRemoved)
                {
                    myMimic.legCount--;
                    isRemoved = true;
                }

                if (progression < 0.05f)
                {
                    legLine.positionCount = 0;
                    myMimic.legLineList.Remove(legLine);
                    myMimic.RecycleLeg(gameObject);
                    return;
                }
            }

            Sethandles();
            Vector3[] points = GetSamplePoints((Vector3[])handles.Clone(), legResolution, progression);
            legLine.positionCount = points.Length;
            legLine.SetPositions(points);
        }

        void Sethandles()
        {
            handles[0] = transform.position;
            handles[6] = footPosition + Vector3.up * 0.3f; // 🟢 높이값 고정으로 땅에 박히지 않게

            handles[2] = Vector3.Lerp(handles[0], handles[6], 0.4f);
            handles[2].y = handles[0].y + legHeight;

            handles[1] = Vector3.Lerp(handles[0], handles[2], 0.5f);
            handles[3] = Vector3.Lerp(handles[2], handles[6], 0.25f);
            handles[4] = Vector3.Lerp(handles[2], handles[6], 0.5f);
            handles[5] = Vector3.Lerp(handles[2], handles[6], 0.75f);

            RotateHandleOffset();

            handles[1] += handleOffsets[0];
            handles[2] += handleOffsets[1];
            handles[3] += handleOffsets[2];
            handles[4] += handleOffsets[3] / 2f;
            handles[5] += handleOffsets[4] / 4f;
        }

        void RotateHandleOffset()
        {
            oscillationProgress += Time.deltaTime * oscillationSpeed;
            if (oscillationProgress >= 360f)
                oscillationProgress -= 360f;

            float newAngle = rotationSpeed * Time.deltaTime * Mathf.Cos(oscillationProgress * Mathf.Deg2Rad) + 1f;

            for (int i = 1; i < 6; i++)
            {
                Vector3 axisRotation = (handles[i + 1] - handles[i - 1]) / 2f;
                handleOffsets[i - 1] = Quaternion.AngleAxis(newAngle, rotationSign * axisRotation) * handleOffsets[i - 1];
            }
        }

        Vector3[] GetSamplePoints(Vector3[] curveHandles, int resolution, float t)
        {
            List<Vector3> segmentPos = new List<Vector3>();
            float segmentLength = 1f / resolution;

            for (float _t = 0; _t <= t; _t += segmentLength)
                segmentPos.Add(GetPointOnCurve((Vector3[])curveHandles.Clone(), _t));
            segmentPos.Add(GetPointOnCurve(curveHandles, t));
            return segmentPos.ToArray();
        }

        Vector3 GetPointOnCurve(Vector3[] curveHandles, float t)
        {
            int currentPoints = curveHandles.Length;
            while (currentPoints > 1)
            {
                for (int i = 0; i < currentPoints - 1; i++)
                    curveHandles[i] = Vector3.Lerp(curveHandles[i], curveHandles[i + 1], t);
                currentPoints--;
            }
            return curveHandles[0];
        }
        
        private IEnumerator RevertColorAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (legLine != null && legLine.material != null)
            {
                legLine.material.color = Color.black;
                isAttackLeg = false;
            }
        }
        
    }
}