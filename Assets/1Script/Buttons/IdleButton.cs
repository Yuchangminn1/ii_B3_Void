using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class IdleButton : MonoBehaviour, IJsonGenericTarget
{
    int _idleCount = 0;
    float _idleTime = 3f;
    int _idleEventCount = 10;
    Coroutine _idleCoroutine;

    WaitForSeconds waitForSeconds;

    JsonGenericUpData _genericData = new JsonGenericUpData();


    void Awake()
    {
        waitForSeconds = new WaitForSeconds(_idleTime);
    }

    void Start()
    {
        Button button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(OnIdleButton);
        }
    }



    public void OnIdleButton()
    {
        if (_idleCount == 0)
        {
            if (_idleCoroutine == null) _idleCoroutine = StartCoroutine(IdleReset());
        }
        _idleCount++;
        if (_idleCount >= _idleEventCount)
        {
            _idleCount = 0;
            Debug.Log("Button Idle");
            PageController.Instance.IdleButton();
        }
    }

    IEnumerator IdleReset()
    {
        yield return waitForSeconds;
        _idleCount = 0;

        _idleCoroutine = null;
    }

    public void Initialize(JsonGenericUpData data)
    {
        _genericData = data;
        data.floatParams.TryGetValue("idleTime", out _idleTime);
        data.intParams.TryGetValue("idleEventCount", out _idleEventCount);
        Debug.Log("Idle Button Initialized: " + _idleTime + ", " + _idleEventCount);

        waitForSeconds = new WaitForSeconds(_idleTime);
    }

    public JsonGenericUpData Data()
    {
        _genericData.intParams = new Dictionary<string, int>();
        _genericData.floatParams = new Dictionary<string, float>();
        _genericData.boolParams = new Dictionary<string, bool>();
        _genericData.floatParams["idleTime"] = _idleTime;
        _genericData.intParams["idleEventCount"] = _idleEventCount;
        return _genericData;
    }
}
