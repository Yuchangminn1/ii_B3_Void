using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class SoundObject : MonoBehaviour
{
    Graphic _graphic;

    Coroutine _showCoroutine = null;

    void Start()
    {
        _graphic = GetComponent<Graphic>();
    }
    public EffectSoundNum _effectSoundNum;

    public void ShowObject()
    {
        if (_showCoroutine == null)
            _showCoroutine = StartCoroutine(ShowCoroutineObject());


    }

    public IEnumerator ShowCoroutineObject()
    {
        while (_graphic.color.a < 0.99f)
        {
            yield return CoroutineReturnManager.WaitForFixedUpdate;
        }
        SoundManager.Instance.PlayEffectSound(_effectSoundNum);
        _showCoroutine = null;
    }
    void OnDisable()
    {
        if (_showCoroutine != null)
        {
            StopCoroutine(_showCoroutine);
            _showCoroutine = null;
        }
    }
}
