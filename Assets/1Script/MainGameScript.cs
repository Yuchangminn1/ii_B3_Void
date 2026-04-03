using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainGameScript : MonoBehaviour
{
    int _currentIndex = 1;


    public ShowShadowScript[] ShowShadowScripts;
    public AcCheckStep[] AcChecks;

    public Timer timer;

    float _timerDefaultTime = 7f;

    Coroutine _gameCoroutine = null;

    public CameraVisible cameraVisible;

    public SequenceScript sequenceScript;



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


    public IEnumerator StartGameCoroutine()
    {
        _currentIndex = 1; //0은 이미 튜토리얼로 했음

        yield return null;

        for (int i = 0; i < ShowShadowScripts.Length; i++)
        {
            ShowShadowScripts[i].SetACcheck(AcChecks[i]);

        }

        cameraVisible.CameraOn();

        foreach (var ShowShadowScript in ShowShadowScripts)
        {
            ShowShadowScript.ShowShadow(_currentIndex);
        }

        timer.AddOnEndListener(() => StartCoroutine(NextStep()));
        _gameCoroutine = null;

    }

    public IEnumerator NextStep()
    {
        yield return CoroutineReturnManager.GetWaitForSeconds(2f);

        _currentIndex++;

        if (_currentIndex >= ShowShadowScripts[0].GetShowImageLength())
        {
            Debug.Log("게임 클리어");
            sequenceScript?.TriggerOn();
            yield break;
        }
        for (int i = 0; i < ShowShadowScripts.Length; i++)
        {
            ShowShadowScripts[i].ResultImageClear();
            ShowShadowScripts[i].SetACcheck(AcChecks[i]);

        }


        foreach (var ShowShadowScript in ShowShadowScripts)
        {
            ShowShadowScript.ShowShadow(_currentIndex);
        }

        yield return CoroutineReturnManager.GetWaitForSeconds(1f);

        timer.AddOnEndListener(() => StartCoroutine(NextStep()));

    }
}
