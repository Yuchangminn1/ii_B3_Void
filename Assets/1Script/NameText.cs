using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class NameText : MonoBehaviour
{

    public Direction CurrentDirection = Direction.Left;

    protected Text _text;

    protected string originText = "";



    void Start()
    {
        _text = GetComponent<Text>();
        originText = _text.text;

    }


    protected virtual void OnEnable()
    {
        if (_text == null)
            return;
        if (originText == "")
        {
            _text.text = originText;
        }

        if (UserDataManager.Instance.GetPlayer() != null)
        {
            SetText(originText);
        }
    }
    public Text GetTextComponent()
    {
        return _text;
    }

    public virtual void SetText(string textData = "")
    {

        if (GameManager.Instance.IsStarted == false || _text == null || UserDataManager.Instance.IsUser() == false)
        {
            return;
        }

        Debug.Log($"NameText SetText 호출: textData='{textData}', originText='{originText}'");

        if (textData == "")
        {
            textData = originText;
        }
        if (textData.Contains("Name"))
        {
            _text.text = textData.Replace("Name", UserDataManager.Instance.GetPlayer(CurrentDirection).FirstName);

        }

    }


}
