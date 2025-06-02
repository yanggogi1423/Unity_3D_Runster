using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.ProBuilder.MeshOperations;

public class InGameManager : MonoBehaviour
{
    [Header("Timer")]
    public TMP_Text timerText;         
    public float timeInSeconds = 0f;    //  시작 시간
    public bool countDown = false;   //  true는 카운트 다운, false는 스톱워치
    private float currentTime;

    [Header("Monsters")]
    public int maxMonster;
    public int curMonster;
    // public int dieMonster;
    public GameObject[] monsterList;
    public int curWave = 1;
    public int maxWave = 3;

    [Header("UIs")] public TMP_Text monsterText;

    [Header("Attributes")]
    public bool isWaving;

    [Header("MonsterSpawn")] public MonsterSpawner mons;
    public TMP_Text waveText;
    public TMP_Text waveTopText;
    
    private void Start()
    {
        currentTime = timeInSeconds;

        monsterList = GameObject.FindGameObjectsWithTag("Monster");
        curMonster = maxMonster = monsterList.Length;
        
        monsterText.SetText(curMonster + " / " + maxMonster);
        
        waveTopText.SetText(curWave + " / " + maxWave);
    }

    private void Update()
    {
        SetTime();

        if (!isWaving && mons.isInit)
        {
            isWaving = true;
            
            StartCoroutine(mons.SpawnMonster(curWave));
            maxMonster = mons.maxMonster[curWave - 1];
            curMonster = maxMonster;
            
            monsterText.SetText(curMonster + " / " + maxMonster);
            waveTopText.SetText(curWave + " / " + maxWave);
            if (curWave < maxWave)
            {
                waveText.SetText("Wave " + curWave);
            }
            else
            {
                waveText.SetText("Final Wave");
            }

            StartCoroutine(ShowWaveTextCoroutine());
        }
    }

    private IEnumerator ShowWaveTextCoroutine()
    {
        waveText.gameObject.GetComponent<Animator>().SetBool("showWave", true);
        yield return new WaitForSeconds(2f);
        waveText.gameObject.GetComponent<Animator>().SetBool("showWave", false);
    }

    //  monster 사망 시에만 update
    public void UpdateMonsterText()
    {
        curMonster--;
        monsterText.SetText(curMonster + " / " + maxMonster);

        if (curWave <= maxWave && curMonster == 0)
        {
            //  TODO : 다음 웨이브 이동
            if(curWave != maxWave)
                StartCoroutine(NextWaveCoroutine());
            
            curWave++;
        }

        if (curMonster == 0 && curWave > maxWave)
        {
            GameManager.Instance.isClear = true;
            StartCoroutine(ClearCoroutine());
        }
    }

    private IEnumerator NextWaveCoroutine()
    {
        yield return new WaitForSeconds(3f);
        isWaving = false;
    }

    private IEnumerator ClearCoroutine()
    {
        yield return new WaitForSeconds(5.5f);
        SceneController.Instance.LoadEndingScene();
    }

    private void SetTime()
    {
        if (countDown)
        {
            currentTime -= Time.deltaTime;
            if (currentTime < 0) currentTime = 0;
        }
        else
        {
            currentTime += Time.deltaTime;
        }

        int minutes = Mathf.FloorToInt(currentTime / 60f);
        int seconds = Mathf.FloorToInt(currentTime % 60f);

        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }
}
