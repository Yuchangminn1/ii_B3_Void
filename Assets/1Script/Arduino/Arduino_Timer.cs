using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class Arduino_Timer : Timer
{
    //TODO 망한 구조 수정

    override protected void OnEnable()
    {
        if (GameManager.Instance.IsStarted == false)
            return;
        if (PageController.Instance.CurrentPage == 4 || PageController.Instance.CurrentPage == 3)
            defultTime = GameManager.Instance.Page4TimerDefaultTime;
        base.OnEnable();
    }

    public float FonrtSize = 173;

    override protected void Start()
    {
        TouchChecker arduino_Touch = GetComponentInParent<Arduino_Touch_Page4>();

        if (arduino_Touch)
        {
            ;
        }
        else
        {
            arduino_Touch = GetComponentInParent<TouchChecker>();
            arduino_Touch?.OnTagSuccess_ChangeGraphic.AddListener(ResetTimer);
            arduino_Touch?.OnTagWaitStart_ResetGraphic.AddListener(ResetTimer);
            // arduino_Touch?.AddOnCheckStartListener(SetTimer);
        }
    }

    public void SetTimer(Arduino arduino_Touch)
    {
        if (gameObject.activeInHierarchy)
        {
            arduino_Touch.GetComponent<TouchChecker>().timer = this;
            Debug.Log($"{name} Chain On ");

        }

    }
    override public void ResetTimer()
    {
        _timerText.fontSize = 173;

        base.ResetTimer();

    }
}
