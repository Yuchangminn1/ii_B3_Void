using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainGameScript : MonoBehaviour
{


    int _currentIndex = 1;


    public ShowShadowScript CShowShadowScript;
    public AcCheckStep CAcCheck;

    public Timer timer;

    public Direction CurrentDirection;

    //float _timerDefaultTime = 7f;

    Coroutine _gameCoroutine = null;

    Coroutine _nextCoroutine = null;


    public CameraVisible cameraVisible;

    public SequenceScript sequenceScript;


    void Start()
    {
        PageController.Instance.OnReset += Reset;
    }



    public int CurrentIndex
    {
        get { return _currentIndex; }
    }


    public void StartGame()
    {
        if (_gameCoroutine != null)
        {
            StopCoroutine(_gameCoroutine);
        }

        _gameCoroutine = StartCoroutine(StartGameCoroutine());

    }

    public void Reset()
    {
        if (_gameCoroutine != null)
        {
            StopCoroutine(_gameCoroutine);
            _gameCoroutine = null;

        }

        if (_nextCoroutine != null)
        {
            StopCoroutine(_nextCoroutine);
            _nextCoroutine = null;

        }
        // if (CurrentDirection == Direction.Left)
        //     SoundManager.Instance.StopEffectSound(EffectSoundNum.SecondSound60);
    }


    public IEnumerator StartGameCoroutine()
    {


        _currentIndex = 1; //0은 이미 튜토리얼로 했음
        // if (CurrentDirection == Direction.Left)
        //     SoundManager.Instance.PlayEffectSound(EffectSoundNum.SecondSound60);

        CAcCheck.CurrentIndex = _currentIndex - 1;

        yield return null;
        if (_gameCoroutine == null)
            yield break;

        CShowShadowScript.SetACcheck(CAcCheck);

        cameraVisible.CameraOn();

        CShowShadowScript.ShowShadow(_currentIndex);

        timer.AddOnEndListener(NextStep);
        timer.AddOnEndListener(NextStep);

        _gameCoroutine = null;

    }

    public void NextStep()
    {
        if (_nextCoroutine != null)
        {
            StopCoroutine(_nextCoroutine);
        }

        _nextCoroutine = StartCoroutine(NextStepCoroutine());
    }

    public IEnumerator NextStepCoroutine()
    {
        yield return CoroutineReturnManager.GetWaitForSeconds(2f);
        if (_nextCoroutine == null)
            yield break;

        _currentIndex++;

        if (_currentIndex >= CShowShadowScript.GetShowImageLength())
        {
            Debug.Log("게임 클리어");
            sequenceScript?.TriggerOn();
            _nextCoroutine = null;
            yield break;
        }

        CShowShadowScript.ResultImageClear();

        CShowShadowScript.SetACcheck(CAcCheck);

        CShowShadowScript.ShowShadow(_currentIndex);


        yield return CoroutineReturnManager.GetWaitForSeconds(1f);
        if (_nextCoroutine == null)
            yield break;

        timer.AddOnEndListener(NextStep);

        _nextCoroutine = null;

    }
}
