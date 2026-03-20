using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EffectSoundNum
{
    // ...existing code...
    BGM,
    SaveSound,      // 답변 저장 소리
    SoulPieceSound, // 마음 조각 뜨는 소리
    ConfirmSound,   // 사용자 확인 완료음
    PopupSound,     // 팝업 뜨는 소리
    ActiveSound,     // 활성화음
    StepTextSound,
    GreenHandSound,
    WhiteLEDSound,
    SecondSound3,
    SecondSound5,
    QuestionSound,
    StartSound,
    TimerSound100
}
public class SoundManager : MonoBehaviour, IJsonGenericTarget
{

    public static SoundManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<SoundManager>();

                if (instance == null)
                {
                    GameObject singletonObject = new GameObject("SoundManager");
                    instance = singletonObject.AddComponent<SoundManager>();
                }
            }

            return instance;
        }
    }
    static SoundManager instance;

    AudioSource[] audioSources;
    JsonGenericUpData _genericData = new JsonGenericUpData();

    float[] _soundVolume = new float[System.Enum.GetValues(typeof(EffectSoundNum)).Length];

    [Header("사운드 매니저\nBGM = 0\nSaveSound = 1\nSoulPieceSound = 2\nConfirmSound = 3\nPopupSound = 4\nActiveSound = 5\nStepTextSound = 6\nGreenHandSound = 7\nWhiteLEDSound = 8\nSecondSound3 = 9\nSecondSound5 = 10\nQuestionSound = 11\nStartSound = 12\nTimerSound100 = 13")]
    [SerializeField] float _baseVolume = 1f;




    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }

    }

    void Start()
    {
        audioSources = GetComponentsInChildren<AudioSource>();

        PlayBGM();

    }


    public void MuteBGM()
    {
        AudioSource tempAudioSource = audioSources[(int)(EffectSoundNum.BGM)];

        if (tempAudioSource == null) return;
        tempAudioSource.volume = 0f;
        tempAudioSource.Stop();

    }
    public void PlayBGM()
    {
        AudioSource tempAudioSource = audioSources[(int)(EffectSoundNum.BGM)];
        if (tempAudioSource == null) return;
        tempAudioSource.Play();
        tempAudioSource.volume = _soundVolume[(int)EffectSoundNum.BGM];
    }


    public void PlayEffectSound(EffectSoundNum effectSoundNum, float soundVolume = 1f)
    {
        if (GameManager.Instance.IsStarted == false)
        {
            return;
        }


        audioSources[(int)effectSoundNum].PlayOneShot(audioSources[(int)effectSoundNum].clip, _baseVolume * _soundVolume[(int)effectSoundNum]);
        //Debug.Log("Played sound: " + effectSoundNum.ToString() + " with volume: " + soundVolume);
    }

    public void PlayEffectSound(int effectSoundNum)
    {
        if (GameManager.Instance.IsStarted == false)
        {
            Debug.Log("Game Not Started Yet");
            return;
        }

        AudioSource tempAudioSource = audioSources[effectSoundNum];
        if (tempAudioSource != null)
        {
            tempAudioSource.PlayOneShot(tempAudioSource.clip, _soundVolume[(int)effectSoundNum]);
        }
        //Debug.Log("Played sound: " + effectSoundNum.ToString());
    }
    public void StopEffectSound(EffectSoundNum effectSoundNum)
    {
        if (GameManager.Instance.IsStarted == false)
        {
            Debug.Log("Game Not Started Yet");
            return;
        }

        audioSources[(int)effectSoundNum].Stop();

    }

    IEnumerator DelayNPlay(EffectSoundNum effectSoundNum, float delayTime = -1f)
    {
        if (delayTime < 0f)
        {
            delayTime = FadeManager.Instance.FadeDuration;
        }
        yield return CoroutineReturnManager.GetWaitForSeconds(delayTime);
        PlayEffectSound(effectSoundNum);
    }

    public void DelayPlayEffectSound(EffectSoundNum effectSoundNum, float delayTime = -1f)
    {
        StartCoroutine(DelayNPlay(effectSoundNum, delayTime));
    }

    public void Initialize(JsonGenericUpData data)
    {
        _genericData = data;

        data.floatParams.TryGetValue("bgmVolume", out _soundVolume[(int)EffectSoundNum.BGM]);
        data.floatParams.TryGetValue("SaveSoundVolume", out _soundVolume[(int)EffectSoundNum.SaveSound]);
        data.floatParams.TryGetValue("SoulPieceSoundVolume", out _soundVolume[(int)EffectSoundNum.SoulPieceSound]);
        data.floatParams.TryGetValue("ConfirmSoundVolume", out _soundVolume[(int)EffectSoundNum.ConfirmSound]);
        data.floatParams.TryGetValue("PopupSoundVolume", out _soundVolume[(int)EffectSoundNum.PopupSound]);
        data.floatParams.TryGetValue("ActiveSoundVolume", out _soundVolume[(int)EffectSoundNum.ActiveSound]);
        data.floatParams.TryGetValue("StepTextSoundVolume", out _soundVolume[(int)EffectSoundNum.StepTextSound]);
        data.floatParams.TryGetValue("GreenHandSoundVolume", out _soundVolume[(int)EffectSoundNum.GreenHandSound]);
        data.floatParams.TryGetValue("WhiteLEDSoundVolume", out _soundVolume[(int)EffectSoundNum.WhiteLEDSound]);
        data.floatParams.TryGetValue("SecondSound3Volume", out _soundVolume[(int)EffectSoundNum.SecondSound3]);
        data.floatParams.TryGetValue("SecondSound5Volume", out _soundVolume[(int)EffectSoundNum.SecondSound5]);
        data.floatParams.TryGetValue("QuestionSoundVolume", out _soundVolume[(int)EffectSoundNum.QuestionSound]);
        data.floatParams.TryGetValue("StartSoundVolume", out _soundVolume[(int)EffectSoundNum.StartSound]);
        data.floatParams.TryGetValue("TimerSound100Volume", out _soundVolume[(int)EffectSoundNum.TimerSound100]);
        data.floatParams.TryGetValue("BaseVolume", out _baseVolume);



        foreach (var soundVolume in _soundVolume)
        {
            Debug.Log("Loaded sound volume: " + soundVolume);
        }

    }
    public JsonGenericUpData Data()
    {
        _genericData.intParams = new Dictionary<string, int>();
        _genericData.floatParams = new Dictionary<string, float>();
        _genericData.boolParams = new Dictionary<string, bool>();
        _genericData.floatParams["bgmVolume"] = _soundVolume[(int)EffectSoundNum.BGM];
        _genericData.floatParams["SaveSoundVolume"] = _soundVolume[(int)EffectSoundNum.SaveSound];
        _genericData.floatParams["SoulPieceSoundVolume"] = _soundVolume[(int)EffectSoundNum.SoulPieceSound];
        _genericData.floatParams["ConfirmSoundVolume"] = _soundVolume[(int)EffectSoundNum.ConfirmSound];
        _genericData.floatParams["PopupSoundVolume"] = _soundVolume[(int)EffectSoundNum.PopupSound];
        _genericData.floatParams["ActiveSoundVolume"] = _soundVolume[(int)EffectSoundNum.ActiveSound];
        _genericData.floatParams["StepTextSoundVolume"] = _soundVolume[(int)EffectSoundNum.StepTextSound];
        _genericData.floatParams["GreenHandSoundVolume"] = _soundVolume[(int)EffectSoundNum.GreenHandSound];
        _genericData.floatParams["WhiteLEDSoundVolume"] = _soundVolume[(int)EffectSoundNum.WhiteLEDSound];
        _genericData.floatParams["SecondSound3Volume"] = _soundVolume[(int)EffectSoundNum.SecondSound3];
        _genericData.floatParams["SecondSound5Volume"] = _soundVolume[(int)EffectSoundNum.SecondSound5];
        _genericData.floatParams["QuestionSoundVolume"] = _soundVolume[(int)EffectSoundNum.QuestionSound];
        _genericData.floatParams["StartSoundVolume"] = _soundVolume[(int)EffectSoundNum.StartSound];
        _genericData.floatParams["TimerSound100Volume"] = _soundVolume[(int)EffectSoundNum.TimerSound100];
        _genericData.floatParams["BaseVolume"] = _baseVolume;
        return _genericData;
    }
}
