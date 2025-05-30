using TMPro;
using UnityEngine;

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
    public GameObject[] monsterList;

    [Header("UIs")] public TMP_Text monsterText;
    
    private void Start()
    {
        currentTime = timeInSeconds;

        monsterList = GameObject.FindGameObjectsWithTag("Monster");
        curMonster = maxMonster = monsterList.Length;
        
        monsterText.SetText(curMonster + " / " + maxMonster);
    }

    private void Update()
    {
        SetTime();
    }

    //  monster 사망 시에만 update
    public void UpdateMonsterText()
    {
        curMonster--;
        monsterText.SetText(curMonster + " / " + maxMonster);
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
