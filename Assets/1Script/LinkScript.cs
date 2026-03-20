using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LinkScript : MonoBehaviour
{
    public Graphic MainPlayerGraphic;
    public Graphic SubPlayerGraphic;


    bool _isMainPlayerChecked = false;
    bool _isSubPlayerChecked = false;

    Coroutine _nextCoroutine = null;

    public SequenceScript NextTrigger;

    WaitForSeconds _nextWaitForSeconds = new WaitForSeconds(0.5f);



    bool isCut = false;

    void OnEnable()
    {
        Reset();
    }


    public void Reset()
    {
        FadeManager.Instance.SetAlphaZero(MainPlayerGraphic);
        FadeManager.Instance.SetAlphaZero(SubPlayerGraphic);
        _isMainPlayerChecked = false;
        _isSubPlayerChecked = false;
        _nextCoroutine = null;

    }

    public void CheckMainPlayer(bool isChecked)
    {
        if (_isMainPlayerChecked == isChecked)
            return;
        _isMainPlayerChecked = isChecked;

        if (isChecked)
        {
            if (isCut)
                FadeManager.Instance.SetAlphaOne(MainPlayerGraphic);
            else
                FadeManager.Instance.TargetFade(MainPlayerGraphic, 1f);

            if (_isSubPlayerChecked && _nextCoroutine == null)
                _nextCoroutine = StartCoroutine(NextCoroutine());
        }
        else
        {
            if (isCut)
                FadeManager.Instance.SetAlphaZero(MainPlayerGraphic);
            else
                FadeManager.Instance.TargetFade(MainPlayerGraphic, 0f);
        }


    }


    public void CheckSubPlayer(bool isChecked)
    {
        if (_isSubPlayerChecked == isChecked)
            return;
        _isSubPlayerChecked = isChecked;

        if (isChecked)
        {
            if (isCut)
                FadeManager.Instance.SetAlphaOne(SubPlayerGraphic);
            else
                FadeManager.Instance.TargetFade(SubPlayerGraphic, 1f);

            if (_isMainPlayerChecked && _nextCoroutine == null)
                _nextCoroutine = StartCoroutine(NextCoroutine());
        }
        else
        {
            if (isCut)
                FadeManager.Instance.SetAlphaZero(SubPlayerGraphic);
            else
                FadeManager.Instance.TargetFade(SubPlayerGraphic, 0f);
        }
    }

    IEnumerator NextCoroutine()
    {
        yield return _nextWaitForSeconds;
        if (NextTrigger != null)
        {
            NextTrigger.TriggerOn();
        }
        _nextCoroutine = null;
    }


}
