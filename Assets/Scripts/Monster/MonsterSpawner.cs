using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class MonsterSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject monsterPrefab;

    public List<GameObject> monsterPool;
    public int initMonsterCnt = 50;

    [Header("Spawn Points")] public Transform[] spawnPointList;

    [Header("Attributes")] 
    public int[] maxMonster;
    
    public bool isInit = false;

    private void Start()
    {
        // SetMonsterSet();
    }

    // private void SetMonsterSet()
    // {
    //     for (int i = 0; i < initMonsterCnt; i++)
    //     {
    //         GameObject mob = Instantiate(monsterPrefab);
    //         mob.SetActive(false);
    //         monsterPool.Add(mob);
    //     }
    //
    //     isInit = true;
    // }
    //
    // private void ResetList()
    // {
    //     for (int i = 0; i < monsterPool.Count; i++)
    //     {
    //         Destroy(monsterPool[i]);
    //     }
    //     
    //     monsterPool.Clear();
    //     
    //     SetMonsterSet();
    // }

    public IEnumerator SpawnMonster(int curWave)
    {
        // ResetList();
        
        for (int i = 0; i < maxMonster[curWave - 1]; i++)
        {
            GameObject mob = Instantiate(monsterPrefab, GetRandomSpawnPoint(), Quaternion.identity);
            // mob.SetActive(true);
            //
            // monsterPool[i].transform.position = GetRandomSpawnPoint();
            // monsterPool[i].SetActive(true);
            
            yield return new WaitForSeconds(2f);
        }
        
    }

    private Vector3 GetRandomSpawnPoint()
    {
        return spawnPointList[Random.Range(0, spawnPointList.Length)].position;
    }
}
