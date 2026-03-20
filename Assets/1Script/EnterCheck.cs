using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class EnterCheck : MonoBehaviour
{
    public SequenceScript[] TextTriggers;

    public SequenceScript[] NextPageTriggers;

    Coroutine _checkCoroutine = null;

    WaitForSeconds _checkWait = new WaitForSeconds(1f);

    SetUpCoroutine _pageController;

    bool isAllChecked = false;


    void OnEnable()
    {
        isAllChecked = false;
        _checkCoroutine = StartCoroutine(CheckCoroutine());
    }
    void OnDisable()
    {
        if (_checkCoroutine != null)
        {
            StopCoroutine(_checkCoroutine);
            _checkCoroutine = null;
        }

    }

    void Start()
    {
        _pageController = GetComponentInParent<SetUpCoroutine>();
    }


    IEnumerator CheckCoroutine()
    {
        while (isAllChecked == false)
        {
            yield return CoroutineReturnManager.GetWaitForSeconds(1f);

            while (GameManager.Instance.IsStarted == false)
            {
                yield return CoroutineReturnManager.GetWaitForSeconds(1f);
            }

            yield return StartCoroutine(UserDataManager.Instance.RequestUserTagAll());

            if (UserDataManager.Instance.IsUsingRoom)
            {
                foreach (var trigger in TextTriggers)
                {
                    trigger.TriggerOn();
                }
                if (UserDataManager.Instance.IsUser())
                {
                    if (NextPageTriggers != null)
                    {
                        for (int i = 0; i < NextPageTriggers.Length; i++)
                        {
                            NextPageTriggers[i].TriggerOn();
                        }
                    }


                }

            }

            else
            {
                if (_pageController.CurrentPage != 0)
                {
                    _pageController.CurrentPage = 0;
                }
            }

        }

        _checkCoroutine = null;
    }
}
