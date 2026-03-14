using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonScript : MonoBehaviour
{
    Button _button;


    void Awake()
    {
        _button = GetComponent<Button>();
    }

    void OnEnable()
    {
        if (_button != null)
            _button.interactable = true;
    }


    void Start()
    {
        _button.onClick.AddListener(OnClickButton);
    }

    public void OnClickButton()
    {
        _button.interactable = false;
    }
}
