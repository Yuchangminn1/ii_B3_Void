using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NameText : MonoBehaviour
{
    Text _text;


    public Direction PlayerDirection;

    string currentText = "";

    //bool isTextSet = false;





    void Start()
    {
        _text = GetComponent<Text>();
        currentText = _text.text;
    }


    void OnEnable()
    {
        if (UserDataManager.Instance.GetPlayer(PlayerDirection) != null)
        {
            SetText();
        }
    }

    public Text GetTextComponent()
    {
        return _text;
    }

    public void SetText()
    {
        _text.text = currentText.Replace("Name", UserDataManager.Instance.GetPlayer(PlayerDirection).FirstName);


    }


}
