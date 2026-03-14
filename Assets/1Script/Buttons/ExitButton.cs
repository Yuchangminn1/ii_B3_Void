using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ExitButton : MonoBehaviour, IJsonGenericTarget
{
    int _exitCount = 0;
    float _exitTime = 3f;
    int _exitEventCount = 10;
    Coroutine _exitCoroutine;

    WaitForSeconds waitForSeconds;

    JsonGenericUpData _genericData = new JsonGenericUpData();


    void Awake()
    {
        waitForSeconds = new WaitForSeconds(_exitTime);
    }

    void Start()
    {
        Button button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(OnExitButton);
        }
    }

    public void OnExitButton()
    {
        if (_exitCount == 0)
        {
            if (_exitCoroutine == null) _exitCoroutine = StartCoroutine(ExitReset());
        }
        _exitCount++;
        if (_exitCount >= _exitEventCount)
        {
            _exitCount = 0;
            Debug.Log("Button Exit");
            Application.Quit();
        }
    }

    IEnumerator ExitReset()
    {
        yield return waitForSeconds;
        _exitCount = 0;

        _exitCoroutine = null;
    }

    public void Initialize(JsonGenericUpData data)
    {
        _genericData = data;
        data.floatParams.TryGetValue("exitTime", out _exitTime);
        data.intParams.TryGetValue("exitEventCount", out _exitEventCount);

        Debug.Log("Exit Button Initialized: " + _exitTime + ", " + _exitEventCount);
        waitForSeconds = new WaitForSeconds(_exitTime);
    }

    public JsonGenericUpData Data()
    {

        _genericData.intParams = new Dictionary<string, int>();
        _genericData.floatParams = new Dictionary<string, float>();
        _genericData.boolParams = new Dictionary<string, bool>();
        _genericData.floatParams["exitTime"] = _exitTime;
        _genericData.intParams["exitEventCount"] = _exitEventCount;
        return _genericData;
    }
}
