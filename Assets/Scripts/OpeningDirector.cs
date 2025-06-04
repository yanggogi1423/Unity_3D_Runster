using System;
using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class OpeningDirector : MonoBehaviour
{
    [Header("UIs")]
    public GameObject titleText;

    public Animator titleTextAnim;
    public TMP_Text[] titles;

    public GameObject buttonSet;
    public Button startButton;
    public Button exitButton;

    private string[] titleFake =
    {
        "XUJETOA", "PLAKSEN", "NURQTAZ", "ZERUMON", "KAWRELS",
        "BUNDZEF", "VORTENQ", "QWESYUH", "ZINAFOL", "DERTUSI",
        "MEKLTAQ", "FIRWOBX", "WONQERP", "TULOPZX", "VANSKER",
        "XELRUGN", "LURMOTE", "QUPONER", "NUWRTEO", "TREKOVA",
        "HUSTEMQ", "KLERWON", "FRANTEZ", "GIRPLOK", "ZUKENOP",
        "YUNSELP", "MRANKOT", "JEMQRUX", "YOLTSER", "MIWERQU",
        "JUKANEL", "RONKUTE", "DUKMEPW", "KOWRELT", "XOMREND",
        "SIWOPAT", "LANKWES", "TURMEPO", "REDSYUK", "BLOQTAZ",
        "ZIRMWEN", "NOPWELT", "QERMIUX", "TRUDEFA", "VELMKOP",
        "KERNULO", "XAYWERT", "QERTUOP", "WUKENAL", "ZENFRAW",
        "RUNSTER" 
    };

    private void Awake()
    {
        exitButton.onClick.AddListener(OnExitButtonClick);
        startButton.onClick.AddListener(OnStartButtonClick);
    }

    private void Start()
    {
        titleText.SetActive(false);
        buttonSet.SetActive(false);
        
        AudioManager.Instance.PlayBGM(AudioManager.Bgm.Main, true);

        StartCoroutine(StartInTro());
    }

    private IEnumerator StartInTro()
    {
        yield return new WaitForSeconds(6.5f);
        
        titleText.SetActive(true);
        titleTextAnim.SetBool("isOn",true);
        StartCoroutine(TitleChange());
    }

    private IEnumerator TitleChange()
    {
        foreach (var s in titleFake)
        {
            for (int i = 0; i < titles.Length; i++)
            {
                titles[i].SetText(s);
            }

            yield return new WaitForSeconds(0.035f);
        }
        
        buttonSet.SetActive(true);
    }

    public void OnExitButtonClick()
    {
        Application.Quit();
    }

    public void OnStartButtonClick()
    {
        SceneController.Instance.LoadLoadingScene(true);
    }
}
