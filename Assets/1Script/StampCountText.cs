using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StampCountText : MonoBehaviour
{
    // Start is called before the first frame update

    Text _text;

    public bool IsTotal = false; //총 피스 수 표시 여부

    string originText = "";

    void OnEnable()
    {
        if (GameManager.Instance.CurrentGameMode != GameMode.Playing)
            return;
        if (_text != null && originText == "")
            originText = _text.text;
        if (_text != null)
        {
            if (IsTotal)
                _text.text = _text.text.Replace("Count", (UserDataManager.Instance.GetPlayer().PieceCount + UserDataManager.Instance.GetPlayer().AddPiece).ToString());
            else
                _text.text = _text.text.Replace("Count", (UserDataManager.Instance.GetPlayer().AddPiece).ToString());
        }
    }

    void OnDisable()
    {
        if (originText != "")
            _text.text = originText;
    }
    void Start()
    {
        _text = GetComponent<Text>();
    }

}
