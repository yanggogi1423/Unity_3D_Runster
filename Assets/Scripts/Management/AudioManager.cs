using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : Singleton<AudioManager>
{
    [Header("Audio Mixer Groups")]
    public AudioMixerGroup bgmMixerGroup; // BGM Mixer Group
    public AudioMixerGroup sfxMixerGroup; // SFX Mixer Group
    
    [Header("BGM")]
    public AudioClip[] bgmClips;
    [Range(0f,1f)]
    public float bgmVolume;
    private AudioSource[] _bgmPlayers;
    private int _bgmChannelIndex;

    [Header("SFX")]
    public AudioClip[] sfxClips;
    [Range(0f,1f)]
    public float sfxVolume;
    private AudioSource[] _sfxPlayers;
    private Dictionary<string, AudioSource> _activeSfx = new Dictionary<string, AudioSource>();

    private GameObject _sfxObject;
    private GameObject _bgmObject;
    public enum Bgm
    {
        Main,
        InGame,
        Ending
    }

    public enum Sfx
    {
        PlayerFire,
        PlayerUltimate,
        PlayerWalk,
        PlayerRunning,
        PlayerWallRunning,
        PlayerJump,
        MonsterDefault,
        MonsterHit,
        MonsterDie,
        MonsterUltimate,
        Darkness
    }
    
    void Awake()
    {
        base.Awake();
        _bgmChannelIndex = bgmClips.Length;
        Init();
        InitializeMixerVolumes();
    }

    void Init()
    {
        //BGM Initialization
        _bgmObject = new GameObject("BGM");
        _bgmObject.transform.parent = transform;
        _bgmPlayers = new AudioSource[_bgmChannelIndex];
        for (int i = 0; i < _bgmPlayers.Length; i++)
        {
            _bgmPlayers[i] = _bgmObject.AddComponent<AudioSource>();
            _bgmPlayers[i].playOnAwake = false;
            _bgmPlayers[i].volume = bgmVolume;
            _bgmPlayers[i].loop = true;
            _bgmPlayers[i].outputAudioMixerGroup = bgmMixerGroup;
        }
        //SFX Initialization
        _sfxObject = new GameObject("SFXPlayer");
        _sfxObject.transform.parent = transform;
    }
    private void InitializeMixerVolumes()
    {
        // BGM 기본 볼륨 초기화
        if (bgmMixerGroup != null)
        {
            // Debug.Log("bgm mixer group set");
            bgmMixerGroup.audioMixer.SetFloat("BGMVolume", Mathf.Log10(Mathf.Clamp(bgmVolume, 0.0001f, 1f)) * 20+42f);
        }

        // SFX 기본 볼륨 초기화
        if (sfxMixerGroup != null)
        {
            // Debug.Log("sfx mixer group set");
            sfxMixerGroup.audioMixer.SetFloat("SFXVolume", Mathf.Log10(Mathf.Clamp(sfxVolume, 0.0001f, 1f)) * 20+16f);
        }
        else
        {
            // Debug.LogWarning("SFX Mixer Group is not assigned! Skipping SFX volume initialization.");
        }
    }

    // BGM 재생
    public void PlayBGM(Bgm bgm, bool isPlay)
    {
        for (int i = 0; i < _bgmPlayers.Length; i++)
        {
            int loopIndex = (i + _bgmChannelIndex) % _bgmPlayers.Length;
            _bgmPlayers[loopIndex].clip = bgmClips[(int)bgm];
            if (isPlay)
            {
                if (_bgmPlayers[loopIndex].isPlaying)
                    continue;
                _bgmPlayers[loopIndex].Play();
            }
            else
            {
                _bgmPlayers[loopIndex].Stop();
            }
        }
    }

    // SFX 재생 (폴링 방식)
    public string PlaySfx(Sfx sfx)
    {
        AudioSource source = _sfxObject.AddComponent<AudioSource>();
        source.volume = sfxVolume;
        source.clip = sfxClips[(int)sfx];
        source.outputAudioMixerGroup = sfxMixerGroup;
        source.dopplerLevel = 0.0f;
        source.reverbZoneMix = 0.0f;
        
        //  if (sfx == Sfx.MonsterDefault) source.volume /= 2f;
        
        source.Play();
        // Debug.Log(_activeSfx.Count);
        string id = System.Guid.NewGuid().ToString(); // 고유 ID 생성
        _activeSfx[id] = source;
        StartCoroutine(RemoveSfxWhenFinished(id, source));
        return id;
    }
    
    public string PlaySfx(Sfx sfx, float distance, float searchDistance)
    {
        AudioSource source = _sfxObject.AddComponent<AudioSource>();
        source.volume = sfxVolume-(sfxVolume* (distance / searchDistance));
        source.clip = sfxClips[(int)sfx];
        source.outputAudioMixerGroup = sfxMixerGroup; // Assign SFX Audio Mixer Group
        source.dopplerLevel = 0.0f;
        source.reverbZoneMix = 0.0f;
        
        //  if (sfx == Sfx.SpaceShipHover) source.loop = true;
        
        source.Play();
        
        string id = System.Guid.NewGuid().ToString(); // 고유 ID 생성
        _activeSfx[id] = source;
        StartCoroutine(RemoveSfxWhenFinished(id, source));
        return id;
    }
    
    //  루프용 사운드 Dictionary (한 종류의 loop당 하나만 재생)
    private Dictionary<Sfx, AudioSource> _loopingSfx = new Dictionary<Sfx, AudioSource>();
    
    public void PlaySfxLoop(Sfx sfx)
    {
        // 현재 루프 중인 같은 SFX면 재생하지 않음
        if (_loopingSfx.ContainsKey(sfx) && _loopingSfx[sfx].isPlaying)
            return;

        // 기존 루프 SFX 전부 정지
        StopAllLoopingSfx();

        // 새 오디오소스 생성 및 설정
        AudioSource source = _sfxObject.AddComponent<AudioSource>();
        source.volume = sfxVolume;
        source.clip = sfxClips[(int)sfx];
        source.loop = true;
        source.outputAudioMixerGroup = sfxMixerGroup;
        source.dopplerLevel = 0.0f;
        source.reverbZoneMix = 0.0f;
        source.Play();

        _loopingSfx[sfx] = source;
    }

    
    public void StopSfxLoop(Sfx sfx)
    {
        if (_loopingSfx.ContainsKey(sfx))
        {
            _loopingSfx[sfx].Stop();
            Destroy(_loopingSfx[sfx]); // AudioSource 제거
            _loopingSfx.Remove(sfx);
        }
    }

    public void StopAllLoopingSfx()
    {
        foreach (var kvp in _loopingSfx)
        {
            kvp.Value.Stop();
            Destroy(kvp.Value);
        }
        _loopingSfx.Clear();
    }

    
    public void ChangeVolume(string id, float distance, float searchDistance)
    {
        if (_activeSfx.ContainsKey(id))
        {
            _activeSfx[id].volume = (sfxVolume - sfxVolume * (distance / searchDistance));
        }
    }
    // 특정 SFX 중지
    public void StopSfx(string id)
    {
        if (_activeSfx.ContainsKey(id))
        {
            _activeSfx[id].Stop(); 
            _activeSfx.Remove(id);
        }
    }

    // 모든 SFX 중지
    public void StopAllSfx()
    {
        foreach (var source in _activeSfx.Values)
        {
            source.Stop();
        }
        _activeSfx.Clear();
        
        RestoreAudioMixerSettings();
    }
    
    public void ChangeBgmVolume(float vol)
    {
        bgmVolume = vol;
        for (int i = 0; i < _bgmPlayers.Length; i++)
        {
            _bgmPlayers[i].volume = bgmVolume;
        }
        bgmMixerGroup.audioMixer.SetFloat("BGMVolume", Mathf.Log10(Mathf.Clamp(bgmVolume, 0.0001f, 1f)) * 20+42f);
    }

    public void ChangeSfxVolume(float vol)
    {
        foreach (var source in _activeSfx.Values)
            source.volume = vol;
        sfxVolume = vol;
        sfxMixerGroup.audioMixer.SetFloat("SFXVolume", Mathf.Log10(Mathf.Clamp(sfxVolume, 0.0001f, 1f)) * 20+16f);
    }
    public void UIBgm(bool isPlay) // UI 창을 띄웠을 때 고음만 통과시켜 간지나게 함
    {
        AudioHighPassFilter bgmEffect = Camera.main.GetComponent<AudioHighPassFilter>();
        bgmEffect.enabled = isPlay;
    }
    public void RestoreAudioMixerSettings()
    {
        if (bgmMixerGroup != null)
        {
            // BGMVolume을 수동으로 복구
            bgmMixerGroup.audioMixer.SetFloat("BGMVolume", Mathf.Log10(Mathf.Clamp(bgmVolume, 0.0001f, 1f)) * 20+42f);
            // bgmMixerGroup.audioMixer.SetFloat("BGMVolume", Mathf.Log10(bgmVolume) * 20);

        }

        if (sfxMixerGroup != null)
        {
            sfxMixerGroup.audioMixer.SetFloat("SFXVolume", Mathf.Log10(Mathf.Clamp(sfxVolume, 0.0001f, 1f)) * 20+16f);
        }
    }
    
// 재생 종료 후 activeSfx에서 제거
    private IEnumerator RemoveSfxWhenFinished(string id, AudioSource source)
    {
        // AudioSource 재생이 끝날 때까지 대기
        yield return new WaitUntil(() => !source.isPlaying);

        // activeSfx에서 제거
        if (_activeSfx.ContainsKey(id))
        {
            _activeSfx.Remove(id);
            Destroy(source); // AudioSource 컴포넌트 제거
        }
    }
}
