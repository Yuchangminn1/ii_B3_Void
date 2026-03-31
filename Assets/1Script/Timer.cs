using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;


public class Timer : MonoBehaviour
{
    public float defaultTime = 1f;
    Action onTimerEnd;

    protected float time = 0f;

    // public float Time2 => time;

    protected Text _timerText;

    public bool IsCounting = false;

    public bool isDownCount = false;


    public Direction CurrentDirection = Direction.Left;



    Graphic[] timerGraphics;


    public void AddOnEndListener(Action action)
    {
        onTimerEnd += action;
    }





    void Awake()
    {
        _timerText = GetComponentInChildren<Text>();
    }

    virtual protected void OnEnable()
    {
        if (GameManager.Instance.IsStarted)
            ResetTimer();
    }

    virtual protected void Start()
    {
        timerGraphics = GetComponentsInChildren<Graphic>();
        FadeManager.Instance.SetAlphaZero(timerGraphics);
    }

    public void SetTextVisible()
    {
        if (_timerText.color.a < 0.1)
        {
            FadeManager.Instance.SetAlphaOne(_timerText);
        }

    }
    public void SetTextInvisible()
    {
        if (_timerText.color.a > 0.1)
        {
            FadeManager.Instance.SetAlphaZero(_timerText);
        }

    }
    void FixedUpdate()
    {
        if (IsCounting && time > 0)
        {
            time -= Time.fixedDeltaTime;
            _timerText.text = $"{Mathf.CeilToInt(time)}";
        }
        else if (IsCounting && time <= 0)
        {
            FadeManager.Instance.SetAlphaZero(timerGraphics);

            IsCounting = false;
            onTimerEnd?.Invoke();

            onTimerEnd = null;
        }
    }

    virtual public void ResetTimer()
    {
        FadeManager.Instance.SetAlphaZero(timerGraphics);

        time = defaultTime;
        _timerText.text = $"{time}";
        IsCounting = false;
        FadeManager.Instance.SetAlphaZero(_timerText);
    }
    public void OffTimer()
    {
        FadeManager.Instance.SetAlphaZero(_timerText);

        time = defaultTime;
        _timerText.text = $"{time}";
        IsCounting = false;
    }
    public void StartTimer()
    {

        FadeManager.Instance.SetAlphaOne(timerGraphics);

        FadeManager.Instance.SetAlphaOne(_timerText);
        time = defaultTime;
        if (CurrentDirection == Direction.Left)
        {
            if (defaultTime > 4.9 && defaultTime < 5.1)
            {
                SoundManager.Instance.PlayEffectSound(EffectSoundNum.SecondSound5);
            }
            else if (defaultTime > 2.9 && defaultTime < 3.1)
            {
                SoundManager.Instance.PlayEffectSound(EffectSoundNum.SecondSound3);
            }
        }

        _timerText.text = $"{time}";
        IsCounting = true;
    }



}

