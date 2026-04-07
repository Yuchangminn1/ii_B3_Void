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
    ShowShadowSound,
    ClearShadowSound,
    SecondSound3,
    SecondSound5,
    QuestionSound,
    StartSound,
    SecondSound10,
    AllReadySound,
    PieceShootSound,
    FailSound,
    SecondSound60,
    FinaleSound,
    SecondSound7
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

    [Header("사운드 매니저\nBGM = 0\nSaveSound = 1\nSoulPieceSound = 2\nConfirmSound = 3\nPopupSound = 4\nActiveSound = 5\nStepTextSound = 6\nShowShadowSound = 7\nClearShadowSound = 8\nSecondSound3 = 9\nSecondSound5 = 10\nQuestionSound = 11\nStartSound = 12\nSecondSound10 = 13\nAllReadySound = 14\nPieceShootSound = 15\nFailSound = 16\nSecondSound60 = 17\nFinaleSound = 18 \nSecondSound7 = 19")]
    [SerializeField] float _baseVolume = 1f;

    Coroutine _finaleCoroutine = null;






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

    public void MuteBGMNPlayFinaleSound()
    {
        if (_finaleCoroutine != null)
        {
            StopCoroutine(_finaleCoroutine);
        }
        _finaleCoroutine = StartCoroutine(MuteBGMNFinaleSoundCoroutine());
    }

    public IEnumerator MuteBGMNFinaleSoundCoroutine()
    {


        float fadeDuration = 1.5f;
        float elapsed = 0f;
        float startVolume = audioSources[(int)EffectSoundNum.BGM].volume;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.fixedDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            audioSources[(int)EffectSoundNum.BGM].volume = Mathf.Lerp(startVolume, 0f, t);
            yield return CoroutineReturnManager.WaitForFixedUpdate;
        }
        audioSources[(int)EffectSoundNum.BGM].volume = 0f;
        audioSources[(int)EffectSoundNum.FinaleSound].Play();

        while (audioSources[(int)EffectSoundNum.FinaleSound].isPlaying)
        {
            yield return CoroutineReturnManager.WaitForFixedUpdate;
        }
        while (audioSources[(int)EffectSoundNum.BGM].volume < _soundVolume[(int)EffectSoundNum.BGM])
        {
            audioSources[(int)EffectSoundNum.BGM].volume += Time.fixedDeltaTime * _baseVolume;
            yield return CoroutineReturnManager.WaitForFixedUpdate;
        }
        audioSources[(int)(EffectSoundNum.BGM)].volume = _soundVolume[(int)EffectSoundNum.BGM];

        _finaleCoroutine = null;
    }


    public void MuteBGM()
    {
        if (audioSources[(int)(EffectSoundNum.BGM)] == null) return;
        audioSources[(int)(EffectSoundNum.BGM)].volume = 0f;
        audioSources[(int)(EffectSoundNum.BGM)].Stop();

    }
    public void PlayBGM()
    {
        if (_finaleCoroutine != null)
        {
            StopCoroutine(_finaleCoroutine);
            _finaleCoroutine = null;
        }

        if (audioSources[(int)(EffectSoundNum.BGM)] == null) return;
        audioSources[(int)(EffectSoundNum.BGM)].Play();
        audioSources[(int)(EffectSoundNum.BGM)].volume = _soundVolume[(int)EffectSoundNum.BGM];
    }


    public void PlayEffectSound(EffectSoundNum effectSoundNum, float soundVolume = 1f)
    {
        if (GameManager.Instance.IsStarted == false)
        {
            return;
        }


        int soundIndex = (int)effectSoundNum;
        AudioSource effectSource = audioSources[soundIndex];
        if (effectSource == null)
        {
            return;
        }

        effectSource.PlayOneShot(effectSource.clip, _baseVolume * _soundVolume[soundIndex] * soundVolume);
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
        data.floatParams.TryGetValue("ShowShadowSoundVolume", out _soundVolume[(int)EffectSoundNum.ShowShadowSound]);
        data.floatParams.TryGetValue("ClearShadowSoundVolume", out _soundVolume[(int)EffectSoundNum.ClearShadowSound]);
        data.floatParams.TryGetValue("SecondSound3Volume", out _soundVolume[(int)EffectSoundNum.SecondSound3]);
        data.floatParams.TryGetValue("SecondSound5Volume", out _soundVolume[(int)EffectSoundNum.SecondSound5]);
        data.floatParams.TryGetValue("QuestionSoundVolume", out _soundVolume[(int)EffectSoundNum.QuestionSound]);
        data.floatParams.TryGetValue("StartSoundVolume", out _soundVolume[(int)EffectSoundNum.StartSound]);
        data.floatParams.TryGetValue("SecondSound10Volume", out _soundVolume[(int)EffectSoundNum.SecondSound10]);
        data.floatParams.TryGetValue("AllReadySoundVolume", out _soundVolume[(int)EffectSoundNum.AllReadySound]);
        data.floatParams.TryGetValue("PieceShootSoundVolume", out _soundVolume[(int)EffectSoundNum.PieceShootSound]);
        data.floatParams.TryGetValue("FailSoundVolume", out _soundVolume[(int)EffectSoundNum.FailSound]);
        data.floatParams.TryGetValue("SecondSound60Volume", out _soundVolume[(int)EffectSoundNum.SecondSound60]);
        data.floatParams.TryGetValue("FinaleSoundVolume", out _soundVolume[(int)EffectSoundNum.FinaleSound]);
        data.floatParams.TryGetValue("SecondSound7Volume", out _soundVolume[(int)EffectSoundNum.SecondSound7]);

        data.floatParams.TryGetValue("BaseVolume", out _baseVolume);



#if UNITY_EDITOR || DEVELOPMENT_BUILD
        foreach (var soundVolume in _soundVolume)
        {
            Debug.Log("Loaded sound volume: " + soundVolume);
        }
#endif

    }

    // public void StopCountSound()
    // {
    //     StopEffectSound(EffectSoundNum.SecondSound3);
    //     StopEffectSound(EffectSoundNum.SecondSound5);
    //     StopEffectSound(EffectSoundNum.SecondSound7);
    //     StopEffectSound(EffectSoundNum.SecondSound10);
    //     StopEffectSound(EffectSoundNum.SecondSound60);
    // }
    public JsonGenericUpData Data()
    {
        if (_genericData.intParams == null)
        {
            _genericData.intParams = new Dictionary<string, int>();
        }
        else
        {
            _genericData.intParams.Clear();
        }

        if (_genericData.floatParams == null)
        {
            _genericData.floatParams = new Dictionary<string, float>(_soundVolume.Length + 1);
        }
        else
        {
            _genericData.floatParams.Clear();
        }

        if (_genericData.boolParams == null)
        {
            _genericData.boolParams = new Dictionary<string, bool>();
        }
        else
        {
            _genericData.boolParams.Clear();
        }

        _genericData.floatParams["bgmVolume"] = _soundVolume[(int)EffectSoundNum.BGM];
        _genericData.floatParams["SaveSoundVolume"] = _soundVolume[(int)EffectSoundNum.SaveSound];
        _genericData.floatParams["SoulPieceSoundVolume"] = _soundVolume[(int)EffectSoundNum.SoulPieceSound];
        _genericData.floatParams["ConfirmSoundVolume"] = _soundVolume[(int)EffectSoundNum.ConfirmSound];
        _genericData.floatParams["PopupSoundVolume"] = _soundVolume[(int)EffectSoundNum.PopupSound];
        _genericData.floatParams["ActiveSoundVolume"] = _soundVolume[(int)EffectSoundNum.ActiveSound];
        _genericData.floatParams["StepTextSoundVolume"] = _soundVolume[(int)EffectSoundNum.StepTextSound];
        _genericData.floatParams["ShowShadowSoundVolume"] = _soundVolume[(int)EffectSoundNum.ShowShadowSound];
        _genericData.floatParams["ClearShadowSoundVolume"] = _soundVolume[(int)EffectSoundNum.ClearShadowSound];
        _genericData.floatParams["SecondSound3Volume"] = _soundVolume[(int)EffectSoundNum.SecondSound3];
        _genericData.floatParams["SecondSound5Volume"] = _soundVolume[(int)EffectSoundNum.SecondSound5];
        _genericData.floatParams["QuestionSoundVolume"] = _soundVolume[(int)EffectSoundNum.QuestionSound];
        _genericData.floatParams["StartSoundVolume"] = _soundVolume[(int)EffectSoundNum.StartSound];
        _genericData.floatParams["SecondSound10Volume"] = _soundVolume[(int)EffectSoundNum.SecondSound10];
        _genericData.floatParams["AllReadySoundVolume"] = _soundVolume[(int)EffectSoundNum.AllReadySound];
        _genericData.floatParams["PieceShootSoundVolume"] = _soundVolume[(int)EffectSoundNum.PieceShootSound];
        _genericData.floatParams["FailSoundVolume"] = _soundVolume[(int)EffectSoundNum.FailSound];
        _genericData.floatParams["SecondSound60Volume"] = _soundVolume[(int)EffectSoundNum.SecondSound60];
        _genericData.floatParams["FinaleSoundVolume"] = _soundVolume[(int)EffectSoundNum.FinaleSound];

        _genericData.floatParams["SecondSound7Volume"] = _soundVolume[(int)EffectSoundNum.SecondSound7];
        _genericData.floatParams["BaseVolume"] = _baseVolume;
        return _genericData;
    }
}
