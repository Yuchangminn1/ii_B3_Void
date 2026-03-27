using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;


public class Timer : MonoBehaviour
{
    public float defultTime = 1f;
    Action onTimerEnd;

    protected float time = 0f;

    // public float Time2 => time;

    protected Text _timerText;

    public bool IsCounting = false;

    public bool isDownCount = false;


    public Direction CurrentDirection = Direction.Left;


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
        ResetTimer();
    }

    virtual protected void Start()
    {
        ;
    }

    public void SetTextVisible()
    {
        if (_timerText.color.a < 0.1)
        {
            FadeManager.Instance.SetAlphaOne(_timerText);
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
            IsCounting = false;
            onTimerEnd?.Invoke();

            onTimerEnd = null;
        }
    }

    // public void SetTimerText(string text)
    // {
    //     //_timerText.fontSize = 95;
    //     if (text == "시작!")
    //     {
    //         if (CurrentDirection == Direction.Left)
    //             SoundManager.Instance.PlayEffectSound(EffectSoundNum.StartSound);
    //     }

    //     _timerText.text = text;
    // }

    // public void ResetTime()
    // {
    //     time = defultTime;
    //     _timerText.text = $"{time}";
    //     IsCounting = false;
    // }



    virtual public void ResetTimer()
    {
        time = defultTime;
        _timerText.text = $"{time}";
        IsCounting = false;
        FadeManager.Instance.SetAlphaZero(_timerText);
    }
    public void OffTimer()
    {
        FadeManager.Instance.SetAlphaZero(_timerText);

        time = defultTime;
        _timerText.text = $"{time}";
        IsCounting = false;
    }
    public void StartTimer()
    {
        FadeManager.Instance.SetAlphaOne(_timerText);
        time = defultTime;
        if (CurrentDirection == Direction.Left)
        {
            if (defultTime > 4.9 && defultTime < 5.1)
            {
                SoundManager.Instance.PlayEffectSound(EffectSoundNum.SecondSound5);
            }
            else if (defultTime > 2.9 && defultTime < 3.1)
            {
                SoundManager.Instance.PlayEffectSound(EffectSoundNum.SecondSound3);
            }
        }

        _timerText.text = $"{time}";
        IsCounting = true;
    }

    // public void SetTime(int setTime)
    // {
    //     _timerText.text = $"{setTime}";
    // }
    // public void SetTime(float setTime)
    // {
    //     if (time > 0 && setTime < 0)
    //     {
    //         onTimerEnd?.Invoke();
    //     }
    //     if (setTime < 0)
    //     {
    //         time = 0f;
    //     }
    //     else
    //     {
    //         time = setTime;
    //         if (isDownCount)
    //         {
    //             if (setTime < 1f)
    //                 _timerText.text = $"";
    //             else
    //                 _timerText.text = $"{Mathf.FloorToInt(setTime)}";
    //         }

    //         else
    //         {
    //             if (setTime < 0.5f)
    //                 _timerText.text = $"";
    //             else
    //                 _timerText.text = $"{Mathf.CeilToInt(setTime)}";
    //         }




    //     }

    //}


}

