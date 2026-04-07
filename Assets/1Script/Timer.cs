using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;


public class Timer : MonoBehaviour
{
    float _defaultTime = 1f;

    public float DefaultTime
    {
        get
        {
            return _defaultTime;
        }

    }


    Action onTimerEnd;

    protected float time = 0f;

    // public float Time2 => time;

    public Text _timerText;

    public bool IsCounting = false;

    public bool isDownCount = false;


    public Direction CurrentDirection = Direction.Left;


    public UnityEvent OnTimerEndEvent;


    public RawImage PairTimerImage;

    public Text PairTimerText;



    public Graphic[] timerGraphics;

    EffectSoundNum _currentEffectSoundNum = EffectSoundNum.BGM;



    public void AddOnEndListener(Action action)
    {
        onTimerEnd += action;
    }







    void Awake()
    {
        if (_timerText == null)
            _timerText = GetComponentInChildren<Text>();
    }


    virtual protected void OnEnable()
    {
        if (GameManager.Instance.IsStarted)
            ResetTimer();
    }

    public void Reset()
    {
        time = _defaultTime;
        SetTimerText(time);
        IsCounting = false;
        FadeManager.Instance.SetAlphaZero(timerGraphics);
        FadeManager.Instance.SetAlphaZero(PairTimerImage);
        FadeManager.Instance.SetAlphaZero(PairTimerText);
        onTimerEnd = null;
    }

    public void SetTimerText(float newTime)
    {
        _timerText.text = $"{newTime}";
        PairTimerText.text = $"{newTime}";
    }

    virtual protected void Start()
    {
        PageController.Instance.OnReset += Reset;
        if (timerGraphics == null || timerGraphics.Length == 0)
        {
            timerGraphics = GetComponentsInChildren<Graphic>();
        }
        FadeManager.Instance.SetAlphaZero(timerGraphics);
        FadeManager.Instance.SetAlphaZero(PairTimerImage);
        FadeManager.Instance.SetAlphaZero(PairTimerText);

    }

    public void SetTextVisible()
    {
        if (_timerText.color.a < 0.1)
        {
            FadeManager.Instance.SetAlphaOne(_timerText);
            FadeManager.Instance.SetAlphaOne(PairTimerText);

        }

    }
    public void SetTextInvisible()
    {
        if (_timerText.color.a > 0.1)
        {
            FadeManager.Instance.SetAlphaZero(_timerText);
            FadeManager.Instance.SetAlphaZero(PairTimerText);
        }

    }
    void FixedUpdate()
    {
        if (IsCounting && time > 0)
        {
            time -= Time.fixedDeltaTime;
            SetTimerText(Mathf.CeilToInt(time));

        }
        else if (IsCounting && time <= 0)
        {
            FadeManager.Instance.SetAlphaZero(timerGraphics);
            FadeManager.Instance.SetAlphaZero(PairTimerImage);
            FadeManager.Instance.SetAlphaZero(PairTimerText);
            IsCounting = false;
            onTimerEnd?.Invoke();
            OnTimerEndEvent?.Invoke();

            onTimerEnd = null;
        }
    }


    public void SetDefaultTime(float time)
    {
        _defaultTime = time;
    }

    virtual public void ResetTimer()
    {

        FadeManager.Instance.SetAlphaZero(timerGraphics);
        FadeManager.Instance.SetAlphaZero(PairTimerImage);

        if (_currentEffectSoundNum != EffectSoundNum.BGM)
            SoundManager.Instance.StopEffectSound(_currentEffectSoundNum);




        time = _defaultTime;
        SetTimerText(time);
        IsCounting = false;
        FadeManager.Instance.SetAlphaZero(_timerText);
        FadeManager.Instance.SetAlphaZero(PairTimerText);

    }
    public void OffTimer()
    {
        FadeManager.Instance.SetAlphaZero(_timerText);
        FadeManager.Instance.SetAlphaZero(PairTimerText);


        time = _defaultTime;
        SetTimerText(time);
        IsCounting = false;
    }
    public void StartTimer()
    {

        FadeManager.Instance.SetAlphaOne(timerGraphics);

        FadeManager.Instance.SetAlphaOne(_timerText);
        FadeManager.Instance.SetAlphaOne(PairTimerImage);
        FadeManager.Instance.SetAlphaOne(PairTimerText);

        time = _defaultTime;
        if (CurrentDirection == Direction.Left)
        {
            if (_defaultTime > 9.9 && _defaultTime < 10.1)
            {
                _currentEffectSoundNum = EffectSoundNum.SecondSound10;
                SoundManager.Instance.PlayEffectSound(EffectSoundNum.SecondSound10);
            }
            else if (_defaultTime > 6.9 && _defaultTime < 7.1)
            {
                _currentEffectSoundNum = EffectSoundNum.SecondSound7;
                SoundManager.Instance.PlayEffectSound(EffectSoundNum.SecondSound7);
            }
            else if (_defaultTime > 4.9 && _defaultTime < 5.1)
            {
                _currentEffectSoundNum = EffectSoundNum.SecondSound5;

                SoundManager.Instance.PlayEffectSound(EffectSoundNum.SecondSound5);
            }
            else if (_defaultTime > 2.9 && _defaultTime < 3.1)
            {
                _currentEffectSoundNum = EffectSoundNum.SecondSound3;
                SoundManager.Instance.PlayEffectSound(EffectSoundNum.SecondSound3);
            }
        }

        SetTimerText(time);
        IsCounting = true;
    }



}

